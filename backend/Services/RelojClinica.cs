namespace backend.Services
{
    // Único lugar donde se convierte entre la hora local de la clínica y UTC.
    // La zona se lee de Clinica:ZonaHoraria (por defecto America/Mexico_City).
    public interface IRelojClinica
    {
        TimeZoneInfo Zona { get; }
        DateTime AUtc(DateOnly fecha, TimeOnly hora);
        (DateOnly Fecha, TimeOnly Hora) ALocal(DateTime utc);
        DateOnly HoyLocal();
    }

    public class RelojClinica : IRelojClinica
    {
        public const string ZonaPorDefecto = "America/Mexico_City";

        private readonly TimeProvider _tiempo;

        public RelojClinica(IConfiguration config, TimeProvider tiempo)
        {
            _tiempo = tiempo;
            var id = config["Clinica:ZonaHoraria"];
            Zona = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(id) ? ZonaPorDefecto : id);
        }

        public TimeZoneInfo Zona { get; }

        public DateTime AUtc(DateOnly fecha, TimeOnly hora)
        {
            var local = DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, Zona);
        }

        public (DateOnly Fecha, TimeOnly Hora) ALocal(DateTime utc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);
            return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
        }

        public DateOnly HoyLocal() => ALocal(_tiempo.GetUtcNow().UtcDateTime).Fecha;
    }
}
