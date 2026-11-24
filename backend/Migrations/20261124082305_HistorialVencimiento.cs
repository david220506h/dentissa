using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class HistorialVencimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial",
                sql: "accion IN ('creacion','modificacion','reagenda','cambio_estado','confirmacion','cancelacion','solicitud_cambio','resolucion_solicitud','vencimiento')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cita_historial_accion",
                table: "cita_historial",
                sql: "accion IN ('creacion','modificacion','reagenda','cambio_estado','confirmacion','cancelacion','solicitud_cambio','resolucion_solicitud')");
        }
    }
}
