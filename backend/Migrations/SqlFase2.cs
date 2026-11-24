namespace backend.Migrations;

// SQL de las migraciones de la Fase 2 que EF Core no puede expresar en el modelo.
// Cada constante pertenece a una migración y no se modifica después de integrarla.
internal static class SqlFase2
{
    // Commit 1: las citas no se borran físicamente (se cancelan con motivo). Mismo
    // código de error que el expediente y la bitácora (restrict_violation).
    public const string CitasSinBorrado = """
        CREATE FUNCTION impedir_borrado_citas() RETURNS trigger
        LANGUAGE plpgsql AS $$
        BEGIN
          RAISE EXCEPTION 'Las citas no se borran: se cancelan con motivo (operación % rechazada)', TG_OP
            USING ERRCODE = 'restrict_violation';
        END;
        $$;

        CREATE TRIGGER citas_sin_borrado BEFORE DELETE ON citas
          FOR EACH ROW EXECUTE FUNCTION impedir_borrado_citas();
        CREATE TRIGGER citas_sin_truncate BEFORE TRUNCATE ON citas
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_borrado_citas();
        """;

    public const string QuitarCitasSinBorrado = """
        DROP TRIGGER IF EXISTS citas_sin_truncate ON citas;
        DROP TRIGGER IF EXISTS citas_sin_borrado ON citas;
        DROP FUNCTION IF EXISTS impedir_borrado_citas();
        """;
}
