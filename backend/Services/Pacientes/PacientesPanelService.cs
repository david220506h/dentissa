using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Pacientes;

public sealed record CitaResumenDto(int Id, string Fecha, string Hora, string Estado, string Servicio);

/// <summary>Elemento de la lista (propuesta de pacientes, 1.1). Nunca correo, CURP, dirección ni expediente.</summary>
public sealed record PacienteListaDto(
    int Id, string Nombre, string Apellido, string? Telefono, string Cuenta, string? Alergias,
    CitaResumenDto? UltimaCita, CitaResumenDto? ProximaCita);

/// <summary>Datos generales (1.2): la lista más correo, fecha de nacimiento, sexo y próximas citas.</summary>
public sealed record PacienteDetalleDto(
    int Id, string Nombre, string Apellido, string? Telefono, string? Correo, DateOnly? FechaNacimiento, string? Sexo,
    string Cuenta, string? Alergias, CitaResumenDto? UltimaCita, IReadOnlyList<CitaResumenDto> ProximasCitas);

public sealed record PaginaPacientes(IReadOnlyList<PacienteListaDto> Elementos, int Total, int Pagina, int Tamano);

public class PacienteEdicionDto
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "El apellido es obligatorio"), MaxLength(100)] public string Apellido { get; set; } = "";
    [MaxLength(20)] public string? Telefono { get; set; }
    [MaxLength(200), EmailAddress(ErrorMessage = "Correo inválido")] public string? Correo { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    [MaxLength(20)] public string? Sexo { get; set; }
    [MaxLength(1000)] public string? Alergias { get; set; }
}

/// <summary>Quién consulta: el backend filtra por rol y por asignación en la consulta (propuesta, sección 2).</summary>
public sealed record Consultante(int UsuarioId, bool EsDentista);

/// <summary>El paciente tiene citas próximas activas: no se da de baja (409).</summary>
public class PacienteConCitasException : InvalidOperationException
{
    public PacienteConCitasException() : base("Tiene citas próximas pendientes o confirmadas; cancélalas o reagéndalas antes de darlo de baja.") { }
}

public interface IPacientesPanelService
{
    Task<PaginaPacientes> ListarAsync(Consultante quien, string? buscar, int pagina, int tamano, CancellationToken ct = default);
    Task<PacienteDetalleDto?> DetalleAsync(Consultante quien, int id, CancellationToken ct = default);
    Task<PacienteDetalleDto> CrearAsync(Consultante quien, PacienteEdicionDto dto, CancellationToken ct = default);
    Task<PacienteDetalleDto?> EditarAsync(Consultante quien, int id, PacienteEdicionDto dto, CancellationToken ct = default);
    Task<PacienteDetalleDto?> CambiarAlergiasAsync(Consultante quien, int id, string? alergias, CancellationToken ct = default);
    Task<bool> DarDeBajaAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Pacientes para el panel (Fase 2b; propuesta aprobada solo para la demo con datos ficticios). Cada respuesta con
/// alergias deja una fila "lectura_sensible" en la bitácora sin valores ni texto de búsqueda; si la bitácora falla,
/// la respuesta falla.
/// </summary>
public class PacientesPanelService : IPacientesPanelService
{
    public const int TamanoPorDefecto = 25;
    public const int TamanoMaximo = 100;
    public const string LecturaSensible = AccionesAuditoria.LecturaSensible;
    public static readonly string[] Sexos = { "femenino", "masculino", "otro", "no_especifica" };

    private readonly DentissaDbContext _db;
    private readonly IActorActual _actor;
    private readonly IRelojClinica _reloj;
    private readonly TimeProvider _tiempo;

    public PacientesPanelService(DentissaDbContext db, IActorActual actor, IRelojClinica reloj, TimeProvider tiempo)
    {
        _db = db;
        _actor = actor;
        _reloj = reloj;
        _tiempo = tiempo;
    }

    private DateTime Ahora => _tiempo.GetUtcNow().UtcDateTime;

    private async Task<int?> DentistaDeAsync(Consultante quien, CancellationToken ct) =>
        quien.EsDentista
            ? await _db.Dentistas.Where(d => d.UsuarioId == quien.UsuarioId).Select(d => (int?)d.Id).FirstOrDefaultAsync(ct) ?? -1
            : null;

    // "Asignado" (decisión provisional 1): al menos una cita no cancelada con ese dentista
    private IQueryable<Paciente> Visibles(int? dentista) =>
        dentista is { } d
            ? _db.Pacientes.Where(p => p.Citas.Any(c => c.DentistaId == d && c.Estado != EstadosCita.Cancelada))
            : _db.Pacientes;

