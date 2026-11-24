using System.Security.Cryptography;
using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Datos de demostración, claramente ficticios (docs/DEMO.md). Solo corre con <c>--sembrar-demo</c> y
    /// <c>Demo:Habilitada=true</c>, y solo sobre una base sin cuentas, pacientes ni citas: nunca mezcla datos
    /// inventados con datos reales. Crea servicios, 1 cuenta de administración, 1 asistente, 3 dentistas,
    /// 30 pacientes (correos .test) y citas de dos semanas en todos los estados.
    ///
    /// Las contraseñas del personal se generan aquí y se escriben UNA vez en la salida estándar (petición
    /// expresa de la demo); no pasan por ILogger ni se guardan en el repositorio.
    /// </summary>
    public class SembradoDemoService
    {
        public const string Dominio = "clinica-demo.test";

        private readonly DentissaDbContext _db;
        private readonly IConfiguration _config;
        private readonly IRelojClinica _reloj;
        private readonly TimeProvider _tiempo;
        private readonly ContextoAuditoria _auditoria;

        public SembradoDemoService(DentissaDbContext db, IConfiguration config, IRelojClinica reloj, TimeProvider tiempo,
            ContextoAuditoria auditoria)
        {
            _db = db;
            _config = config;
            _reloj = reloj;
            _tiempo = tiempo;
            _auditoria = auditoria;
        }

        public sealed record Cuenta(string Rol, string Nombre, string Correo, string Password);

/// <summary>Contraseñas fijas (despliegue): administración y una compartida para las otras 4 cuentas.</summary>
        public sealed record ContrasenasFijas(string Administracion, string Personal);

        /// <summary>¿La base está vacía de cuentas, pacientes y citas?</summary>
        public async Task<bool> BaseVaciaAsync(CancellationToken ct = default) =>
            !await _db.Usuarios.IgnoreQueryFilters().AnyAsync(ct) && !await _db.Pacientes.IgnoreQueryFilters().AnyAsync(ct)
            && !await _db.Citas.AnyAsync(ct);

        /// <summary>
        /// Siembra y devuelve las cuentas del personal. Sin <paramref name="fijas"/>, las contraseñas se generan
        /// (demo local, se muestran una vez); con ellas, se usan esas y nadie las imprime (despliegue).
        /// </summary>
        public async Task<IReadOnlyList<Cuenta>> EjecutarAsync(ContrasenasFijas? fijas = null, CancellationToken ct = default)
        {
            if (fijas is null && !_config.GetValue<bool>("Demo:Habilitada"))
                throw new InvalidOperationException("El sembrado de la demo requiere Demo:Habilitada=true.");
            if (!await BaseVaciaAsync(ct))
                throw new InvalidOperationException(
                    "La base ya tiene cuentas, pacientes o citas: el sembrado de la demo solo corre sobre una base vacía.");

            using var _ = _auditoria.ComoSistema(new { sembrado = "demo" });
            var azar = new Random(2026);
            var ahora = _tiempo.GetUtcNow().UtcDateTime;

            // Servicios
            var general = new CategoriaServicio { Nombre = "Odontología general" };
            var especialidad = new CategoriaServicio { Nombre = "Especialidades" };
            var servicios = new[]
            {
                new Servicio { Categoria = general, Nombre = "Limpieza dental", Precio = 600, DuracionSlots = 1 },
                new Servicio { Categoria = general, Nombre = "Resina dental", Precio = 900, DuracionSlots = 1 },
                new Servicio { Categoria = general, Nombre = "Extracción dental", Precio = 1200, DuracionSlots = 1 },
                new Servicio { Categoria = especialidad, Nombre = "Valoración de ortodoncia", Precio = 500, DuracionSlots = 1 },
                new Servicio { Categoria = especialidad, Nombre = "Blanqueamiento", Precio = 2500, DuracionSlots = 1 },
                new Servicio { Categoria = especialidad, Nombre = "Tratamiento de conductos", Precio = 3500, DuracionSlots = 2 },
            };
            _db.Servicios.AddRange(servicios);

            // Personal
            var cuentas = new List<Cuenta>();
            Usuario Personal(string rol, string nombre, string apellido, string correo)
            {
                var password = fijas is null ? GenerarPassword() : rol == Roles.Admin ? fijas.Administracion : fijas.Personal;
                cuentas.Add(new Cuenta(rol, $"{nombre} {apellido}", correo, password));
                var u = new Usuario
                {
                    Correo = correo, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Rol = rol,
                    Nombre = nombre, Apellido = apellido, Activo = true,
                };
                _db.Usuarios.Add(u);
                return u;
            }
            Personal(Roles.Admin, "Laura", "Mendoza", $"administracion@{Dominio}");
            var asistente = Personal(Roles.Asistente, "Karla", "Reyes", $"recepcion@{Dominio}");
            var dentistas = new[]
            {
                (Personal(Roles.Dentista, "Mariana", "Soto", $"mariana.soto@{Dominio}"), "Dra.", "Odontología general"),
                (Personal(Roles.Dentista, "Andrés", "Lara", $"andres.lara@{Dominio}"), "Dr.", "Endodoncia"),
                (Personal(Roles.Dentista, "Elena", "Ríos", $"elena.rios@{Dominio}"), "Dra.", "Ortodoncia"),
            }.Select(d => new Dentista { Usuario = d.Item1, Titulo = d.Item2, Especialidad = d.Item3, Activo = true }).ToArray();
            _db.Dentistas.AddRange(dentistas);

            // Pacientes inventados; 10 con cuenta (contraseña al azar que nadie conoce: no entran al panel)
            var pacientes = PACIENTES.Select((p, i) =>
            {
                var correo = i % 7 == 6 ? null : $"{Sin(p.Nombre)}.{Sin(p.Apellido)}@correo.test";
                var telefono = i % 9 == 8 ? null : $"55 5550 {1000 + i * 37:0000}";
                var paciente = new Paciente
                {
                    Nombre = p.Nombre, Apellido = p.Apellido, Correo = correo, Telefono = telefono ?? (correo is null ? "55 5550 9999" : null),
                    Sexo = p.Sexo, Alergias = p.Alergias, Origen = OrigenesCita.Recepcion, ContactoVerificado = true,
                    FechaNacimiento = new DateOnly(1960 + azar.Next(0, 50), azar.Next(1, 13), azar.Next(1, 29)),
                };
                if (i < 10 && correo is not null)
                    paciente.Usuario = new Usuario
                    {
                        Correo = correo, PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerarPassword()), Rol = Roles.Paciente,
                        Nombre = p.Nombre, Apellido = p.Apellido, Activo = true,
                    };
                return paciente;
            }).ToArray();
            _db.Pacientes.AddRange(pacientes);
            await _db.SaveChangesAsync(ct);

            // Citas: de 7 días antes a 7 días después de hoy, de lunes a sábado, en los seis horarios
            var config = await _db.ConfiguracionAgenda.Include(c => c.Dias).Include(c => c.Descansos).SingleAsync(ct);
            var horas = new[] { 8, 9, 10, 12, 13, 15 };
            var hoy = _reloj.HoyLocal();
            var citas = new List<Cita>();
            for (var fecha = hoy.AddDays(-7); fecha <= hoy.AddDays(7); fecha = fecha.AddDays(1))
            {
                if (fecha.DayOfWeek == DayOfWeek.Sunday) continue;
                foreach (var dentista in dentistas)
                {
                    var libreDesde = 0;
                    foreach (var hora in horas)
                    {
                        if (hora < libreDesde || azar.NextDouble() > 0.55) continue;
                        var servicio = servicios[azar.Next(servicios.Length)];
                        // Un servicio de 2 horarios solo cabe si el siguiente es contiguo (no cruza un descanso)
                        if (servicio.DuracionSlots == 2 && !horas.Contains(hora + 1)) servicio = servicios[0];
                        var inicio = _reloj.AUtc(fecha, new TimeOnly(hora, 0));
                        var fin = inicio.AddMinutes(config.DuracionSlotMinutos * servicio.DuracionSlots);
                        libreDesde = hora + servicio.DuracionSlots;
                        var paciente = pacientes[azar.Next(pacientes.Length)];
                        citas.Add(NuevaCita(paciente, dentista, servicio, inicio, fin, ahora, asistente, azar));
                    }
                }
            }
            _db.Citas.AddRange(citas);
            await _db.SaveChangesAsync(ct);

            // Historial: creación y, si cambió, el estado final (textos genéricos, como los servicios de la agenda)
            foreach (var c in citas)
            {
                _db.CitaHistorial.Add(new CitaHistorial
                {
                    CitaId = c.Id, Accion = AccionesCitaHistorial.Creacion, UsuarioId = asistente.Id,
                    Canal = CanalesCitaHistorial.Panel, Detalle = "Cita creada por recepción (datos de demostración)", CreadoEn = c.CreadoEn,
                });
                var (accion, detalle) = c.Estado switch
                {
                    EstadosCita.Confirmada => (AccionesCitaHistorial.Confirmacion, "Confirmada por recepción (llamada)"),
                    EstadosCita.Cancelada => (AccionesCitaHistorial.Cancelacion, "Cancelada por recepción"),
                    EstadosCita.Completada => (AccionesCitaHistorial.CambioEstado, "confirmada → completada"),
                    EstadosCita.NoAsistio => (AccionesCitaHistorial.CambioEstado, "confirmada → no_asistio"),
                    _ => ((string?)null, (string?)null),
                };
                if (accion is not null)
                    _db.CitaHistorial.Add(new CitaHistorial
                    {
                        CitaId = c.Id, Accion = accion, UsuarioId = asistente.Id, Detalle = detalle,
                        Canal = accion == AccionesCitaHistorial.Confirmacion ? CanalesCitaHistorial.Llamada : CanalesCitaHistorial.Panel,
                        CreadoEn = c.ActualizadoEn,
                    });
            }
            await _db.SaveChangesAsync(ct);
            return cuentas;
        }

        private static readonly string[] MOTIVOS_CANCELACION =
        {
            "La persona avisó que no puede asistir", "La persona pidió otra fecha", "La clínica reprogramó la cita",
        };

        private static Cita NuevaCita(Paciente paciente, Dentista dentista, Servicio servicio, DateTime inicio, DateTime fin,
            DateTime ahora, Usuario asistente, Random azar)
        {
            var dado = azar.NextDouble();
            string estado;
            if (fin <= ahora)
                estado = dado < 0.75 ? EstadosCita.Completada : dado < 0.88 ? EstadosCita.NoAsistio : EstadosCita.Cancelada;
            else if (inicio <= ahora)
                estado = EstadosCita.Confirmada; // en curso
            else
                estado = dado < 0.55 ? EstadosCita.Confirmada : dado < 0.88 ? EstadosCita.Pendiente : EstadosCita.Cancelada;

            var creada = inicio.AddDays(-azar.Next(2, 10));
            if (creada > ahora) creada = ahora.AddHours(-1);
            var confirmada = estado is EstadosCita.Confirmada or EstadosCita.Completada or EstadosCita.NoAsistio;
            var cancelada = estado == EstadosCita.Cancelada;
            var cambio = cancelada ? creada.AddDays(1) : estado == EstadosCita.Completada || estado == EstadosCita.NoAsistio ? fin : creada.AddHours(2);
            if (cambio > ahora) cambio = ahora;
            return new Cita
            {
                PacienteId = paciente.Id, DentistaId = dentista.Id, ServicioId = servicio.Id, Inicio = inicio, Fin = fin,
                Estado = estado, Origen = OrigenesCita.Recepcion, CreadoPor = asistente.Id,
                CostoBase = servicio.Precio, CostoTotal = servicio.Precio,
                ConfirmadaEn = confirmada ? (creada.AddHours(2) < ahora ? creada.AddHours(2) : ahora) : null,
                ConfirmadaPor = confirmada ? asistente.Id : null,
                ConfirmacionCanal = confirmada ? CanalesCitaHistorial.Llamada : null,
                MotivoCancelacion = cancelada ? MOTIVOS_CANCELACION[azar.Next(MOTIVOS_CANCELACION.Length)] : null,
                CanceladaEn = cancelada ? cambio : null,
                CreadoEn = creada, ActualizadoEn = cambio,
            };
        }

        // 16 caracteres sin los que se confunden (0/O, 1/l/I); al menos una mayúscula, una minúscula y un dígito
        private static string GenerarPassword()
        {
            const string mayus = "ABCDEFGHJKLMNPQRSTUVWXYZ", minus = "abcdefghijkmnopqrstuvwxyz", digitos = "23456789";
            const string todos = mayus + minus + digitos;
            var c = new char[16];
            c[0] = mayus[RandomNumberGenerator.GetInt32(mayus.Length)];
            c[1] = minus[RandomNumberGenerator.GetInt32(minus.Length)];
            c[2] = digitos[RandomNumberGenerator.GetInt32(digitos.Length)];
            for (var i = 3; i < c.Length; i++) c[i] = todos[RandomNumberGenerator.GetInt32(todos.Length)];
            RandomNumberGenerator.Shuffle(c.AsSpan());
            return new string(c);
        }

        private static string Sin(string s) =>
            new string(s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD)
                .Where(ch => char.IsAsciiLetterLower(ch)).ToArray());

        private sealed record P(string Nombre, string Apellido, string? Sexo, string? Alergias = null);

        // Nombres inventados para la demostración; cualquier parecido con una persona real es coincidencia
        private static readonly P[] PACIENTES =
        {
            new("Rosa", "Aguilar", "femenino"), new("Ernesto", "Vega", "masculino", "Penicilina"),
            new("Valeria", "Núñez", "femenino"), new("Mario", "Delgado", "masculino", "Látex"),
            new("Sofía", "Rangel", "femenino"), new("Camila", "Ortega", "femenino"),
            new("Diego", "Herrera", "masculino"), new("Paola", "Rivas", "femenino", "Lidocaína"),
            new("Andrea", "Cruz", "femenino"), new("Luis", "Ibarra", "masculino"),
            new("Jorge", "Salinas", "masculino", "Ibuprofeno"), new("Elena", "Mora", "femenino"),
            new("Hugo", "Paredes", "masculino"), new("Daniela", "Solís", "femenino"),
            new("Iván", "Lozano", "masculino"), new("Patricia", "Gil", "femenino", "Penicilina y amoxicilina"),
            new("Roberto", "Salas", "masculino"), new("Óscar", "Reyna", "masculino"),
            new("Mariana", "Paz", "femenino"), new("Alex", "Fuentes", "no_especifica"),
            new("Natalia", "Campos", "femenino"), new("Tomás", "Ibáñez", "masculino", "Látex"),
            new("Lucía", "Treviño", "femenino"), new("Gabriel", "Montes", "masculino"),
            new("Renata", "Villalobos", "femenino"), new("Sam", "Cordero", "otro"),
            new("Fernanda", "Quiroz", "femenino", "Sulfas"), new("Emilio", "Bravo", "masculino"),
            new("Ximena", "Duarte", "femenino"), new("Rafael", "Olmos", "masculino"),
        };
    }
}
