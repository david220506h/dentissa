using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AvisoConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones",
                sql: "tipo IN ('solicitud_nueva','confirmacion','recordatorio','cambio','cancelacion','solicitud_cambio','aviso_configuracion')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones",
                sql: "tipo IN ('solicitud_nueva','confirmacion','recordatorio','cambio','cancelacion','solicitud_cambio')");
        }
    }
}
