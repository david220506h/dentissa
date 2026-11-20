using backend.Entities;

namespace backend.Services
{
    // Reglas de consistencia entre usuarios.correo (inicio de sesión) y pacientes.correo
    // (contacto). Viven solo aquí (docs/database/modelo-datos.md, sección 3.1).
    public interface IPacientesService
    {
        // Regla 1: al crearse la cuenta, la ficha copia nombre, apellido, correo y teléfono
        Paciente NuevaFichaParaCuenta(Usuario usuario, string origen);
    }

    public class PacientesService : IPacientesService
    {
        public Paciente NuevaFichaParaCuenta(Usuario usuario, string origen)
        {
            if (!OrigenesCita.Todos.Contains(origen))
                throw new ArgumentOutOfRangeException(nameof(origen));

            return new Paciente
            {
                Usuario = usuario,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono,
                Origen = origen,
                ContactoVerificado = false
            };
        }
    }
}
