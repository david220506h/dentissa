namespace backend.Migrations;

// SQL de la migración Inicial que EF Core no puede expresar en el modelo.
// No se modifica: un cambio posterior va en una migración nueva.
internal static class SqlInicial
{
    // Dos citas activas del mismo dentista no pueden traslaparse. Compara rangos
    // semiabiertos [inicio, fin): una cita de dos slots choca con la que empieza
    // dentro de ella, y dos citas contiguas no chocan.
    public const string SinTraslape = """
        ALTER TABLE citas ADD CONSTRAINT citas_sin_traslape_dentista
          EXCLUDE USING gist (
            dentista_id WITH =,
            tstzrange(inicio, fin, '[)') WITH &&
          ) WHERE (estado IN ('pendiente', 'confirmada'));
        """;

    // La ficha de dentista solo se liga a una cuenta con rol dentista, y la de paciente
    // a una con rol paciente. Tampoco se puede cambiar el rol de una cuenta con ficha.
    public const string RolAmarrado = """
        ALTER TABLE dentistas
          ADD COLUMN rol character varying(20) GENERATED ALWAYS AS ('dentista'::character varying(20)) STORED;
        ALTER TABLE dentistas
          ADD CONSTRAINT fk_dentistas_usuarios_id_rol
          FOREIGN KEY (usuario_id, rol) REFERENCES usuarios (id, rol);

        ALTER TABLE pacientes
          ADD COLUMN rol character varying(20) GENERATED ALWAYS AS ('paciente'::character varying(20)) STORED;
        ALTER TABLE pacientes
          ADD CONSTRAINT fk_pacientes_usuarios_id_rol
          FOREIGN KEY (usuario_id, rol) REFERENCES usuarios (id, rol);
        """;

    // auditoria y cita_historial son de solo inserción
    public const string BitacoraInmodificable = """
        CREATE FUNCTION impedir_modificacion_bitacora() RETURNS trigger
        LANGUAGE plpgsql AS $$
        BEGIN
          RAISE EXCEPTION 'La tabla % es de solo inserción (operación % rechazada)', TG_TABLE_NAME, TG_OP
            USING ERRCODE = 'restrict_violation';
        END;
        $$;

        CREATE TRIGGER auditoria_solo_insercion
          BEFORE UPDATE OR DELETE ON auditoria
          FOR EACH ROW EXECUTE FUNCTION impedir_modificacion_bitacora();
        CREATE TRIGGER auditoria_sin_truncate
          BEFORE TRUNCATE ON auditoria
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_modificacion_bitacora();

        CREATE TRIGGER cita_historial_solo_insercion
          BEFORE UPDATE OR DELETE ON cita_historial
          FOR EACH ROW EXECUTE FUNCTION impedir_modificacion_bitacora();
        CREATE TRIGGER cita_historial_sin_truncate
          BEFORE TRUNCATE ON cita_historial
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_modificacion_bitacora();
        """;

    // RN-11: el expediente no se borra físicamente
    public const string ExpedienteSinBorrado = """
        CREATE FUNCTION impedir_borrado_expediente() RETURNS trigger
        LANGUAGE plpgsql AS $$
        BEGIN
          RAISE EXCEPTION 'Los registros del expediente clínico no se borran (tabla %)', TG_TABLE_NAME
            USING ERRCODE = 'restrict_violation';
        END;
        $$;

        CREATE TRIGGER examen_medico_sin_borrado BEFORE DELETE ON examen_medico
          FOR EACH ROW EXECUTE FUNCTION impedir_borrado_expediente();
        CREATE TRIGGER examen_medico_sin_truncate BEFORE TRUNCATE ON examen_medico
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_borrado_expediente();
        CREATE TRIGGER examen_clinico_sin_borrado BEFORE DELETE ON examen_clinico
          FOR EACH ROW EXECUTE FUNCTION impedir_borrado_expediente();
        CREATE TRIGGER examen_clinico_sin_truncate BEFORE TRUNCATE ON examen_clinico
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_borrado_expediente();
        CREATE TRIGGER consulta_registros_sin_borrado BEFORE DELETE ON consulta_registros
          FOR EACH ROW EXECUTE FUNCTION impedir_borrado_expediente();
        CREATE TRIGGER consulta_registros_sin_truncate BEFORE TRUNCATE ON consulta_registros
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_borrado_expediente();
        CREATE TRIGGER consulta_fotos_sin_borrado BEFORE DELETE ON consulta_fotos
          FOR EACH ROW EXECUTE FUNCTION impedir_borrado_expediente();
        CREATE TRIGGER consulta_fotos_sin_truncate BEFORE TRUNCATE ON consulta_fotos
          FOR EACH STATEMENT EXECUTE FUNCTION impedir_borrado_expediente();
        """;

    // Las tablas (y con ellas triggers y restricciones) las borra el Down generado
    public const string QuitarFunciones = """
        DROP FUNCTION IF EXISTS impedir_modificacion_bitacora();
        DROP FUNCTION IF EXISTS impedir_borrado_expediente();
        """;
}
