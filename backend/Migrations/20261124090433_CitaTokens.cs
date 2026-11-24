using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CitaTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cita_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cita_id = table.Column<int>(type: "integer", nullable: false),
                    proposito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token_hash = table.Column<string>(type: "char(64)", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    invalidado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cita_tokens", x => x.id);
                    table.CheckConstraint("ck_cita_tokens_hash", "token_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_cita_tokens_proposito", "proposito IN ('confirmar','gestionar')");
                    table.ForeignKey(
                        name: "fk_cita_tokens_citas_cita_id",
                        column: x => x.cita_id,
                        principalTable: "citas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cita_tokens_cita_id_proposito",
                table: "cita_tokens",
                columns: new[] { "cita_id", "proposito" });

            migrationBuilder.CreateIndex(
                name: "ix_cita_tokens_token_hash",
                table: "cita_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.Sql(SqlFase2.CitaTokensSinBorrado);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SqlFase2.QuitarCitaTokensSinBorrado);

            migrationBuilder.DropTable(
                name: "cita_tokens");
        }
    }
}
