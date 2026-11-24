namespace backend.Services
{
    /// <summary>
    /// Quién figura en la bitácora dentro de un ámbito. Por defecto, la cuenta de la petición (IActorActual).
    /// En una acción automática (p. ej., vencimiento de una solicitud) el actor es "sistema", sin usuario, y
    /// <see cref="ProvocadoPor"/> guarda qué cita y qué cuenta la desencadenaron.
    /// </summary>
    public class ContextoAuditoria
    {
        public const string ActorSistema = "sistema";

        public string? Actor { get; private set; }

        // jsonb: p. ej., {"citaId": 12, "usuarioId": 3}
        public string? ProvocadoPor { get; private set; }

        public IDisposable ComoSistema(object? provocadoPor)
        {
            var (actor, provocado) = (Actor, ProvocadoPor);
            Actor = ActorSistema;
            ProvocadoPor = provocadoPor == null ? null : JsonBitacora.Serializar(provocadoPor);
            return new Restaurar(() => (Actor, ProvocadoPor) = (actor, provocado));
        }

        private sealed class Restaurar(Action accion) : IDisposable
        {
            public void Dispose() => accion();
        }
    }
}
