using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.Correo
{
    public interface IRecordatorios
    {
        /// <summary>Encola el recordatorio de las citas confirmadas que empiezan en 23 a 25 h. Devuelve cuántos creó.</summary>
        Task<int> CrearAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Recordatorio 24 h antes (decisión 14), solo de citas confirmadas. La ventana de 23 a 25 h tolera que el
    /// trabajador se atrase; el índice único parcial (cita_id) de los recordatorios impide duplicarlos.
    /// </summary>
    public class Recordatorios : IRecordatorios
    {
        private readonly DentissaDbContext _db;
        private readonly TimeProvider _tiempo;
        private readonly AvisoBandeja _aviso;

        public Recordatorios(DentissaDbContext db, TimeProvider tiempo, AvisoBandeja aviso)
        {
            _db = db;
            _tiempo = tiempo;
            _aviso = aviso;
        }

        public async Task<int> CrearAsync(CancellationToken ct = default)
        {
            var ahora = _tiempo.GetUtcNow().UtcDateTime;
            var (desde, hasta) = (ahora.AddHours(23), ahora.AddHours(25));
            var creados = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO notificaciones (paciente_id, cita_id, tipo, canal, plantilla, estado, intentos, proximo_intento_en, mensaje, leida, enviada, creado_en)
                SELECT c.paciente_id, c.id, 'recordatorio', 'correo', 'recordatorio', 'pendiente', 0, {ahora}, {"{}"}, false, false, {ahora}
                FROM citas c
                WHERE c.estado = 'confirmada' AND c.inicio >= {desde} AND c.inicio < {hasta}
                ON CONFLICT (cita_id) WHERE tipo = 'recordatorio' AND canal = 'correo' DO NOTHING
                """, ct);
            if (creados > 0) _aviso.Avisar();
            return creados;
        }
    }

    /// <summary>Cada hora (Correo:RecordatoriosMinutos). Solo corre con Correo:Habilitado.</summary>
    public class RecordatoriosService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<OpcionesCorreo> _opciones;
        private readonly ILogger<RecordatoriosService> _log;

        public RecordatoriosService(IServiceScopeFactory scopes, IOptionsMonitor<OpcionesCorreo> opciones, ILogger<RecordatoriosService> log)
        {
            _scopes = scopes;
            _opciones = opciones;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IRecordatorios>().CrearAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError("Recordatorios: {Error}", ex.GetType().Name);
                }
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _opciones.CurrentValue.RecordatoriosMinutos)), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
