using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaActorSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "actor",
                table: "auditoria",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provocado_por",
                table: "auditoria",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_auditoria_actor",
                table: "auditoria",
                sql: "actor IS NULL OR actor = 'sistema'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_auditoria_actor",
                table: "auditoria");

            migrationBuilder.DropColumn(
                name: "actor",
                table: "auditoria");

            migrationBuilder.DropColumn(
                name: "provocado_por",
                table: "auditoria");
        }
    }
}
