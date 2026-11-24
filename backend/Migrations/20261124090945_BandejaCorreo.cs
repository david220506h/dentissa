using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class BandejaCorreo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones");

            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "notificaciones",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "intentos",
                table: "notificaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "plantilla",
                table: "notificaciones",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "procesando_desde",
                table: "notificaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "proximo_intento_en",
                table: "notificaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(SqlFase2.BandejaCorreoDatos);

            migrationBuilder.CreateIndex(
                name: "ix_notificaciones_bandeja_pendientes",
                table: "notificaciones",
                column: "proximo_intento_en",
                filter: "estado = 'pendiente'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notificaciones_bandeja",
                table: "notificaciones",
                sql: "(canal = 'correo') = (estado IS NOT NULL AND plantilla IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notificaciones_estado",
                table: "notificaciones",
                sql: "estado IS NULL OR estado IN ('pendiente','procesando','enviada','fallida','descartada')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notificaciones_plantilla",
                table: "notificaciones",
                sql: "plantilla IS NULL OR plantilla IN ('solicitud_recibida','confirmacion','recordatorio','cambio','cancelacion','solicitud_cambio_recibida','vencimiento','tope_pendientes','intento_registro')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones",
                sql: "tipo IN ('solicitud_nueva','confirmacion','recordatorio','cambio','cancelacion','solicitud_cambio','aviso_configuracion','vencimiento','reasignacion','envio_fallido','tope_pendientes','intento_registro','configuracion')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notificaciones_bandeja_pendientes",
                table: "notificaciones");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notificaciones_bandeja",
                table: "notificaciones");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notificaciones_estado",
                table: "notificaciones");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notificaciones_plantilla",
                table: "notificaciones");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "notificaciones");

            migrationBuilder.DropColumn(
                name: "intentos",
                table: "notificaciones");

            migrationBuilder.DropColumn(
                name: "plantilla",
                table: "notificaciones");

            migrationBuilder.DropColumn(
                name: "procesando_desde",
                table: "notificaciones");

            migrationBuilder.DropColumn(
                name: "proximo_intento_en",
                table: "notificaciones");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notificaciones_tipo",
                table: "notificaciones",
                sql: "tipo IN ('solicitud_nueva','confirmacion','recordatorio','cambio','cancelacion','solicitud_cambio','aviso_configuracion')");
        }
    }
}
