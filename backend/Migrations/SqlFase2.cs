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
    // convierte, y se corrige lo mínimo para cumplir las restricciones nuevas:
    //   - liberación ≤ 0 → 120 minutos de atención;
    //   - anticipación en línea ≤ liberación → liberación + 60.
    // Si la configuración estaba personalizada, la conversión queda en la bitácora con los valores de antes
    // (horas) y de después (minutos), y qué campos se ajustaron al mínimo. Sin usuario: la hace la migración.
    public const string ConvertirConfiguracionAMinutos = """
        DO $$
        DECLARE
          antes RECORD;
          despues RECORD;
          sembrada boolean;
          ajustados text[] := ARRAY[]::text[];
        BEGIN
          SELECT anticipacion_en_linea_atencion_minutos AS anticipacion, vida_maxima_solicitud_atencion_minutos AS vida,
                 liberacion_antes_cita_atencion_minutos AS liberacion
            INTO antes FROM configuracion_agenda WHERE id = 1;
          IF NOT FOUND THEN RETURN; END IF;
          sembrada := antes.anticipacion = 2 AND antes.vida = 24 AND antes.liberacion = 2;

          UPDATE configuracion_agenda SET
            anticipacion_en_linea_atencion_minutos = anticipacion_en_linea_atencion_minutos * 60,
            vida_maxima_solicitud_atencion_minutos = vida_maxima_solicitud_atencion_minutos * 60,
            liberacion_antes_cita_atencion_minutos = liberacion_antes_cita_atencion_minutos * 60
          WHERE id = 1;

          IF sembrada THEN
            UPDATE configuracion_agenda SET
              anticipacion_en_linea_atencion_minutos = 180,
              vida_maxima_solicitud_atencion_minutos = 720,
              liberacion_antes_cita_atencion_minutos = 120
            WHERE id = 1;
            RETURN;
          END IF;

          IF (SELECT liberacion_antes_cita_atencion_minutos FROM configuracion_agenda WHERE id = 1) <= 0 THEN
            UPDATE configuracion_agenda SET liberacion_antes_cita_atencion_minutos = 120 WHERE id = 1;
            ajustados := array_append(ajustados, 'liberacion_antes_cita_atencion_minutos');
          END IF;
          IF (SELECT anticipacion_en_linea_atencion_minutos <= liberacion_antes_cita_atencion_minutos FROM configuracion_agenda WHERE id = 1) THEN
            UPDATE configuracion_agenda SET anticipacion_en_linea_atencion_minutos = liberacion_antes_cita_atencion_minutos + 60 WHERE id = 1;
            ajustados := array_append(ajustados, 'anticipacion_en_linea_atencion_minutos');
          END IF;

          SELECT anticipacion_en_linea_atencion_minutos AS anticipacion, vida_maxima_solicitud_atencion_minutos AS vida,
                 liberacion_antes_cita_atencion_minutos AS liberacion
            INTO despues FROM configuracion_agenda WHERE id = 1;

          INSERT INTO auditoria (ocurrido_en, accion, entidad, entidad_id, detalle)
          VALUES (now(), 'modificar', 'configuracion_agenda', '1', jsonb_build_object(
            'origen', 'migración AgendaEnHorasDeAtencion',
            'unidades', jsonb_build_object('antes', 'horas de reloj', 'despues', 'minutos de atención'),
            'cambios', jsonb_build_object(
              'anticipacion_en_linea_atencion_minutos', jsonb_build_object('antes', antes.anticipacion, 'despues', despues.anticipacion),
              'vida_maxima_solicitud_atencion_minutos', jsonb_build_object('antes', antes.vida, 'despues', despues.vida),
              'liberacion_antes_cita_atencion_minutos', jsonb_build_object('antes', antes.liberacion, 'despues', despues.liberacion)),
            'ajustadosAlMinimo', to_jsonb(ajustados)));
        END
        $$;
        """;

    // Commit 9 (5.2): los tokens de cita no se borran físicamente; se usan, vencen o se invalidan
    public const string CitaTokensSinBorrado = """
        CREATE FUNCTION impedir_borrado_cita_tokens() RETURNS trigger
        LANGUAGE plpgsql AS $$
        BEGIN
          RAISE EXCEPTION 'Los enlaces de cita no se borran: se usan, vencen o se invalidan (operación % rechazada)', TG_OP
            USING ERRCODE = 'restrict_violation';
        END;
        $$;

        CREATE TRIGGER cita_tokens_sin_borrado BEFORE DELETE ON cita_tokens
          FOR EACH ROW EXECUTE FUNCTION impedir_borrado_cita_tokens();
        CREATE TRIGGER cita_tokens_sin_truncate BEFORE TRUNCATE ON cita_tokens
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_borrado_cita_tokens();
        """;

    public const string QuitarCitaTokensSinBorrado = """
        DROP TRIGGER IF EXISTS cita_tokens_sin_truncate ON cita_tokens;
        DROP TRIGGER IF EXISTS cita_tokens_sin_borrado ON cita_tokens;
        DROP FUNCTION IF EXISTS impedir_borrado_cita_tokens();
        """;

    // Reversa del commit 2: minutos → horas, redondeando hacia arriba
    public const string ConvertirConfiguracionAHoras = """
        UPDATE configuracion_agenda SET
          anticipacion_en_linea_atencion_minutos = CEIL(anticipacion_en_linea_atencion_minutos / 60.0),
          vida_maxima_solicitud_atencion_minutos = CEIL(vida_maxima_solicitud_atencion_minutos / 60.0),
          liberacion_antes_cita_atencion_minutos = CEIL(liberacion_antes_cita_atencion_minutos / 60.0);
        """;
}
