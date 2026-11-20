using System.ComponentModel.DataAnnotations;
using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    // Se ejecuta al arrancar la API:
    //  1. Aplica migraciones solo si BaseDeDatos:AplicarMigracionesAlArrancar = true
    //     (Development). En otros entornos se aplican de forma explícita.
    //  2. Crea la configuración de agenda inicial si no existe.
    //  3. Crea el administrador inicial si no existe ningún administrador, con
    //     Seed:AdminEmail y Seed:AdminPassword (user-secrets o variables de entorno).
    // Nunca escribe en logs ni en consola el correo ni la contraseña.
    public class DatosInicialesService
    {
        public const int LongitudMinimaPassword = 12;

        private readonly DentissaDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<DatosInicialesService> _log;

        public DatosInicialesService(DentissaDbContext db, IConfiguration config, ILogger<DatosInicialesService> log)
        {
            _db = db;
            _config = config;
            _log = log;
        }

        public async Task EjecutarAsync(CancellationToken ct = default)
        {
            if (_config.GetValue<bool>("BaseDeDatos:AplicarMigracionesAlArrancar"))
            {
                _log.LogInformation("Aplicando migraciones pendientes.");
                await _db.Database.MigrateAsync(ct);
            }

            await CrearConfiguracionAgendaAsync(ct);
            await CrearAdministradorInicialAsync(ct);
        }

        // Horario acordado: lunes a sábado de 8:00 a 16:00, descansos de 11 a 12 y de 14 a 15
        // (seis horarios de 60 minutos). Es solo el valor inicial: después se edita en la base.
        private async Task CrearConfiguracionAgendaAsync(CancellationToken ct)
        {
            if (await _db.ConfiguracionAgenda.AnyAsync(ct))
                return;

            var config = new ConfiguracionAgenda
            {
                Id = ConfiguracionAgenda.IdUnico,
                DuracionSlotMinutos = 60,
                AnticipacionMinimaHoras = 2,
                BloqueoPendienteHoras = 24,
                LiberacionAntesCitaHoras = 2,
                ActualizadoEn = DateTime.UtcNow
            };
            for (short dia = 1; dia <= 7; dia++)
            {
                var laborable = dia <= 6;
                config.Dias.Add(new HorarioDia
                {
                    DiaSemana = dia,
                    Laborable = laborable,
                    HoraApertura = laborable ? new TimeOnly(8, 0) : null,
                    HoraCierre = laborable ? new TimeOnly(16, 0) : null
                });
            }
            config.Descansos.Add(new HorarioDescanso { HoraInicio = new TimeOnly(11, 0), HoraFin = new TimeOnly(12, 0) });
            config.Descansos.Add(new HorarioDescanso { HoraInicio = new TimeOnly(14, 0), HoraFin = new TimeOnly(15, 0) });

            _db.ConfiguracionAgenda.Add(config);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Se creó la configuración de agenda inicial.");
        }

        private async Task CrearAdministradorInicialAsync(CancellationToken ct)
        {
            if (await _db.Usuarios.AnyAsync(u => u.Rol == Roles.Admin, ct))
                return;

            var correo = _config["Seed:AdminEmail"]?.Trim();
            var password = _config["Seed:AdminPassword"];

            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
            {
                _log.LogWarning("No hay ningún administrador y no se creó el inicial: faltan Seed:AdminEmail o " +
                                "Seed:AdminPassword (user-secrets o variables de entorno Seed__AdminEmail y Seed__AdminPassword).");
                return;
            }
            if (!new EmailAddressAttribute().IsValid(correo))
            {
                _log.LogWarning("No se creó el administrador inicial: Seed:AdminEmail no es un correo válido.");
                return;
            }
            if (password.Length < LongitudMinimaPassword)
            {
                _log.LogWarning("No se creó el administrador inicial: Seed:AdminPassword debe tener al menos {Minimo} caracteres.",
                                LongitudMinimaPassword);
                return;
            }
            if (await _db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
            {
                _log.LogWarning("No se creó el administrador inicial: Seed:AdminEmail ya pertenece a otra cuenta.");
                return;
            }

            _db.Usuarios.Add(new Usuario
            {
                Correo = correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = Roles.Admin,
                Nombre = "Administrador",
                Apellido = "Dentissa",
                Activo = true
            });
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Se creó el administrador inicial.");
        }
    }
}