    private static string Escapar(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<PaginaPacientes> ListarAsync(Consultante quien, string? buscar, int pagina, int tamano, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamano = tamano <= 0 ? TamanoPorDefecto : Math.Min(tamano, TamanoMaximo);
        var dentista = await DentistaDeAsync(quien, ct);
        var q = Visibles(dentista).AsNoTracking();

        var termino = buscar?.Trim();
        if (!string.IsNullOrEmpty(termino))
        {
            if (termino.Length < 2) throw new ArgumentException("Escribe al menos 2 caracteres para buscar");
            var patron = $"%{Escapar(termino)}%";
            var digitos = Regex.Replace(termino, "[^0-9]", "");
            // El dentista no ve teléfonos (decisión provisional 4), así que tampoco busca por teléfono
            var porTelefono = !quien.EsDentista && digitos.Length >= 2;
            q = q.Where(p =>
                EF.Functions.ILike(EF.Functions.Unaccent(p.Nombre + " " + p.Apellido), EF.Functions.Unaccent(patron))
                || EF.Functions.ILike(EF.Functions.Unaccent(p.Apellido + " " + p.Nombre), EF.Functions.Unaccent(patron))
                || (porTelefono && p.Telefono != null
                    && p.Telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace("+", "").Contains(digitos)));
        }

        var total = await q.CountAsync(ct);
        var filas = await q.OrderBy(p => p.Apellido).ThenBy(p => p.Nombre).ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(p => new { p.Id, p.Nombre, p.Apellido, p.Telefono, ConCuenta = p.UsuarioId != null, p.Alergias })
            .ToListAsync(ct);
        var citas = await CitasDeAsync(filas.Select(f => f.Id).ToList(), ct);

        var elementos = filas.Select(f => new PacienteListaDto(
            f.Id, f.Nombre, f.Apellido, quien.EsDentista ? null : f.Telefono, f.ConCuenta ? "con-cuenta" : "invitado", f.Alergias,
            Ultima(citas, f.Id), Proximas(citas, f.Id).FirstOrDefault())).ToList();

        if (elementos.Count > 0)
            await RegistrarLecturaAsync(null, new { campos = new[] { "alergias" }, ids = elementos.Select(e => e.Id), pagina, busqueda = !string.IsNullOrEmpty(termino) }, ct);
        return new PaginaPacientes(elementos, total, pagina, tamano);
    }

    public async Task<PacienteDetalleDto?> DetalleAsync(Consultante quien, int id, CancellationToken ct = default)
    {
        var dentista = await DentistaDeAsync(quien, ct);
        var p = await Visibles(dentista).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        var dto = await ADetalleAsync(p, quien, ct);
        await RegistrarLecturaAsync(id, new { campos = new[] { "alergias" } }, ct);
        return dto;
    }

    public async Task<PacienteDetalleDto> CrearAsync(Consultante quien, PacienteEdicionDto dto, CancellationToken ct = default)
    {
        var p = new Paciente { Origen = OrigenesCita.Recepcion };
        Aplicar(p, dto);
        _db.Pacientes.Add(p);
        await _db.SaveChangesAsync(ct);
        return await ADetalleAsync(p, quien, ct);
    }

    public async Task<PacienteDetalleDto?> EditarAsync(Consultante quien, int id, PacienteEdicionDto dto, CancellationToken ct = default)
    {
        var p = await _db.Pacientes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        Aplicar(p, dto);
        await _db.SaveChangesAsync(ct);
        return await ADetalleAsync(p, quien, ct);
    }

    public async Task<PacienteDetalleDto?> CambiarAlergiasAsync(Consultante quien, int id, string? alergias, CancellationToken ct = default)
    {
        var p = await _db.Pacientes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        if (alergias?.Length > 1000) throw new ArgumentException("Las alergias no pueden pasar de 1000 caracteres");
        p.Alergias = string.IsNullOrWhiteSpace(alergias) ? null : alergias.Trim();
        await _db.SaveChangesAsync(ct);
        return await ADetalleAsync(p, quien, ct);
    }

    // Baja lógica: nada se borra físicamente; una ficha con citas próximas activas no se da de baja
    public async Task<bool> DarDeBajaAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Pacientes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return false;
        var ahora = Ahora;
        if (await _db.Citas.AnyAsync(c => c.PacienteId == id && c.Inicio > ahora && EstadosCita.Activos.Contains(c.Estado), ct))
            throw new PacienteConCitasException();
        p.EliminadoEn = ahora;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Validación del backend (autoridad); los CHECK de la base quedan como respaldo.</summary>
    public static void Aplicar(Paciente p, PacienteEdicionDto d)
    {
        var nombre = d.Nombre?.Trim();
        var apellido = d.Apellido?.Trim();
        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido)) throw new ArgumentException("El nombre y el apellido son obligatorios");
        var telefono = string.IsNullOrWhiteSpace(d.Telefono) ? null : d.Telefono.Trim();
        if (telefono is not null && Regex.Replace(telefono, "[^0-9]", "").Length is < 10 or > 15)
            throw new ArgumentException("El teléfono debe tener de 10 a 15 dígitos");
        var correo = string.IsNullOrWhiteSpace(d.Correo) ? null : d.Correo.Trim();
        if (correo is not null && !new EmailAddressAttribute().IsValid(correo)) throw new ArgumentException("Correo inválido");
        if (telefono is null && correo is null) throw new ArgumentException("Indica al menos un teléfono o un correo");
        var sexo = string.IsNullOrWhiteSpace(d.Sexo) ? null : d.Sexo.Trim().ToLowerInvariant();
        if (sexo is not null && !Sexos.Contains(sexo)) throw new ArgumentException("Sexo inválido");
        if (d.FechaNacimiento is { } f && (f > DateOnly.FromDateTime(DateTime.UtcNow) || f.Year < 1900))
            throw new ArgumentException("Fecha de nacimiento inválida");
        if (d.Alergias?.Length > 1000) throw new ArgumentException("Las alergias no pueden pasar de 1000 caracteres");

