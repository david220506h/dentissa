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

        private IQueryable<Cita> CitasConRelaciones() => _context.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Dentista).ThenInclude(d => d.Usuario)
            .Include(c => c.Servicio)
            .Include(c => c.Oferta);

        public async Task<List<Cita>> GetAllAsync()
            => await CitasConRelaciones().OrderBy(c => c.Inicio).ToListAsync();

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

        public async Task DeleteAsync(Cita cita)
        {
            _context.Citas.Remove(cita);
            await _context.SaveChangesAsync();
        }

        // La restricción citas_sin_traslape_dentista es la autoridad: el backend no
        // repite la comprobación, solo traduce la violación a un mensaje claro.
        private async Task GuardarAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation,
                ConstraintName: CitaConfiguracion.RestriccionSinTraslape
            })
            {
                throw new HorarioOcupadoException(ex);
            }
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
    }
}
