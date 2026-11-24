using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <summary>
    /// Diseño de la agenda, 1.6: configuración en minutos de atención. Las tres columnas en horas se
    /// renombran y convierten (no se borran), se agregan anticipación de recepción y horizonte, y la fila
    /// sembrada se ajusta a los valores aprobados solo si conserva los valores de siembra originales.
    /// </summary>
    public partial class AgendaEnHorasDeAtencion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "ck_configuracion_agenda_valores", table: "configuracion_agenda");

            migrationBuilder.RenameColumn(name: "anticipacion_minima_horas", table: "configuracion_agenda", newName: "anticipacion_en_linea_atencion_minutos");
            migrationBuilder.RenameColumn(name: "bloqueo_pendiente_horas", table: "configuracion_agenda", newName: "vida_maxima_solicitud_atencion_minutos");
            migrationBuilder.RenameColumn(name: "liberacion_antes_cita_horas", table: "configuracion_agenda", newName: "liberacion_antes_cita_atencion_minutos");
            migrationBuilder.Sql(SqlFase2.ConvertirConfiguracionAMinutos);

            migrationBuilder.AlterColumn<short>(name: "anticipacion_en_linea_atencion_minutos", table: "configuracion_agenda", type: "smallint", nullable: false, defaultValue: (short)180, oldClrType: typeof(short), oldType: "smallint");
            migrationBuilder.AlterColumn<short>(name: "vida_maxima_solicitud_atencion_minutos", table: "configuracion_agenda", type: "smallint", nullable: false, defaultValue: (short)720, oldClrType: typeof(short), oldType: "smallint");
            migrationBuilder.AlterColumn<short>(name: "liberacion_antes_cita_atencion_minutos", table: "configuracion_agenda", type: "smallint", nullable: false, defaultValue: (short)120, oldClrType: typeof(short), oldType: "smallint");

            migrationBuilder.AddColumn<short>(name: "anticipacion_recepcion_minutos", table: "configuracion_agenda", type: "smallint", nullable: false, defaultValue: (short)60);
            migrationBuilder.AddColumn<short>(name: "horizonte_reserva_dias", table: "configuracion_agenda", type: "smallint", nullable: false, defaultValue: (short)60);

            migrationBuilder.AddCheckConstraint(
                name: "ck_configuracion_agenda_anticipacion_mayor_que_liberacion",
                table: "configuracion_agenda",
                sql: "anticipacion_en_linea_atencion_minutos > liberacion_antes_cita_atencion_minutos");
            migrationBuilder.AddCheckConstraint(
                name: "ck_configuracion_agenda_valores",
                table: "configuracion_agenda",
                sql: "duracion_slot_minutos > 0 AND anticipacion_recepcion_minutos >= 0 AND liberacion_antes_cita_atencion_minutos > 0 AND vida_maxima_solicitud_atencion_minutos > 0 AND horizonte_reserva_dias > 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_citas_bloqueo_despues_de_creacion",
                table: "citas",
                sql: "bloqueo_expira_en IS NULL OR bloqueo_expira_en > creado_en");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "ck_citas_bloqueo_despues_de_creacion", table: "citas");
            migrationBuilder.DropCheckConstraint(name: "ck_configuracion_agenda_valores", table: "configuracion_agenda");
            migrationBuilder.DropCheckConstraint(name: "ck_configuracion_agenda_anticipacion_mayor_que_liberacion", table: "configuracion_agenda");

            migrationBuilder.DropColumn(name: "anticipacion_recepcion_minutos", table: "configuracion_agenda");
            migrationBuilder.DropColumn(name: "horizonte_reserva_dias", table: "configuracion_agenda");

            migrationBuilder.AlterColumn<short>(name: "anticipacion_en_linea_atencion_minutos", table: "configuracion_agenda", type: "smallint", nullable: false, oldClrType: typeof(short), oldType: "smallint", oldDefaultValue: (short)180);
            migrationBuilder.AlterColumn<short>(name: "vida_maxima_solicitud_atencion_minutos", table: "configuracion_agenda", type: "smallint", nullable: false, oldClrType: typeof(short), oldType: "smallint", oldDefaultValue: (short)720);
            migrationBuilder.AlterColumn<short>(name: "liberacion_antes_cita_atencion_minutos", table: "configuracion_agenda", type: "smallint", nullable: false, oldClrType: typeof(short), oldType: "smallint", oldDefaultValue: (short)120);

            migrationBuilder.Sql(SqlFase2.ConvertirConfiguracionAHoras);
            migrationBuilder.RenameColumn(name: "anticipacion_en_linea_atencion_minutos", table: "configuracion_agenda", newName: "anticipacion_minima_horas");
            migrationBuilder.RenameColumn(name: "vida_maxima_solicitud_atencion_minutos", table: "configuracion_agenda", newName: "bloqueo_pendiente_horas");
            migrationBuilder.RenameColumn(name: "liberacion_antes_cita_atencion_minutos", table: "configuracion_agenda", newName: "liberacion_antes_cita_horas");

            migrationBuilder.AddCheckConstraint(
                name: "ck_configuracion_agenda_valores",
                table: "configuracion_agenda",
                sql: "duracion_slot_minutos > 0 AND anticipacion_minima_horas >= 0 AND bloqueo_pendiente_horas > 0 AND liberacion_antes_cita_horas >= 0");
        }
    }
}
