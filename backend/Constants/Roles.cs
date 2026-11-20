namespace backend.Constants
{
    // Nombres de rol centralizados (deben coincidir con frontend/src/constants/roles.js
    // y con la restricción ck_usuarios_rol de la base). Un solo rol por cuenta.
    public static class Roles
    {
        public const string Admin     = "admin";
        public const string Asistente = "asistente";
        public const string Dentista  = "dentista";
        public const string Paciente  = "paciente";

        // Combinaciones para [Authorize(Roles = ...)]
        // Recepción: gestiona citas y pacientes
        public const string Panel = Admin + "," + Asistente;

        public static readonly string[] Todos = { Admin, Asistente, Dentista, Paciente };
    }
}
