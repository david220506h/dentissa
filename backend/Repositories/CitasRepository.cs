using backend.Entities;
using backend.Entities.Configuraciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Repositories
{
    public class CitasRepository : ICitasRepository
    {
        private readonly DentissaDbContext _context;

        public CitasRepository(DentissaDbContext context)
        {
            _context = context;
        }

        // Una cita nunca desaparece del listado, el historial ni mi-agenda aunque su
        // paciente, dentista o servicio esté eliminado lógicamente: se ignora solo el
        // filtro de borrado lógico y el DTO marca la relación eliminada.
        private IQueryable<Cita> CitasConRelaciones() => _context.Citas
            .IgnoreQueryFilters([DentissaDbContext.FiltroBorradoLogico])
            .Include(c => c.Paciente)
            .Include(c => c.Dentista).ThenInclude(d => d.Usuario)
            .Include(c => c.Servicio)
            .Include(c => c.Oferta);

        public async Task<List<Cita>> GetAllAsync()
            => await CitasConRelaciones().OrderBy(c => c.Inicio).ToListAsync();

        public async Task<List<Cita>> GetFiltradasAsync(DateTime? desdeUtc, DateTime? hastaUtc, int? dentistaId, string? estado)
        {
            var q = CitasConRelaciones();
            if (desdeUtc is { } d) q = q.Where(c => c.Inicio >= d);
            if (hastaUtc is { } h) q = q.Where(c => c.Inicio < h);
            if (dentistaId is { } id) q = q.Where(c => c.DentistaId == id);
            if (estado is not null) q = q.Where(c => c.Estado == estado);
            return await q.OrderBy(c => c.Inicio).ToListAsync();
        }

        public async Task<List<Cita>> GetConDentistaInactivoAsync(DateTime desdeUtc)
            => await CitasConRelaciones()
                .Where(c => c.Inicio >= desdeUtc && EstadosCita.Activos.Contains(c.Estado)
                            && (!c.Dentista.Activo || c.Dentista.EliminadoEn != null))
                .OrderBy(c => c.Inicio)
                .ToListAsync();

        public async Task<Cita?> GetByIdAsync(int id)
            => await CitasConRelaciones().FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Cita> CreateAsync(Cita cita)
        {
            _context.Citas.Add(cita);
            await GuardarAsync();
            return cita;
        }

        public async Task UpdateAsync(Cita cita)
        {
            await GuardarAsync();
        }

        private const int IntentosPorInterbloqueo = 3;

        // La restricción citas_sin_traslape_dentista es la autoridad: el backend no
        // repite la comprobación, solo traduce la violación a un mensaje claro.
        // Dos inserciones simultáneas que chocan pueden esperar una a la otra dentro de
        // la restricción de exclusión; PostgreSQL aborta una con 40P01 (interbloqueo).
        // Se reintenta: el reintento ya ve la cita confirmada y recibe 23P01.
        private async Task GuardarAsync()
        {
            for (var intento = 1; ; intento++)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return;
                }
                catch (Exception ex) when (ErrorDePostgres(ex) is
                {
                    SqlState: PostgresErrorCodes.ExclusionViolation,
                    ConstraintName: CitaConfiguracion.RestriccionSinTraslape
                })
                {
                    throw new HorarioOcupadoException(ex);
                }
                catch (Exception ex) when (intento < IntentosPorInterbloqueo &&
                                           ErrorDePostgres(ex)?.SqlState == PostgresErrorCodes.DeadlockDetected)
                {
                    // reintenta con los mismos cambios (EF no los aceptó al fallar)
                }
            }
        }

        // Npgsql envuelve los errores transitorios (como 40P01) en otra excepción
        private static PostgresException? ErrorDePostgres(Exception? ex)
        {
            for (; ex != null; ex = ex.InnerException)
                if (ex is PostgresException pg) return pg;
            return null;
        }

        // Vigencia inclusiva en fechas locales de la clínica
        public async Task<Oferta?> GetOfertaVigenteAsync(int servicioId, DateOnly fechaLocal)
            => await _context.Ofertas
                .Where(o => o.ServicioId == servicioId && o.Activo &&
                            o.FechaInicio <= fechaLocal && o.FechaFin >= fechaLocal)
                .OrderByDescending(o => o.Descuento)
                .FirstOrDefaultAsync();

        public async Task<Servicio?> GetServicioActivoAsync(int servicioId)
            => await _context.Servicios.FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo);

        public async Task<Paciente?> GetPacienteAsync(int pacienteId)
            => await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        public async Task<Dentista?> GetDentistaActivoAsync(int dentistaId)
            => await _context.Dentistas.FirstOrDefaultAsync(d => d.Id == dentistaId && d.Activo);

        public async Task<List<Paciente>> GetPacientesAsync()
            => await _context.Pacientes
                .OrderBy(p => p.Nombre).ThenBy(p => p.Apellido)
                .ToListAsync();

        public async Task<List<Dentista>> GetDentistasActivosAsync()
            => await _context.Dentistas
                .Include(d => d.Usuario)
                .Where(d => d.Activo)
                .ToListAsync();

        public async Task<List<Servicio>> GetServiciosActivosAsync()
            => await _context.Servicios
                .Where(s => s.Activo)
                .OrderBy(s => s.Nombre)
                .ToListAsync();

        public async Task<Dentista?> GetDentistaPorUsuarioAsync(int usuarioId)
            => await _context.Dentistas
                .IgnoreQueryFilters([DentissaDbContext.FiltroBorradoLogico])
                .FirstOrDefaultAsync(d => d.UsuarioId == usuarioId);

        // Citas que empiezan en [desde, hasta)
        public async Task<List<Cita>> GetAgendaDentistaAsync(int dentistaId, DateTime desdeUtc, DateTime hastaUtc)
            => await CitasConRelaciones()
                .Where(c => c.DentistaId == dentistaId && c.Inicio >= desdeUtc && c.Inicio < hastaUtc)
                .OrderBy(c => c.Inicio)
                .ToListAsync();
    }
}
