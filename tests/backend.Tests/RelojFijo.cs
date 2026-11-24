namespace backend.Tests;

// Reloj controlado para las pruebas: devuelve siempre el instante fijado (y se puede mover).
public sealed class RelojFijo : TimeProvider
{
    private DateTimeOffset _ahora;

    public RelojFijo(DateTimeOffset ahora) => _ahora = ahora;

    public override DateTimeOffset GetUtcNow() => _ahora;

    public void Fijar(DateTimeOffset ahora) => _ahora = ahora;

    public void Avanzar(TimeSpan cuanto) => _ahora += cuanto;
}
