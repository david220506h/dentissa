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

    // Commit 2 (1.6): horas de reloj → minutos. La fila sembrada (2 h, 24 h, 2 h) pasa a los valores
    // aprobados (180, 720 y 120 minutos de atención); una configuración ya personalizada solo se
    // convierte, y se corrige lo mínimo para cumplir las restricciones nuevas.
    public const string ConvertirConfiguracionAMinutos = """
        UPDATE configuracion_agenda SET
          anticipacion_en_linea_atencion_minutos = anticipacion_en_linea_atencion_minutos * 60,
          vida_maxima_solicitud_atencion_minutos = vida_maxima_solicitud_atencion_minutos * 60,
          liberacion_antes_cita_atencion_minutos = liberacion_antes_cita_atencion_minutos * 60;

        UPDATE configuracion_agenda SET
          anticipacion_en_linea_atencion_minutos = 180,
          vida_maxima_solicitud_atencion_minutos = 720,
          liberacion_antes_cita_atencion_minutos = 120
        WHERE anticipacion_en_linea_atencion_minutos = 120
          AND vida_maxima_solicitud_atencion_minutos = 1440
          AND liberacion_antes_cita_atencion_minutos = 120;

        UPDATE configuracion_agenda SET liberacion_antes_cita_atencion_minutos = 120
        WHERE liberacion_antes_cita_atencion_minutos <= 0;
        UPDATE configuracion_agenda SET anticipacion_en_linea_atencion_minutos = liberacion_antes_cita_atencion_minutos + 60
        WHERE anticipacion_en_linea_atencion_minutos <= liberacion_antes_cita_atencion_minutos;
        """;

    // Reversa del commit 2: minutos → horas, redondeando hacia arriba
    public const string ConvertirConfiguracionAHoras = """
        UPDATE configuracion_agenda SET
          anticipacion_en_linea_atencion_minutos = CEIL(anticipacion_en_linea_atencion_minutos / 60.0),
          vida_maxima_solicitud_atencion_minutos = CEIL(vida_maxima_solicitud_atencion_minutos / 60.0),
          liberacion_antes_cita_atencion_minutos = CEIL(liberacion_antes_cita_atencion_minutos / 60.0);
        """;
}
