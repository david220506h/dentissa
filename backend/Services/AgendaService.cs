using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    // Horarios de la clínica a partir de configuracion_agenda. La lógica completa de
    // disponibilidad (anticipación, liberación de solicitudes, servicios de varios
    // slots al ofrecer horarios) es de la fase de agenda.
    public interface IAgendaService
    {
        Task<ConfiguracionAgenda> ObtenerConfiguracionAsync();
        Task<List<TimeOnly>> HorariosDelDiaAsync(DateOnly fecha);
        Task<(DateTime Inicio, DateTime Fin)> CalcularIntervaloAsync(DateOnly fecha, TimeOnly hora, short duracionSlots);
        Task<List<string>> HorariosDisponiblesAsync(DateOnly fecha, int? dentistaId);
    }

    public class AgendaNoConfiguradaException : InvalidOperationException
    {
        public AgendaNoConfiguradaException()
            : base("La configuración de agenda no existe. Se crea al arrancar la aplicación.") { }
    }

    public class AgendaService : IAgendaService
    {
        private readonly DentissaDbContext _db;
        private readonly IRelojClinica _reloj;

        public AgendaService(DentissaDbContext db, IRelojClinica reloj)
        {
            _db = db;
            _reloj = reloj;
        }

        public async Task<ConfiguracionAgenda> ObtenerConfiguracionAsync()
        {
            return await _db.ConfiguracionAgenda
                .AsNoTracking()
                .Include(c => c.Dias)
                .Include(c => c.Descansos)
                .FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico)
                ?? throw new AgendaNoConfiguradaException();
        }

        // Inicio de cada horario del día (hora local): de la apertura al cierre en pasos de
        // duracion_slot_minutos, sin traslapar descansos ni cierres.
        public async Task<List<TimeOnly>> HorariosDelDiaAsync(DateOnly fecha)
        {
            var config = await ObtenerConfiguracionAsync();
            var diaSemana = (short)(fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek);

            var dia = config.Dias.FirstOrDefault(d => d.DiaSemana == diaSemana);
            if (dia is not { Laborable: true, HoraApertura: { } apertura, HoraCierre: { } cierre })
                return new List<TimeOnly>();

            var cierres = await _db.AgendaCierres.AsNoTracking()
                .Where(c => c.Fecha == fecha)
                .ToListAsync();
            if (cierres.Any(c => c.HoraInicio == null))
                return new List<TimeOnly>();

            var bloqueos = config.Descansos
                .Where(d => d.DiaSemana == null || d.DiaSemana == diaSemana)
                .Select(d => (Inicio: d.HoraInicio, Fin: d.HoraFin))
                .Concat(cierres.Select(c => (Inicio: c.HoraInicio!.Value, Fin: c.HoraFin!.Value)))
                .ToList();

            var duracion = TimeSpan.FromMinutes(config.DuracionSlotMinutos);
            var horarios = new List<TimeOnly>();
            var inicio = apertura;
            // Se recorre en TimeSpan para no dar la vuelta a medianoche
            while (inicio.ToTimeSpan() + duracion <= cierre.ToTimeSpan())
            {
                var fin = inicio.Add(duracion);
                var traslapa = bloqueos.Any(b => inicio < b.Fin && b.Inicio < fin);
                if (!traslapa)
                    horarios.Add(inicio);
                inicio = fin;
            }
            return horarios;
        }

        public async Task<(DateTime Inicio, DateTime Fin)> CalcularIntervaloAsync(DateOnly fecha, TimeOnly hora, short duracionSlots)
        {
            var config = await ObtenerConfiguracionAsync();
            var inicio = _reloj.AUtc(fecha, hora);
            return (inicio, inicio.AddMinutes(config.DuracionSlotMinutos * duracionSlots));
        }

        // Con dentista: horarios en que ese dentista no tiene cita activa.
        // Sin dentista: horarios en que queda al menos un dentista activo libre.
        public async Task<List<string>> HorariosDisponiblesAsync(DateOnly fecha, int? dentistaId)
        {
            var horarios = await HorariosDelDiaAsync(fecha);
            if (horarios.Count == 0) return new List<string>();

            var config = await ObtenerConfiguracionAsync();
            var duracion = TimeSpan.FromMinutes(config.DuracionSlotMinutos);
            var desde = _reloj.AUtc(fecha, TimeOnly.MinValue);
            var hasta = _reloj.AUtc(fecha.AddDays(1), TimeOnly.MinValue);

            var citas = await _db.Citas.AsNoTracking()
                .Where(c => EstadosCita.Activos.Contains(c.Estado) && c.Inicio < hasta && c.Fin > desde)
                .Where(c => dentistaId == null || c.DentistaId == dentistaId)
                .Select(c => new { c.DentistaId, c.Inicio, c.Fin })
                .ToListAsync();

            var capacidad = dentistaId != null
                ? 1
                : await _db.Dentistas.CountAsync(d => d.Activo);

            return horarios
                .Where(h =>
                {
                    var inicio = _reloj.AUtc(fecha, h);
                    var fin = inicio + duracion;
                    var ocupados = citas
                        .Where(c => c.Inicio < fin && inicio < c.Fin)
                        .Select(c => c.DentistaId)
                        .Distinct()
                        .Count();
                    return ocupados < capacidad;
                })
                .Select(h => h.ToString("HH:mm"))
                .ToList();
        }
    }
}
