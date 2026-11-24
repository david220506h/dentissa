using backend.Entities;

namespace backend.Repositories
{
    public interface ICitasRepository
    {
        Task<List<Cita>> GetAllAsync();

        // Citas futuras activas cuyo dentista está inactivo o eliminado (diseño, 3.5)
        Task<List<Cita>> GetConDentistaInactivoAsync(DateTime desdeUtc);
        Task<Cita?> GetByIdAsync(int id);
        Task<Cita> CreateAsync(Cita cita);
        Task UpdateAsync(Cita cita);
        Task<Oferta?> GetOfertaVigenteAsync(int servicioId, DateOnly fechaLocal);
        Task<Servicio?> GetServicioActivoAsync(int servicioId);
        Task<Paciente?> GetPacienteAsync(int pacienteId);
        Task<Dentista?> GetDentistaActivoAsync(int dentistaId);
        Task<List<Paciente>> GetPacientesAsync();
        Task<List<Dentista>> GetDentistasActivosAsync();
        Task<List<Servicio>> GetServiciosActivosAsync();
        Task<Dentista?> GetDentistaPorUsuarioAsync(int usuarioId);
        Task<List<Cita>> GetAgendaDentistaAsync(int dentistaId, DateTime desdeUtc, DateTime hastaUtc);
    }

    // La base rechazó la cita por traslape con otra activa del mismo dentista (23P01)
    public class HorarioOcupadoException : Exception
    {
        public const string Mensaje = "El dentista ya tiene una cita en ese horario.";

        public HorarioOcupadoException(Exception interna) : base(Mensaje, interna) { }

        public const string MensajeSinDentistas = "No hay dentistas libres en ese horario.";

        public HorarioOcupadoException(string mensaje = Mensaje) : base(mensaje) { }
    }
}
