using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Services.Agenda;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.Correo
{
    /// <summary>Referencias que guarda una fila de la bandeja (en notificaciones.mensaje): nada personal ni tokens.</summary>
    public sealed record ReferenciasCorreo(DateTime? InicioAnteriorUtc = null);

    /// <summary>
    /// Bandeja de salida sobre notificaciones (diseño de la agenda, 7.3). Encolar solo agrega la fila al contexto: se
    /// guarda en el mismo SaveChanges (y la misma transacción) que el cambio que la provoca.
    /// </summary>
    public static class BandejaCorreo
    {
        public static Notificacion Encolar(DentissaDbContext db, Cita cita, string plantilla, string tipo, DateTime ahora, ReferenciasCorreo? refs = null)
        {
            var fila = new Notificacion
            {
                PacienteId = cita.PacienteId,
                Cita = cita,
                Tipo = tipo,
                Canal = CanalesNotificacion.Correo,
                Plantilla = plantilla,
                Estado = EstadosNotificacion.Pendiente,
                ProximoIntentoEn = ahora,
                CreadoEn = ahora,
                Mensaje = JsonSerializer.Serialize(refs ?? new ReferenciasCorreo()),
            };
            db.Notificaciones.Add(fila);
            return fila;
        }
    }

    /// <summary>Despierta al trabajador tras un commit, sin esperar al siguiente ciclo.</summary>
    public sealed class AvisoBandeja
    {
        private readonly SemaphoreSlim _senal = new(0, 1);

        public void Avisar()
        {
            try { if (_senal.CurrentCount == 0) _senal.Release(); }
            catch (SemaphoreFullException) { }
        }

        public Task EsperarAsync(TimeSpan maximo, CancellationToken ct) => _senal.WaitAsync(maximo, ct);
    }

    public interface IProcesadorBandeja
    {
        /// <summary>Reclama un lote, lo envía y registra el resultado. Devuelve cuántas filas procesó.</summary>
        Task<int> ProcesarAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Trabajo de la bandeja: reclama con FOR UPDATE SKIP LOCKED y arrendamiento, lee el correo vigente, emite los
    /// tokens al enviar (solo se guarda su hash), renderiza y envía. Entrega "al menos una vez": si el proceso cae
    /// entre el envío y el registro, el arrendamiento vence y la fila se reintenta.
    /// </summary>
    public class ProcesadorBandeja : IProcesadorBandeja
    {
        // Esperas entre intentos (7.3); a la 6.ª falla, "fallida" y aviso interno
        public static readonly TimeSpan[] Esperas =
            { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1), TimeSpan.FromHours(6) };

        public const string SinCorreo = "SinCorreo";
        public const string Antigua = "Antigua";
        public const string CitaPasada = "CitaPasada";

        private static readonly string[] ConCitaFutura = { PlantillasCorreo.SolicitudRecibida, PlantillasCorreo.Confirmacion, PlantillasCorreo.Recordatorio, PlantillasCorreo.Cambio };

        private readonly IServiceScopeFactory _scopes;
        private readonly IEmailSender _sender;
        private readonly ITokensCitaService _tokens;
        private readonly IRelojClinica _reloj;
        private readonly TimeProvider _tiempo;
        private readonly IOptionsMonitor<OpcionesCorreo> _correo;
        private readonly IOptionsMonitor<OpcionesClinica> _clinica;
        private readonly ILogger<ProcesadorBandeja> _log;

        public ProcesadorBandeja(IServiceScopeFactory scopes, IEmailSender sender, ITokensCitaService tokens, IRelojClinica reloj,
            TimeProvider tiempo, IOptionsMonitor<OpcionesCorreo> correo, IOptionsMonitor<OpcionesClinica> clinica, ILogger<ProcesadorBandeja> log)
        {
            _scopes = scopes;
            _sender = sender;
            _tokens = tokens;
            _reloj = reloj;
            _tiempo = tiempo;
            _correo = correo;
            _clinica = clinica;
            _log = log;
        }

        private DateTime Ahora => _tiempo.GetUtcNow().UtcDateTime;

        public async Task<int> ProcesarAsync(CancellationToken ct = default)
        {
            var ids = await ReclamarAsync(ct);
            foreach (var id in ids)
                await ProcesarUnaAsync(id, ct);
            return ids.Count;
        }

        private async Task<List<long>> ReclamarAsync(CancellationToken ct)
        {
            var o = _correo.CurrentValue;
            var ahora = Ahora;
            var arrendamientoVencido = ahora.AddMinutes(-o.ArrendamientoMinutos);
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DentissaDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var ids = await db.Database.SqlQuery<long>($"""
                SELECT id AS "Value" FROM notificaciones
                WHERE canal = 'correo'
                  AND ((estado = 'pendiente' AND proximo_intento_en <= {ahora})
                    OR (estado = 'procesando' AND procesando_desde <= {arrendamientoVencido}))
                ORDER BY proximo_intento_en, id
                LIMIT {o.Lote}
                FOR UPDATE SKIP LOCKED
                """).ToListAsync(ct);
            if (ids.Count > 0)
                await db.Notificaciones.Where(n => ids.Contains(n.Id)).ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.Estado, EstadosNotificacion.Procesando)
                    .SetProperty(n => n.ProcesandoDesde, ahora), ct);
            await tx.CommitAsync(ct);
            return ids;
        }

        private async Task ProcesarUnaAsync(long id, CancellationToken ct)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DentissaDbContext>();
            var n = await db.Notificaciones.IgnoreQueryFilters()
                .Include(x => x.Paciente).Include(x => x.Usuario)
                .Include(x => x.Cita).ThenInclude(c => c!.Servicio)
                .SingleAsync(x => x.Id == id, ct);
            var ahora = Ahora;
            n.Intentos++;

            if (n.CreadoEn < ahora.AddHours(-_correo.CurrentValue.MaxAntiguedadHoras))
            {
                await TerminarAsync(db, n, EstadosNotificacion.Descartada, Antigua, ct);
                return;
            }
            if (n.Cita is { } futura && ConCitaFutura.Contains(n.Plantilla!) && futura.Inicio <= ahora)
            {
                await TerminarAsync(db, n, EstadosNotificacion.Descartada, CitaPasada, ct);
                return;
            }

            var para = n.Paciente?.Correo ?? n.Usuario?.Correo;
            if (string.IsNullOrWhiteSpace(para))
            {
                AvisarRecepcion(db, n, $"Sin correo: {PlantillasCorreoRender.Titulo(n.Plantilla!)}",
                    "No se pudo avisar por correo porque la ficha no tiene correo. Avisa por teléfono si hace falta.", ahora);
                await TerminarAsync(db, n, EstadosNotificacion.Descartada, SinCorreo, ct);
                return;
            }

            try
            {
                var datos = await DatosAsync(db, n, ct);
                var (asunto, texto, html) = PlantillasCorreoRender.Renderizar(n.Plantilla!, datos, _clinica.CurrentValue);
                await _sender.EnviarAsync(new MensajeCorreo(para, asunto, texto, html), ct);
                n.Asunto = asunto;
                n.Enviada = true;
                n.EnviadaEn = Ahora;
                await TerminarAsync(db, n, EstadosNotificacion.Enviada, null, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Solo la clase del error: el mensaje puede traer direcciones o datos del servidor
                var error = ex.GetType().Name;
                _log.LogWarning("Correo {Id} no enviado (intento {Intento}): {Error}", n.Id, n.Intentos, error);
                if (n.Intentos > Esperas.Length)
                {
                    AvisarRecepcion(db, n, $"Correo no enviado: {PlantillasCorreoRender.Titulo(n.Plantilla!)}",
                        "El correo no se pudo enviar tras varios intentos. Avisa por teléfono si hace falta.", ahora);
                    await TerminarAsync(db, n, EstadosNotificacion.Fallida, error, ct);
                }
                else
                {
                    n.ProximoIntentoEn = ahora + Esperas[n.Intentos - 1];
                    await TerminarAsync(db, n, EstadosNotificacion.Pendiente, error, ct);
                }
            }
        }

        private static async Task TerminarAsync(DentissaDbContext db, Notificacion n, string estado, string? error, CancellationToken ct)
        {
            n.Estado = estado;
            n.ErrorEnvio = error;
            n.ProcesandoDesde = null;
            await db.SaveChangesAsync(CancellationToken.None);
        }

        private async Task<DatosCorreo> DatosAsync(DentissaDbContext db, Notificacion n, CancellationToken ct)
        {
            if (n.Cita is not { } cita) return new DatosCorreo(null, null, null);
            var (fecha, hora) = _reloj.ALocal(cita.Inicio);
            var refs = JsonSerializer.Deserialize<ReferenciasCorreo>(n.Mensaje) ?? new ReferenciasCorreo();
            (DateOnly Fecha, TimeOnly Hora)? anterior = refs.InicioAnteriorUtc is { } a ? _reloj.ALocal(a) : null;

            string? confirmar = null, gestionar = null;
            var url = _clinica.CurrentValue.UrlPublica?.Trim().TrimEnd('/');
            if (!string.IsNullOrEmpty(url))
            {
                if (n.Plantilla == PlantillasCorreo.SolicitudRecibida)
                    confirmar = await EnlaceAsync(db, url, cita.Id, PropositosToken.Confirmar, ct);
                if (n.Plantilla is PlantillasCorreo.SolicitudRecibida or PlantillasCorreo.Confirmacion or PlantillasCorreo.Recordatorio or PlantillasCorreo.Cambio)
                    gestionar = await EnlaceAsync(db, url, cita.Id, PropositosToken.Gestionar, ct);
            }
            return new DatosCorreo(cita.Servicio?.Nombre, fecha, hora, anterior?.Fecha, anterior?.Hora, confirmar, gestionar);
        }

        // El token va en el fragmento (#t=): no llega al servidor, ni en Referer, ni en registros (5.3)
        private async Task<string?> EnlaceAsync(DentissaDbContext db, string url, int citaId, string proposito, CancellationToken ct)
        {
            try
            {
                return $"{url}/cita#t={await _tokens.EmitirAsync(db, citaId, proposito, ct)}";
            }
            catch (EnlaceNoDisponibleException)
            {
                return null;
            }
        }

        // Aviso interno a recepción: asistentes activas; si no hay, administración
        private static void AvisarRecepcion(DentissaDbContext db, Notificacion n, string asunto, string mensaje, DateTime ahora)
        {
            var destinatarios = db.Usuarios.AsNoTracking().Where(u => u.Activo && u.Rol == Roles.Asistente).Select(u => u.Id).ToList();
            if (destinatarios.Count == 0)
                destinatarios = db.Usuarios.AsNoTracking().Where(u => u.Activo && u.Rol == Roles.Admin).Select(u => u.Id).ToList();
            foreach (var usuario in destinatarios)
                db.Notificaciones.Add(new Notificacion
                {
                    UsuarioId = usuario,
                    CitaId = n.CitaId,
                    Tipo = TiposNotificacion.EnvioFallido,
                    Canal = CanalesNotificacion.Interna,
                    Asunto = asunto,
                    Mensaje = n.CitaId is { } c ? $"{mensaje} (cita #{c})" : mensaje,
                    CreadoEn = ahora,
                });
        }
    }

    /// <summary>Trabajador en segundo plano: cada Correo:IntervaloSegundos o al recibir un aviso tras un commit.</summary>
    public class EnvioCorreosService : BackgroundService
    {
        private readonly IProcesadorBandeja _procesador;
        private readonly AvisoBandeja _aviso;
        private readonly IOptionsMonitor<OpcionesCorreo> _opciones;
        private readonly ILogger<EnvioCorreosService> _log;

        public EnvioCorreosService(IProcesadorBandeja procesador, AvisoBandeja aviso, IOptionsMonitor<OpcionesCorreo> opciones, ILogger<EnvioCorreosService> log)
        {
            _procesador = procesador;
            _aviso = aviso;
            _opciones = opciones;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Mientras haya lotes llenos, sigue sin esperar
                    while (await _procesador.ProcesarAsync(ct) >= _opciones.CurrentValue.Lote) { }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError("Bandeja de correo: {Error}", ex.GetType().Name);
                }
                try
                {
                    await _aviso.EsperarAsync(TimeSpan.FromSeconds(Math.Max(1, _opciones.CurrentValue.IntervaloSegundos)), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
