namespace backend.Entities;

public class ExamenMedico : IConMarcasDeTiempo
{
    public int Id { get; set; }

    public int PacienteId { get; set; }

    public string? MotivoConsulta { get; set; }

    public string? AntecedenteFamiliar { get; set; }

    public string? ComentarioAdicional { get; set; }

    public bool? PresionAlta { get; set; }

    public string? PresionAltaDetalle { get; set; }

    public bool? PresionBaja { get; set; }

    public string? PresionBajaDetalle { get; set; }

    public bool? Vih { get; set; }

    public string? Vihdetalle { get; set; }

    public bool? Diabetes { get; set; }

    public string? DiabetesDetalle { get; set; }

    public bool? Hepatitis { get; set; }

    public string? HepatitisDetalle { get; set; }

    public bool? Asma { get; set; }

    public string? AsmaDetalle { get; set; }

    public bool? Gastritis { get; set; }

    public string? GastritisDetalle { get; set; }

    public bool? Ulceras { get; set; }

    public string? UlcerasDetalle { get; set; }

    public bool? Fuma { get; set; }

    public string? FumaDetalle { get; set; }

    public bool? EnfermedadesSanguineas { get; set; }

    public string? EnfermedadesSanguineasDetalle { get; set; }

    public bool? ProblemasCardiacos { get; set; }

    public string? ProblemasCardiacosDetalle { get; set; }

    public bool? OtraEnfermedad { get; set; }

    public string? OtraEnfermedadDetalle { get; set; }

    public bool? Alergias { get; set; }

    public string? AlergiasDetalle { get; set; }

    public bool? OperacionGrande { get; set; }

    public string? OperacionGrandeDetalle { get; set; }

    public bool? MedicacionPermanente { get; set; }

    public string? MedicacionDetalle { get; set; }

    public int? VecesCepilladoDia { get; set; }

    public bool? SangraEncias { get; set; }

    public bool? HemorragiasExtraccion { get; set; }

    public bool? RechinamientoDientes { get; set; }

    public string? OtrasMolestias { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual Paciente Paciente { get; set; } = null!;
}
