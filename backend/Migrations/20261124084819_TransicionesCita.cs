using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class TransicionesCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_citas_confirmacion_canal",
                table: "citas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial");

            migrationBuilder.AddColumn<string>(
                name: "motivo_cambio",
                table: "citas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
            // xmin es columna de sistema de PostgreSQL: solo se mapea (concurrencia optimista), no se crea

            migrationBuilder.AddCheckConstraint(
                name: "ck_citas_confirmacion_canal",
                table: "citas",
                sql: "confirmacion_canal IN ('correo','portal','llamada','panel')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial",
                sql: "accion IN ('creacion','modificacion','reagenda','cambio_estado','confirmacion','cancelacion','solicitud_cambio','resolucion_solicitud','vencimiento','reasignacion','reenvio_enlace','correccion_estado')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cita_historial_canal",
                table: "cita_historial",
                sql: "canal IS NULL OR canal IN ('panel','llamada','correo','portal','web','sistema')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_citas_confirmacion_canal",
                table: "citas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cita_historial_canal",
                table: "cita_historial");

            migrationBuilder.DropColumn(
                name: "motivo_cambio",
                table: "citas");

            migrationBuilder.AddCheckConstraint(
                name: "ck_citas_confirmacion_canal",
                table: "citas",
                sql: "confirmacion_canal IN ('correo','portal','llamada')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial",
                sql: "accion IN ('creacion','modificacion','reagenda','cambio_estado','confirmacion','cancelacion','solicitud_cambio','resolucion_solicitud','vencimiento')");
        }
    }
}
