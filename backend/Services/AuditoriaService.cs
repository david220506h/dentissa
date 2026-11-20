using backend.Entities;

namespace backend.Services
{
    // Registro explícito de eventos que no son cambios de datos (login, logout).
    // Los cambios de datos los registra AuditoriaInterceptor.
    public interface IAuditoriaService
    {
        Task RegistrarAsync(string accion, string entidad, string? entidadId,
                            object? detalle = null, int? usuarioId = null, string? usuarioCorreo = null);
    }

    public class AuditoriaService : IAuditoriaService
    {
        private readonly DentissaDbContext _db;
        private readonly IActorActual _actor;

        public AuditoriaService(DentissaDbContext db, IActorActual actor)
        {
            _db = db;
            _actor = actor;
        }

        // usuarioId y usuarioCorreo explícitos sirven cuando aún no hay sesión en el token
        // (login); si se omiten se toma el actor de la petición.
        public async Task RegistrarAsync(string accion, string entidad, string? entidadId,
                                         object? detalle = null, int? usuarioId = null, string? usuarioCorreo = null)
        {
            _db.Auditoria.Add(new Auditoria
            {
                OcurridoEn    = DateTime.UtcNow,
                UsuarioId     = usuarioId ?? _actor.UsuarioId,
                UsuarioCorreo = usuarioCorreo ?? _actor.Correo,
                Accion        = accion,
                Entidad       = entidad,
                EntidadId     = entidadId,
                Detalle       = detalle == null ? null : JsonBitacora.Serializar(detalle),
                Ip            = _actor.Ip
            });
            await _db.SaveChangesAsync();
        }
    }

    // Historial de cada cita. Agrega la fila al contexto: se guarda con el mismo
    // SaveChanges que el cambio de la cita (misma transacción).
    public interface ICitaHistorialService
    {
        void Registrar(Cita cita, string accion, string? detalle = null, object? datos = null);
    }

    public class CitaHistorialService : ICitaHistorialService
    {
        public const string CanalPanel = "panel";

        private readonly DentissaDbContext _db;
        private readonly IActorActual _actor;

        public CitaHistorialService(DentissaDbContext db, IActorActual actor)
        {
            _db = db;
            _actor = actor;
        }

        public void Registrar(Cita cita, string accion, string? detalle = null, object? datos = null)
        {
            _db.CitaHistorial.Add(new CitaHistorial
            {
                Cita      = cita,
                Accion    = accion,
                UsuarioId = _actor.UsuarioId,
                Canal     = CanalPanel,
                Detalle   = detalle,
                Datos     = datos == null ? null : JsonBitacora.Serializar(datos),
                CreadoEn  = DateTime.UtcNow
            });
        }
    }
}
