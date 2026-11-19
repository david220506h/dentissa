using backend.Entities;

namespace backend.Repositories
{
    public interface ICitasRepository
    {
        Task<List<Cita>> GetAllAsync();
        Task<Cita?> GetByIdAsync(int id);
        Task<List<string>> GetHorariosDisponiblesAsync(DateOnly fecha);
        Task<bool> TrabajadorOcupadoAsync(int trabajadorId, DateOnly fecha, TimeOnly hora, int? excluirId = null);
        Task<Cita> CreateAsync(Cita cita);
        Task UpdateAsync(Cita cita);
        Task DeleteAsync(Cita cita);
        Task<Oferta?> GetOfertaActivaAsync(int servicioId);
        Task<Servicio?> GetServicioAsync(int servicioId);
        Task<List<Cliente>> GetClientesAsync();
        Task<List<Trabajadore>> GetTrabajadoresAsync();
        Task<List<Servicio>> GetServiciosAsync();
    }
}