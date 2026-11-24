using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Agenda;

/// <summary>Solicitud de cita del sitio público (sin cuenta). El backend es la autoridad de cada regla.</summary>
public class SolicitudPublicaDto
{
    [Range(1, int.MaxValue)] public int ServicioId { get; set; }
    [Required] public string? Fecha { get; set; }
    [Required] public string? Hora { get; set; }
    [Required, MinLength(2), MaxLength(100)] public string? Nombre { get; set; }
    [Required, MinLength(2), MaxLength(100)] public string? Apellido { get; set; }
    [Required, MaxLength(20)] public string? Telefono { get; set; }
    [Required, MaxLength(200), EmailAddress] public string? Correo { get; set; }
    [MaxLength(500)] public string? Motivo { get; set; }
    public bool AceptaAviso { get; set; }
    /// <summary>Campo trampa: invisible para las personas; si trae algo, es un robot.</summary>
    [MaxLength(200)] public string? SitioWeb { get; set; }
}

public sealed record SolicitudCreada(string Fecha, string Hora, string Servicio);

public class SolicitudInvalidaException(string mensaje) : ArgumentException(mensaje);

public sealed record SolicitudBandejaDto(
    int Id, uint Version, string Paciente, string? Telefono, string? Correo, int PacienteId, string Servicio,
    string Fecha, string Hora, string? Motivo, string Dentista, DateTime CreadaEn, DateTime? ExpiraEn);

public interface ISolicitudesPublicasService
{
    /// <summary>Null si el campo trampa vino lleno (se responde igual que un éxito, sin crear nada).</summary>
    Task<SolicitudCreada?> CrearAsync(SolicitudPublicaDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudBandejaDto>> BandejaAsync(CancellationToken ct = default);
}

public class SolicitudesPublicasService : ISolicitudesPublicasService
{
    private static readonly Regex Letras = new(@"^[\p{L}][\p{L}\p{M} '.-]*$", RegexOptions.Compiled);

    private readonly DentissaDbContext _db;
    private readonly IReservasService _reservas;
    private readonly IRelojClinica _reloj;
    private readonly TimeProvider _tiempo;

    public SolicitudesPublicasService(DentissaDbContext db, IReservasService reservas, IRelojClinica reloj, TimeProvider tiempo)
    {
        _db = db;
        _reservas = reservas;
        _reloj = reloj;
        _tiempo = tiempo;
    }

    public async Task<SolicitudCreada?> CrearAsync(SolicitudPublicaDto dto, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(dto.SitioWeb)) return null;
        if (!dto.AceptaAviso) throw new SolicitudInvalidaException("Debes aceptar el aviso de privacidad.");
        if (!DateOnly.TryParseExact(dto.Fecha, "yyyy-MM-dd", out var fecha) || !TimeOnly.TryParseExact(dto.Hora, "HH:mm", out var hora))
            throw new SolicitudInvalidaException("Revisa la fecha y el horario.");
        var nombre = dto.Nombre!.Trim();
        var apellido = dto.Apellido!.Trim();
        if (!Letras.IsMatch(nombre) || !Letras.IsMatch(apellido))
            throw new SolicitudInvalidaException("Escribe el nombre y el apellido solo con letras.");
        var digitos = Regex.Replace(dto.Telefono!, "[^0-9]", "");
        if (digitos.Length is < 10 or > 15 || !Regex.IsMatch(dto.Telefono!, @"^[0-9 +()\-.]+$"))
            throw new SolicitudInvalidaException("El teléfono debe tener de 10 a 15 dígitos.");
        var correo = dto.Correo!.Trim().ToLowerInvariant();
        var motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim();

        var servicio = await _db.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.ServicioId && s.Activo, ct)
            ?? throw new ServicioNoDisponibleException();

        // Mismo correo = misma persona (sin cuenta). Nunca se revela si ya existía: la respuesta es igual.
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.Correo != null && p.Correo.ToLower() == correo, ct);
        var nuevo = paciente is null;
        if (nuevo)
        {
            paciente = new Paciente
            {
                Nombre = nombre, Apellido = apellido, Correo = correo, Telefono = dto.Telefono!.Trim(),
                Origen = OrigenesCita.Web, ContactoVerificado = false,
            };
            _db.Pacientes.Add(paciente);
            await _db.SaveChangesAsync(ct);
        }

        try
        {
            await _reservas.ReservarAsync(new SolicitudReserva(paciente!.Id, null, servicio.Id, fecha, hora, CanalReserva.EnLinea,
                OrigenesCita.Web, "Solicitud desde el sitio", motivo), ct);
        }
        catch
        {
            // La reserva no se hizo: la ficha recién creada no debe quedar como paciente (baja lógica)
            if (nuevo)
            {
                paciente!.EliminadoEn = _tiempo.GetUtcNow().UtcDateTime;
                await _db.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
        return new SolicitudCreada(fecha.ToString("yyyy-MM-dd"), hora.ToString("HH:mm"), servicio.Nombre);
    }

    public async Task<IReadOnlyList<SolicitudBandejaDto>> BandejaAsync(CancellationToken ct = default)
    {
        var ahora = _tiempo.GetUtcNow().UtcDateTime;
        var filas = await _db.Citas.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.Estado == EstadosCita.Pendiente && (c.Origen == OrigenesCita.Web || c.Origen == OrigenesCita.Portal)
                        && (c.BloqueoExpiraEn == null || c.BloqueoExpiraEn > ahora) && c.Inicio > ahora)
            .OrderBy(c => c.Inicio)
            .Select(c => new
            {
                c.Id, c.Version, c.PacienteId, c.Paciente.Nombre, c.Paciente.Apellido, c.Paciente.Telefono, c.Paciente.Correo,
                Servicio = c.Servicio.Nombre, c.Inicio, c.Motivo, c.CreadoEn, c.BloqueoExpiraEn,
                Dentista = (c.Dentista.Titulo == null ? "" : c.Dentista.Titulo + " ") + c.Dentista.Usuario.Nombre + " " + c.Dentista.Usuario.Apellido,
            })
            .ToListAsync(ct);
        return filas.Select(f =>
        {
            var (fecha, hora) = _reloj.ALocal(f.Inicio);
            return new SolicitudBandejaDto(f.Id, f.Version, $"{f.Nombre} {f.Apellido}", f.Telefono, f.Correo, f.PacienteId, f.Servicio,
                fecha.ToString("yyyy-MM-dd"), hora.ToString("HH:mm"), f.Motivo, f.Dentista, f.CreadoEn, f.BloqueoExpiraEn);
        }).ToList();
    }
}
