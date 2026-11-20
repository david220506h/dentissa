using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "categorias_servicio",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorias_servicio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    correo = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    refresh_token_expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultimo_acceso_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.CheckConstraint("ck_usuarios_rol", "rol IN ('admin','asistente','dentista','paciente')");
                });

            migrationBuilder.CreateTable(
                name: "servicios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    categoria_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    duracion_slots = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    imagen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicios", x => x.id);
                    table.CheckConstraint("ck_servicios_duracion_slots", "duracion_slots BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_servicios_precio", "precio >= 0");
                    table.ForeignKey(
                        name: "fk_servicios_categorias_servicio_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_correo = table.Column<string>(type: "citext", nullable: true),
                    accion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entidad = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entidad_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    detalle = table.Column<string>(type: "jsonb", nullable: true),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "configuracion_agenda",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    duracion_slot_minutos = table.Column<short>(type: "smallint", nullable: false),
                    anticipacion_minima_horas = table.Column<short>(type: "smallint", nullable: false),
                    bloqueo_pendiente_horas = table.Column<short>(type: "smallint", nullable: false),
                    liberacion_antes_cita_horas = table.Column<short>(type: "smallint", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_por = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracion_agenda", x => x.id);
                    table.CheckConstraint("ck_configuracion_agenda_unica", "id = 1");
                    table.CheckConstraint("ck_configuracion_agenda_valores", "duracion_slot_minutos > 0 AND anticipacion_minima_horas >= 0 AND bloqueo_pendiente_horas > 0 AND liberacion_antes_cita_horas >= 0");
                    table.ForeignKey(
                        name: "fk_configuracion_agenda_usuarios_actualizado_por",
                        column: x => x.actualizado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "dentistas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    cedula_profesional = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    especialidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    titulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    foto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dentistas", x => x.id);
                    table.ForeignKey(
                        name: "fk_dentistas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correo = table.Column<string>(type: "citext", nullable: true),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    sexo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    curp = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    tipo_sangre = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    alergias = table.Column<string>(type: "text", nullable: true),
                    condiciones_medicas = table.Column<string>(type: "text", nullable: true),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contacto_verificado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacientes", x => x.id);
                    table.CheckConstraint("ck_pacientes_contacto", "correo IS NOT NULL OR telefono IS NOT NULL");
                    table.CheckConstraint("ck_pacientes_origen", "origen IN ('web','portal','recepcion')");
                    table.CheckConstraint("ck_pacientes_sexo", "sexo IN ('femenino','masculino','otro','no_especifica')");
                    table.CheckConstraint("ck_pacientes_tipo_sangre", "tipo_sangre IN ('A+','A-','B+','B-','AB+','AB-','O+','O-')");
                    table.ForeignKey(
                        name: "fk_pacientes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ofertas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servicio_id = table.Column<int>(type: "integer", nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ofertas", x => x.id);
                    table.CheckConstraint("ck_ofertas_descuento", "descuento > 0 AND descuento <= 100");
                    table.CheckConstraint("ck_ofertas_vigencia", "fecha_fin >= fecha_inicio");
                    table.ForeignKey(
                        name: "fk_ofertas_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "agenda_cierres",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    configuracion_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    creado_por = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agenda_cierres", x => x.id);
                    table.CheckConstraint("ck_agenda_cierres_horas", "(hora_inicio IS NULL AND hora_fin IS NULL) OR (hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio)");
                    table.ForeignKey(
                        name: "fk_agenda_cierres_configuracion_agenda_configuracion_id",
                        column: x => x.configuracion_id,
                        principalTable: "configuracion_agenda",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agenda_cierres_usuarios_creado_por",
                        column: x => x.creado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "horario_descansos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    configuracion_id = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<short>(type: "smallint", nullable: true),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_horario_descansos", x => x.id);
                    table.CheckConstraint("ck_horario_descansos_dia_semana", "dia_semana IS NULL OR dia_semana BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_horario_descansos_horas", "hora_fin > hora_inicio");
                    table.ForeignKey(
                        name: "fk_horario_descansos_configuracion_agenda_configuracion_id",
                        column: x => x.configuracion_id,
                        principalTable: "configuracion_agenda",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "horario_dias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    configuracion_id = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<short>(type: "smallint", nullable: false),
                    laborable = table.Column<bool>(type: "boolean", nullable: false),
                    hora_apertura = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    hora_cierre = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_horario_dias", x => x.id);
                    table.CheckConstraint("ck_horario_dias_dia_semana", "dia_semana BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_horario_dias_horas", "NOT laborable OR (hora_apertura IS NOT NULL AND hora_cierre IS NOT NULL AND hora_cierre > hora_apertura)");
                    table.ForeignKey(
                        name: "fk_horario_dias_configuracion_agenda_configuracion_id",
                        column: x => x.configuracion_id,
                        principalTable: "configuracion_agenda",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dentista_servicios",
                columns: table => new
                {
                    dentista_id = table.Column<int>(type: "integer", nullable: false),
                    servicio_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dentista_servicios", x => new { x.dentista_id, x.servicio_id });
                    table.ForeignKey(
                        name: "fk_dentista_servicios_dentistas_dentista_id",
                        column: x => x.dentista_id,
                        principalTable: "dentistas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_dentista_servicios_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "servicios_realizados",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servicio_id = table.Column<int>(type: "integer", nullable: false),
                    dentista_id = table.Column<int>(type: "integer", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    foto_antes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    foto_despues = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fecha_realizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicios_realizados", x => x.id);
                    table.ForeignKey(
                        name: "fk_servicios_realizados_dentistas_dentista_id",
                        column: x => x.dentista_id,
                        principalTable: "dentistas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_servicios_realizados_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "examen_medico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<int>(type: "integer", nullable: false),
                    motivo_consulta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    antecedente_familiar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    comentario_adicional = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    presion_alta = table.Column<bool>(type: "boolean", nullable: true),
                    presion_alta_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    presion_baja = table.Column<bool>(type: "boolean", nullable: true),
                    presion_baja_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    vih = table.Column<bool>(type: "boolean", nullable: true),
                    vihdetalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    diabetes = table.Column<bool>(type: "boolean", nullable: true),
                    diabetes_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    hepatitis = table.Column<bool>(type: "boolean", nullable: true),
                    hepatitis_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    asma = table.Column<bool>(type: "boolean", nullable: true),
                    asma_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    gastritis = table.Column<bool>(type: "boolean", nullable: true),
                    gastritis_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ulceras = table.Column<bool>(type: "boolean", nullable: true),
                    ulceras_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    fuma = table.Column<bool>(type: "boolean", nullable: true),
                    fuma_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    enfermedades_sanguineas = table.Column<bool>(type: "boolean", nullable: true),
                    enfermedades_sanguineas_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    problemas_cardiacos = table.Column<bool>(type: "boolean", nullable: true),
                    problemas_cardiacos_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    otra_enfermedad = table.Column<bool>(type: "boolean", nullable: true),
                    otra_enfermedad_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    alergias = table.Column<bool>(type: "boolean", nullable: true),
                    alergias_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    operacion_grande = table.Column<bool>(type: "boolean", nullable: true),
                    operacion_grande_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    medicacion_permanente = table.Column<bool>(type: "boolean", nullable: true),
                    medicacion_detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    veces_cepillado_dia = table.Column<int>(type: "integer", nullable: true),
                    sangra_encias = table.Column<bool>(type: "boolean", nullable: true),
                    hemorragias_extraccion = table.Column<bool>(type: "boolean", nullable: true),
                    rechinamiento_dientes = table.Column<bool>(type: "boolean", nullable: true),
                    otras_molestias = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_examen_medico", x => x.id);
                    table.ForeignKey(
                        name: "fk_examen_medico_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "citas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<int>(type: "integer", nullable: false),
                    dentista_id = table.Column<int>(type: "integer", nullable: false),
                    servicio_id = table.Column<int>(type: "integer", nullable: false),
                    oferta_id = table.Column<int>(type: "integer", nullable: true),
                    inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pendiente"),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    motivo_cancelacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    costo_base = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    descuento_aplicado = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    costo_extra = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    motivo_extra = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    costo_total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    bloqueo_expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmacion_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    confirmacion_token_expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmada_por = table.Column<int>(type: "integer", nullable: true),
                    confirmacion_canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    solicitud_cambio_tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    solicitud_cambio_motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    solicitud_cambio_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_citas", x => x.id);
                    table.CheckConstraint("ck_citas_bloqueo_solo_solicitudes", "origen IN ('web','portal') OR bloqueo_expira_en IS NULL");
                    table.CheckConstraint("ck_citas_cancelacion_con_motivo", "estado <> 'cancelada' OR motivo_cancelacion IS NOT NULL");
                    table.CheckConstraint("ck_citas_confirmacion_canal", "confirmacion_canal IN ('correo','portal','llamada')");
                    table.CheckConstraint("ck_citas_costos", "costo_base >= 0 AND costo_extra >= 0 AND costo_total >= 0");
                    table.CheckConstraint("ck_citas_descuento", "descuento_aplicado >= 0 AND descuento_aplicado <= 100");
                    table.CheckConstraint("ck_citas_estado", "estado IN ('pendiente','confirmada','completada','cancelada','no_asistio')");
                    table.CheckConstraint("ck_citas_fin_despues_de_inicio", "fin > inicio");
                    table.CheckConstraint("ck_citas_origen", "origen IN ('web','portal','recepcion')");
                    table.CheckConstraint("ck_citas_solicitud_cambio_completa", "(solicitud_cambio_tipo IS NULL AND solicitud_cambio_motivo IS NULL AND solicitud_cambio_en IS NULL) OR (solicitud_cambio_tipo IS NOT NULL AND solicitud_cambio_motivo IS NOT NULL AND solicitud_cambio_en IS NOT NULL)");
                    table.CheckConstraint("ck_citas_solicitud_cambio_tipo", "solicitud_cambio_tipo IN ('reagendar','cancelar')");
                    table.ForeignKey(
                        name: "fk_citas_dentistas_dentista_id",
                        column: x => x.dentista_id,
                        principalTable: "dentistas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_citas_ofertas_oferta_id",
                        column: x => x.oferta_id,
                        principalTable: "ofertas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_citas_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_citas_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_citas_usuarios_confirmada_por",
                        column: x => x.confirmada_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_citas_usuarios_creado_por",
                        column: x => x.creado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cita_historial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cita_id = table.Column<int>(type: "integer", nullable: false),
                    accion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    datos = table.Column<string>(type: "jsonb", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cita_historial", x => x.id);
                    table.CheckConstraint("ck_cita_historial_accion", "accion IN ('creacion','modificacion','reagenda','cambio_estado','confirmacion','cancelacion','solicitud_cambio','resolucion_solicitud')");
                    table.ForeignKey(
                        name: "fk_cita_historial_citas_cita_id",
                        column: x => x.cita_id,
                        principalTable: "citas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cita_historial_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "consulta_registros",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cita_id = table.Column<int>(type: "integer", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consulta_registros", x => x.id);
                    table.ForeignKey(
                        name: "fk_consulta_registros_citas_cita_id",
                        column: x => x.cita_id,
                        principalTable: "citas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "examen_clinico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<int>(type: "integer", nullable: false),
                    cita_id = table.Column<int>(type: "integer", nullable: true),
                    pa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    frecuencia_cardiaca = table.Column<int>(type: "integer", nullable: true),
                    temperatura = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    frecuencia_respiratoria = table.Column<int>(type: "integer", nullable: true),
                    examen_extraoral = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    examen_intraoral = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resultado_examenes_auxiliares = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_examen_clinico", x => x.id);
                    table.ForeignKey(
                        name: "fk_examen_clinico_citas_cita_id",
                        column: x => x.cita_id,
                        principalTable: "citas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_examen_clinico_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    paciente_id = table.Column<int>(type: "integer", nullable: true),
                    cita_id = table.Column<int>(type: "integer", nullable: true),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    asunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    leida = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    leida_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    enviada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    enviada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_envio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificaciones", x => x.id);
                    table.CheckConstraint("ck_notificaciones_canal", "canal IN ('interna','correo')");
                    table.CheckConstraint("ck_notificaciones_destinatario", "num_nonnulls(usuario_id, paciente_id) = 1");
                    table.CheckConstraint("ck_notificaciones_tipo", "tipo IN ('solicitud_nueva','confirmacion','recordatorio','cambio','cancelacion','solicitud_cambio')");
                    table.ForeignKey(
                        name: "fk_notificaciones_citas_cita_id",
                        column: x => x.cita_id,
                        principalTable: "citas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notificaciones_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notificaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "consulta_fotos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    consulta_registro_id = table.Column<int>(type: "integer", nullable: false),
                    foto_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consulta_fotos", x => x.id);
                    table.ForeignKey(
                        name: "fk_consulta_fotos_consulta_registros_consulta_registro_id",
                        column: x => x.consulta_registro_id,
                        principalTable: "consulta_registros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agenda_cierres_configuracion_id",
                table: "agenda_cierres",
                column: "configuracion_id");

            migrationBuilder.CreateIndex(
                name: "ix_agenda_cierres_creado_por",
                table: "agenda_cierres",
                column: "creado_por");

            migrationBuilder.CreateIndex(
                name: "ix_agenda_cierres_fecha",
                table: "agenda_cierres",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_entidad_entidad_id",
                table: "auditoria",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_ocurrido_en",
                table: "auditoria",
                column: "ocurrido_en");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_usuario_id",
                table: "auditoria",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_servicio_nombre",
                table: "categorias_servicio",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cita_historial_cita_id_creado_en",
                table: "cita_historial",
                columns: new[] { "cita_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_cita_historial_usuario_id",
                table: "cita_historial",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_citas_bloqueo_pendiente",
                table: "citas",
                column: "bloqueo_expira_en",
                filter: "estado = 'pendiente'");

            migrationBuilder.CreateIndex(
                name: "ix_citas_confirmacion_token_hash",
                table: "citas",
                column: "confirmacion_token_hash",
                unique: true,
                filter: "confirmacion_token_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_citas_confirmada_por",
                table: "citas",
                column: "confirmada_por");

            migrationBuilder.CreateIndex(
                name: "ix_citas_creado_por",
                table: "citas",
                column: "creado_por");

            migrationBuilder.CreateIndex(
                name: "ix_citas_dentista_id_inicio",
                table: "citas",
                columns: new[] { "dentista_id", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_citas_estado_inicio",
                table: "citas",
                columns: new[] { "estado", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_citas_oferta_id",
                table: "citas",
                column: "oferta_id");

            migrationBuilder.CreateIndex(
                name: "ix_citas_paciente_id_inicio",
                table: "citas",
                columns: new[] { "paciente_id", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_citas_servicio_id",
                table: "citas",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_configuracion_agenda_actualizado_por",
                table: "configuracion_agenda",
                column: "actualizado_por");

            migrationBuilder.CreateIndex(
                name: "ix_consulta_fotos_consulta_registro_id",
                table: "consulta_fotos",
                column: "consulta_registro_id");

            migrationBuilder.CreateIndex(
                name: "ix_consulta_registros_cita_id",
                table: "consulta_registros",
                column: "cita_id");

            migrationBuilder.CreateIndex(
                name: "ix_dentista_servicios_servicio_id",
                table: "dentista_servicios",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_dentistas_usuario_id",
                table: "dentistas",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examen_clinico_cita_id",
                table: "examen_clinico",
                column: "cita_id");

            migrationBuilder.CreateIndex(
                name: "ix_examen_clinico_paciente_id",
                table: "examen_clinico",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_examen_medico_paciente_id",
                table: "examen_medico",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_horario_descansos_configuracion_id",
                table: "horario_descansos",
                column: "configuracion_id");

            migrationBuilder.CreateIndex(
                name: "ix_horario_dias_configuracion_id_dia_semana",
                table: "horario_dias",
                columns: new[] { "configuracion_id", "dia_semana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notificaciones_cita_id",
                table: "notificaciones",
                column: "cita_id");

            migrationBuilder.CreateIndex(
                name: "ix_notificaciones_paciente_id",
                table: "notificaciones",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_notificaciones_usuario_id",
                table: "notificaciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_servicio_id_fecha_inicio_fecha_fin",
                table: "ofertas",
                columns: new[] { "servicio_id", "fecha_inicio", "fecha_fin" });

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_correo",
                table: "pacientes",
                column: "correo");

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_telefono",
                table: "pacientes",
                column: "telefono");

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_usuario_id",
                table: "pacientes",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servicios_categoria_id",
                table: "servicios",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_servicios_realizados_dentista_id",
                table: "servicios_realizados",
                column: "dentista_id");

            migrationBuilder.CreateIndex(
                name: "ix_servicios_realizados_servicio_id",
                table: "servicios_realizados",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_refresh_token",
                table: "usuarios",
                column: "refresh_token");

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_correo_vigente",
                table: "usuarios",
                column: "correo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_id_rol",
                table: "usuarios",
                columns: new[] { "id", "rol" },
                unique: true);

            // Lo que el modelo de EF Core no expresa (ver SqlInicial)
            migrationBuilder.Sql(SqlInicial.SinTraslape);
            migrationBuilder.Sql(SqlInicial.RolAmarrado);
            migrationBuilder.Sql(SqlInicial.BitacoraInmodificable);
            migrationBuilder.Sql(SqlInicial.ExpedienteSinBorrado);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agenda_cierres");

            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "cita_historial");

            migrationBuilder.DropTable(
                name: "consulta_fotos");

            migrationBuilder.DropTable(
                name: "dentista_servicios");

            migrationBuilder.DropTable(
                name: "examen_clinico");

            migrationBuilder.DropTable(
                name: "examen_medico");

            migrationBuilder.DropTable(
                name: "horario_descansos");

            migrationBuilder.DropTable(
                name: "horario_dias");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "servicios_realizados");

            migrationBuilder.DropTable(
                name: "consulta_registros");

            migrationBuilder.DropTable(
                name: "configuracion_agenda");

            migrationBuilder.DropTable(
                name: "citas");

            migrationBuilder.DropTable(
                name: "dentistas");

            migrationBuilder.DropTable(
                name: "ofertas");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.DropTable(
                name: "servicios");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "categorias_servicio");

            migrationBuilder.Sql(SqlInicial.QuitarFunciones);
        }
    }
}
