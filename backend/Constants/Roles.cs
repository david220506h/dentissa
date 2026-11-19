namespace backend.Constants
{
    // Nombres de rol centralizados. El renombre a los roles objetivo
    // (admin, asistente, dentista, paciente) se hará en la Fase 1 solo aquí.
    public static class Roles
    {
        public const string Admin      = "admin";
        public const string Secretaria = "secretaria";
        public const string Cliente    = "cliente";

        // Combinaciones para [Authorize(Roles = ...)]
        public const string Panel = Admin + "," + Secretaria;

        public static readonly string[] Todos = { Admin, Secretaria, Cliente };
    }
}
