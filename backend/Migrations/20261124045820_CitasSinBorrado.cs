using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CitasSinBorrado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sin cambios de modelo: solo los triggers (ver SqlFase2)
            migrationBuilder.Sql(SqlFase2.CitasSinBorrado);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SqlFase2.QuitarCitasSinBorrado);
        }
    }
}
