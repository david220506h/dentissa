using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Recordatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_notificaciones_recordatorio_cita",
                table: "notificaciones",
                column: "cita_id",
                unique: true,
                filter: "tipo = 'recordatorio' AND canal = 'correo'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_notificaciones_recordatorio_cita",
                table: "notificaciones");
        }
    }
}