        p.Nombre = nombre;
        p.Apellido = apellido;
        p.Telefono = telefono;
        p.Correo = correo;
        p.Sexo = sexo;
        p.FechaNacimiento = d.FechaNacimiento;
        p.Alergias = string.IsNullOrWhiteSpace(d.Alergias) ? null : d.Alergias.Trim();
    }

    private sealed record CitaPaciente(int Id, int PacienteId, DateTime Inicio, string Estado, string Servicio);

    private async Task<List<CitaPaciente>> CitasDeAsync(List<int> pacientes, CancellationToken ct) =>
        await _db.Citas.AsNoTracking().IgnoreQueryFilters([DentissaDbContext.FiltroBorradoLogico])
            .Where(c => pacientes.Contains(c.PacienteId) && c.Estado != EstadosCita.Cancelada)
            .Select(c => new CitaPaciente(c.Id, c.PacienteId, c.Inicio, c.Estado, c.Servicio.Nombre))
            .ToListAsync(ct);

    private CitaResumenDto Resumen(CitaPaciente c)
    {
        var (f, h) = _reloj.ALocal(c.Inicio);
        return new CitaResumenDto(c.Id, f.ToString("yyyy-MM-dd"), h.ToString("HH:mm"), c.Estado, c.Servicio);
    }

    private CitaResumenDto? Ultima(List<CitaPaciente> citas, int paciente) =>
        citas.Where(c => c.PacienteId == paciente && c.Inicio <= Ahora).OrderByDescending(c => c.Inicio).Select(Resumen).FirstOrDefault();

    private IEnumerable<CitaResumenDto> Proximas(List<CitaPaciente> citas, int paciente) =>
        citas.Where(c => c.PacienteId == paciente && c.Inicio > Ahora).OrderBy(c => c.Inicio).Select(Resumen);

    private async Task<PacienteDetalleDto> ADetalleAsync(Paciente p, Consultante quien, CancellationToken ct)
    {
        var citas = await CitasDeAsync([p.Id], ct);
        return new PacienteDetalleDto(p.Id, p.Nombre, p.Apellido, quien.EsDentista ? null : p.Telefono, p.Correo, p.FechaNacimiento, p.Sexo,
            p.UsuarioId != null ? "con-cuenta" : "invitado", p.Alergias, Ultima(citas, p.Id), Proximas(citas, p.Id).Take(10).ToList());
    }

    // Sin valores sensibles ni el texto de búsqueda: solo los campos leídos y los ids (propuesta, sección 3)
    private async Task RegistrarLecturaAsync(int? pacienteId, object detalle, CancellationToken ct)
    {
        _db.Auditoria.Add(new Auditoria
        {
            OcurridoEn = Ahora,
            UsuarioId = _actor.UsuarioId,
            UsuarioCorreo = _actor.Correo,
            Ip = _actor.Ip,
            Accion = LecturaSensible,
            Entidad = "pacientes",
            EntidadId = pacienteId?.ToString(),
            Detalle = JsonBitacora.Serializar(detalle),
        });
        await _db.SaveChangesAsync(ct);
    }
}
