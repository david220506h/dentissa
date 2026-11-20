using System.Reflection;
using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Services
{
    // Escribe en auditoria una fila por cada alta, cambio o borrado de las entidades
    // principales, en la misma transacción que el cambio: si la bitácora no se puede
    // escribir, el cambio tampoco se guarda.
    //
    // Los cambios y bajas se agregan antes de guardar (van en la transacción de EF).
    // Las altas necesitan la clave que genera la base, así que, si hay altas, el
    // interceptor abre una transacción, guarda, agrega sus filas de bitácora y confirma.
    //
    // Columnas con [DatoSensible]: se registran como "[cambió]", nunca con su valor.
    public class AuditoriaInterceptor : SaveChangesInterceptor
    {
        public const string Cambio = "[cambió]";

        private static readonly HashSet<Type> Auditadas = new()
        {
            typeof(Usuario), typeof(Paciente), typeof(Dentista), typeof(DentistaServicio),
            typeof(Servicio), typeof(Oferta), typeof(Cita),
            typeof(ConfiguracionAgenda), typeof(HorarioDia), typeof(HorarioDescanso), typeof(AgendaCierre)
        };

        // Marcas de tiempo: las pone el contexto, no aportan a la bitácora
        private static readonly HashSet<string> Ignoradas = new()
        {
            nameof(IConMarcasDeTiempo.CreadoEn), nameof(IConMarcasDeTiempo.ActualizadoEn)
        };

        private readonly IActorActual _actor;
        private readonly List<(EntityEntry Entrada, Dictionary<string, object?> Detalle)> _altas = new();
        private IDbContextTransaction? _transaccionPropia;
        private bool _guardandoBitacora;

        public AuditoriaInterceptor(IActorActual actor) => _actor = actor;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (!_guardandoBitacora && eventData.Context is DentissaDbContext db && Preparar(db))
                _transaccionPropia = db.Database.BeginTransaction();
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_guardandoBitacora && eventData.Context is DentissaDbContext db && Preparar(db))
                _transaccionPropia = await db.Database.BeginTransactionAsync(cancellationToken);
            return result;
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (_guardandoBitacora || eventData.Context is not DentissaDbContext db) return result;
            try
            {
                if (AgregarAltas(db))
                {
                    _guardandoBitacora = true;
                    db.SaveChanges();
                }
                _transaccionPropia?.Commit();
            }
            catch
            {
                _transaccionPropia?.Rollback();
                throw;
            }
            finally
            {
                Limpiar();
            }
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (_guardandoBitacora || eventData.Context is not DentissaDbContext db) return result;
            try
            {
                if (AgregarAltas(db))
                {
                    _guardandoBitacora = true;
                    await db.SaveChangesAsync(cancellationToken);
                }
                if (_transaccionPropia != null)
                    await _transaccionPropia.CommitAsync(cancellationToken);
            }
            catch
            {
                if (_transaccionPropia != null)
                    await _transaccionPropia.RollbackAsync(CancellationToken.None);
                throw;
            }
            finally
            {
                Limpiar();
            }
            return result;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            if (_guardandoBitacora) return;
            _transaccionPropia?.Rollback();
            Limpiar();
        }

        public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (_guardandoBitacora) return;
            if (_transaccionPropia != null)
                await _transaccionPropia.RollbackAsync(CancellationToken.None);
            Limpiar();
        }

        private void Limpiar()
        {
            _altas.Clear();
            _transaccionPropia?.Dispose();
            _transaccionPropia = null;
            _guardandoBitacora = false;
        }

        // Agrega las filas de cambios y bajas; guarda las altas para después.
        // Devuelve true si hace falta abrir una transacción propia.
        private bool Preparar(DentissaDbContext db)
        {
            _altas.Clear();
            var entradas = db.ChangeTracker.Entries()
                .Where(e => Auditadas.Contains(e.Metadata.ClrType))
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            foreach (var entrada in entradas)
            {
                switch (entrada.State)
                {
                    case EntityState.Added:
                        _altas.Add((entrada, DetalleAlta(entrada)));
                        break;
                    case EntityState.Modified:
                        var cambios = DetalleCambios(entrada);
                        if (cambios == null) break;
                        var eliminado = entrada.Entity is IEliminable eliminable && eliminable.EliminadoEn != null
                            && entrada.OriginalValues[nameof(IEliminable.EliminadoEn)] == null;
                        db.Auditoria.Add(Fila(eliminado ? AccionesAuditoria.Eliminar : AccionesAuditoria.Modificar, entrada, cambios));
                        break;
                    case EntityState.Deleted:
                        db.Auditoria.Add(Fila(AccionesAuditoria.Eliminar, entrada, null));
                        break;
                }
            }

            return _altas.Count > 0
                && db.Database.IsRelational()
                && db.Database.CurrentTransaction == null;
        }

        private bool AgregarAltas(DentissaDbContext db)
        {
            foreach (var (entrada, detalle) in _altas)
                db.Auditoria.Add(Fila(AccionesAuditoria.Crear, entrada, detalle));
            return _altas.Count > 0;
        }

        private Auditoria Fila(string accion, EntityEntry entrada, object? detalle) => new()
        {
            OcurridoEn    = DateTime.UtcNow,
            UsuarioId     = _actor.UsuarioId,
            UsuarioCorreo = _actor.Correo,
            Accion        = accion,
            Entidad       = entrada.Metadata.GetTableName() ?? entrada.Metadata.ClrType.Name,
            EntidadId     = string.Join(",", entrada.Metadata.FindPrimaryKey()!.Properties
                                .Select(p => entrada.Property(p.Name).CurrentValue?.ToString())),
            Detalle       = detalle == null ? null : JsonBitacora.Serializar(detalle),
            Ip            = _actor.Ip
        };

        private static Dictionary<string, object?> DetalleAlta(EntityEntry entrada)
        {
            var valores = new Dictionary<string, object?>();
            foreach (var p in entrada.Properties.Where(p => !Ignoradas.Contains(p.Metadata.Name)))
            {
                if (p.CurrentValue == null) continue;
                valores[Columna(p)] = EsSensible(p) ? Cambio : p.CurrentValue;
            }
            return new Dictionary<string, object?> { ["valores"] = valores };
        }

        // null si no hay cambios que registrar
        private static Dictionary<string, object?>? DetalleCambios(EntityEntry entrada)
        {
            var modificadas = entrada.Properties
                .Where(p => p.IsModified && !Ignoradas.Contains(p.Metadata.Name)
                            && !Equals(p.OriginalValue, p.CurrentValue))
                .ToList();

            // Solo cambió el control de sesión: lo cubren los eventos de inicio y cierre
            if (modificadas.Count == 0 || modificadas.All(EsControlDeSesion))
                return null;

            var cambios = new Dictionary<string, object?>();
            foreach (var p in modificadas)
            {
                cambios[Columna(p)] = EsSensible(p)
                    ? Cambio
                    : new Dictionary<string, object?> { ["antes"] = p.OriginalValue, ["despues"] = p.CurrentValue };
            }
            return new Dictionary<string, object?> { ["cambios"] = cambios };
        }

        private static string Columna(PropertyEntry p) => p.Metadata.GetColumnName();

        private static bool EsSensible(PropertyEntry p)
            => p.Metadata.PropertyInfo?.IsDefined(typeof(DatoSensibleAttribute)) == true;

        private static bool EsControlDeSesion(PropertyEntry p)
            => p.Metadata.PropertyInfo?.IsDefined(typeof(ControlDeSesionAttribute)) == true;
    }

    public static class AccionesAuditoria
    {
        public const string Crear               = "crear";
        public const string Modificar           = "modificar";
        public const string Eliminar            = "eliminar";
        public const string InicioSesion        = "inicio_sesion";
        public const string InicioSesionFallido = "inicio_sesion_fallido";
        public const string CierreSesion        = "cierre_sesion";
    }
}
