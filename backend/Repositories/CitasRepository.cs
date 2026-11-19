using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class CitasRepository : ICitasRepository
    {
        private readonly DentissaDbContext _context;

        public CitasRepository(DentissaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cita>> GetAllAsync()
        {
            return await _context.Citas
                .Include(c => c.Cliente).ThenInclude(cl => cl.Usuario)
                .Include(c => c.Trabajador).ThenInclude(t => t.Usuario)
                .Include(c => c.Servicio)
                .Include(c => c.Oferta)
                .ToListAsync();
        }

        public async Task<Cita?> GetByIdAsync(int id)
        {
            return await _context.Citas
                .Include(c => c.Cliente).ThenInclude(cl => cl.Usuario)
                .Include(c => c.Trabajador).ThenInclude(t => t.Usuario)
                .Include(c => c.Servicio)
                .Include(c => c.Oferta)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<string>> GetHorariosDisponiblesAsync(DateOnly fecha)
        {
            // Generar horarios de 8:00 a 19:00 cada 30 min
            var todos = new List<TimeOnly>();
            var h = new TimeOnly(8, 0);
            while (h <= new TimeOnly(19, 0))
            {
                todos.Add(h);
                h = h.AddMinutes(30);
            }

            // Horarios ocupados en esa fecha
            var ocupados = await _context.Citas
                .Where(c => c.Fecha == fecha && c.Estado != "cancelada")
                .Select(c => c.Hora)
                .ToListAsync();

            return todos
                .Where(t => !ocupados.Contains(t))
                .Select(t => t.ToString("HH:mm"))
                .ToList();
        }

        public async Task<bool> TrabajadorOcupadoAsync(int trabajadorId, DateOnly fecha, TimeOnly hora, int? excluirId = null)
        {
            return await _context.Citas.AnyAsync(c =>
                c.TrabajadorId == trabajadorId &&
                c.Fecha        == fecha &&
                c.Hora         == hora &&
                c.Estado       != "cancelada" &&
                (excluirId == null || c.Id != excluirId));
        }

        public async Task<Cita> CreateAsync(Cita cita)
        {
            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();
            return cita;
        }

        public async Task UpdateAsync(Cita cita)
        {
            _context.Citas.Update(cita);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Cita cita)
        {
            _context.Citas.Remove(cita);
            await _context.SaveChangesAsync();
        }

        public async Task<Oferta?> GetOfertaActivaAsync(int servicioId)
        {
            return await _context.Ofertas.FirstOrDefaultAsync(o =>
                o.ServicioId  == servicioId &&
                o.Activa      == true &&
                o.FechaInicio <= DateTime.Now &&
                o.FechaFin    >= DateTime.Now);
        }

        public async Task<Servicio?> GetServicioAsync(int servicioId)
        {
            return await _context.Servicios.FindAsync(servicioId);
        }

        public async Task<List<Cliente>> GetClientesAsync()
        {
            return await _context.Clientes
                .Include(c => c.Usuario)
                .ToListAsync();
        }

        public async Task<List<Trabajadore>> GetTrabajadoresAsync()
        {
            return await _context.Trabajadores
                .Include(t => t.Usuario)
                .ToListAsync();
        }

        public async Task<List<Servicio>> GetServiciosAsync()
        {
            return await _context.Servicios
                .Where(s => s.Disponible == true)
                .ToListAsync();
        }
    }
}