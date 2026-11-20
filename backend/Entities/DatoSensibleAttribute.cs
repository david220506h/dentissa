namespace backend.Entities;

// Columna cuyo valor nunca se escribe en la bitácora: se registra como "[cambió]".
// Es la única lista de columnas sensibles del sistema (credenciales, salud, identidad).
[AttributeUsage(AttributeTargets.Property)]
public sealed class DatoSensibleAttribute : Attribute
{
}

// Columna de control de sesión (token de renovación, último acceso). Un guardado que
// solo cambia estas columnas no genera fila de bitácora: el inicio y el cierre de
// sesión se registran como eventos explícitos.
[AttributeUsage(AttributeTargets.Property)]
public sealed class ControlDeSesionAttribute : Attribute
{
}
