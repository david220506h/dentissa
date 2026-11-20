namespace backend.Entities;

// Tablas con creado_en y actualizado_en: los llena DentissaDbContext al guardar
public interface IConMarcasDeTiempo
{
    DateTime CreadoEn { get; set; }
    DateTime ActualizadoEn { get; set; }
}

// Tablas con borrado lógico (eliminado_en)
public interface IEliminable
{
    DateTime? EliminadoEn { get; set; }
}

// La clínica nunca se queda sin un administrador activo
public class UltimoAdministradorException : InvalidOperationException
{
    public UltimoAdministradorException()
        : base("No se puede desactivar, eliminar ni cambiar de rol al último administrador activo.") { }
}
