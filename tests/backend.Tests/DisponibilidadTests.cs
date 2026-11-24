using backend.Constants;
using backend.Entities;
using backend.Services.Agenda;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests;

// Fase 2, commit 4 (diseño de la agenda, sección 1): motor de disponibilidad contra PostgreSQL, con reloj fijo.
// "Ahora": sábado 28-nov-2026 20:00 en la clínica (02:00 UTC del domingo). El lunes es 30-nov.
public class FabricaDisponibilidad : DentissaFactory
{
    public static readonly DateTimeOffset SabadoNoche = new(2026, 11, 29, 2, 0, 0, TimeSpan.Zero);
    public RelojFijo Reloj { get; } = new(SabadoNoche);
    public override TimeProvider? Tiempo => Reloj;

    public async Task<IReadOnlyList<HorarioDisponible>> CalcularAsync(DateOnly desde, DateOnly hasta, int servicioId, CanalReserva canal, int? dentistaId = null)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IDisponibilidadService>().CalcularAsync(desde, hasta, servicioId, canal, dentistaId);
    }

    // Dentista activo y servicio de n horarios ligado solo a él
    public Task<(int DentistaId, int ServicioId, int PacienteId)> DentistaConServicioAsync(short espacios = 1) => ConDbAsync(async db =>
    {
        var dentista = new Dentista
        {
            Usuario = new Usuario
            {
                Nombre = "Dentista", Apellido = "Prueba", Correo = $"dent-{Guid.NewGuid():N}@prueba.test",
                PasswordHash = "x", Rol = Roles.Dentista
            },
            Especialidad = "General"
        };
        var servicio = new Servicio
        {
            Nombre = $"Servicio {espacios}", Precio = 500, DuracionSlots = espacios,
            Categoria = new CategoriaServicio { Nombre = "Cat-" + Guid.NewGuid().ToString("N") }
        };
        var paciente = new Paciente { Nombre = "Paciente", Apellido = "Prueba", Telefono = "5550001111", Origen = OrigenesCita.Recepcion };
        db.AddRange(dentista, servicio, paciente);
        await db.SaveChangesAsync();
        db.DentistaServicios.Add(new DentistaServicio { DentistaId = dentista.Id, ServicioId = servicio.Id });
        await db.SaveChangesAsync();
        return (dentista.Id, servicio.Id, paciente.Id);
    });

    public static DateTime Utc(int dia, int hora, int mes = 11) => new DateTime(2026, mes, dia, hora, 0, 0, DateTimeKind.Utc).AddHours(6);
}

public class DisponibilidadTests : IClassFixture<FabricaDisponibilidad>
{
    private static readonly DateOnly Lunes = new(2026, 11, 30);
    private readonly FabricaDisponibilidad _f;

    public DisponibilidadTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(FabricaDisponibilidad.SabadoNoche);
    }

    private static string[] Horas(IEnumerable<HorarioDisponible> h) => h.Select(x => x.Hora.ToString("H:mm")).ToArray();

    private Task CitaAsync(int dentista, int servicio, int paciente, DateTime inicioUtc, int horas = 1, string estado = EstadosCita.Confirmada)
        => _f.ConDbAsync(db =>
        {
            var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicioUtc, horas);
            c.Estado = estado;
            if (estado == EstadosCita.Cancelada) c.MotivoCancelacion = "Prueba";
            db.Citas.Add(c);
            return db.SaveChangesAsync();
        });

    [Fact]
    public async Task Lunes_SeisHorarios_EnHoraLocalYUtc()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();

        var h = await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion);

        Assert.Equal(["8:00", "9:00", "10:00", "12:00", "13:00", "15:00"], Horas(h));
        // Sin desplazamientos fijos: 8:00 en Ciudad de México es 14:00 UTC
        Assert.Equal(new DateTime(2026, 11, 30, 14, 0, 0, DateTimeKind.Utc), h[0].InicioUtc);
        Assert.Equal(h[0].InicioUtc.AddHours(1), h[0].FinUtc);
    }

    [Fact]
    public async Task Domingo_SinHorarios()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();

        Assert.Empty(await _f.CalcularAsync(new DateOnly(2026, 12, 6), new DateOnly(2026, 12, 6), servicio, CanalReserva.Recepcion));
    }

    [Fact]
    public async Task DosEspacios_OchoNueveYDoce()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync(2);

        Assert.Equal(["8:00", "9:00", "12:00"], Horas(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion)));
    }

    [Fact]
    public async Task CitaDeDosEspacios_OcupaAmbos()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        await CitaAsync(dentista, servicio, paciente, FabricaDisponibilidad.Utc(30, 12), horas: 2);

        Assert.Equal(["8:00", "9:00", "10:00", "15:00"], Horas(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion)));
    }

    [Fact]
    public async Task DosEspacios_NoCabenEnUnHuecoDeUno()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync(2);
        await CitaAsync(dentista, servicio, paciente, FabricaDisponibilidad.Utc(30, 9));

        // 8–10 y 9–11 chocan con la de las 9:00; solo queda 12–14
        Assert.Equal(["12:00"], Horas(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion)));
    }

    [Fact]
    public async Task EnLinea_AnticipacionEnHorasDeAtencion()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();

        // Sábado 20:00 → lunes: 8, 9 y 10 tienen 0, 1 y 2 h de atención antes (filas 1 y 2 de la tabla 2.5)
        Assert.Equal(["12:00", "13:00", "15:00"], Horas(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.EnLinea)));
    }

    [Fact]
    public async Task Recepcion_SesentaMinutosDeReloj_YNuncaEnElPasado()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        _f.Reloj.Fijar(new DateTimeOffset(2026, 11, 30, 15, 30, 0, TimeSpan.Zero)); // lunes 9:30

        // 8 y 9 ya pasaron; 10:00 está a 30 min (menos de 60)
        Assert.Equal(["12:00", "13:00", "15:00"], Horas(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion)));
    }

    [Fact]
    public async Task Horizonte_SesentaDias()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();

        // Hoy (local) es 28-nov: el límite es 27-ene-2027 (martes) y el 28-ene ya no
        Assert.NotEmpty(await _f.CalcularAsync(new DateOnly(2027, 1, 27), new DateOnly(2027, 1, 27), servicio, CanalReserva.Recepcion));
        Assert.Empty(await _f.CalcularAsync(new DateOnly(2027, 1, 28), new DateOnly(2027, 1, 28), servicio, CanalReserva.Recepcion));
    }

    [Fact]
    public async Task Ocupacion_SoloConfirmadasYPendientesVigentes()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        await CitaAsync(dentista, servicio, paciente, FabricaDisponibilidad.Utc(30, 8), estado: EstadosCita.Pendiente);   // recepción: bloquea
        await CitaAsync(dentista, servicio, paciente, FabricaDisponibilidad.Utc(30, 9), estado: EstadosCita.Cancelada);   // no bloquea
        await CitaAsync(dentista, servicio, paciente, FabricaDisponibilidad.Utc(30, 10), estado: EstadosCita.Completada); // no está activa

        // Solicitudes en línea: una vencida (no bloquea) y una vigente (bloquea). Se crean una hora antes de "ahora".
        _f.Reloj.Fijar(FabricaDisponibilidad.SabadoNoche.AddHours(-2));
        await _f.ConDbAsync(db =>
        {
            var vencida = DentissaFactory.NuevaCita(paciente, dentista, servicio, FabricaDisponibilidad.Utc(30, 12));
            vencida.Origen = OrigenesCita.Web;
            vencida.BloqueoExpiraEn = FabricaDisponibilidad.SabadoNoche.UtcDateTime.AddHours(-1);
            var vigente = DentissaFactory.NuevaCita(paciente, dentista, servicio, FabricaDisponibilidad.Utc(30, 13));
            vigente.Origen = OrigenesCita.Web;
            vigente.BloqueoExpiraEn = FabricaDisponibilidad.SabadoNoche.UtcDateTime.AddHours(5);
            db.Citas.AddRange(vencida, vigente);
            return db.SaveChangesAsync();
        });
        _f.Reloj.Fijar(FabricaDisponibilidad.SabadoNoche);

        Assert.Equal(["9:00", "10:00", "12:00", "15:00"], Horas(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion)));
    }

    [Fact]
    public async Task VariosDentistas_LibresYFiltroPorDentista()
    {
        var (a, servicio, paciente) = await _f.DentistaConServicioAsync();
        var (b, _, _) = await _f.DentistaConServicioAsync();
        await _f.ConDbAsync(db => { db.DentistaServicios.Add(new DentistaServicio { DentistaId = b, ServicioId = servicio }); return db.SaveChangesAsync(); });
        await CitaAsync(a, servicio, paciente, FabricaDisponibilidad.Utc(30, 8));

        var todos = await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion);
        Assert.Equal([b], todos.Single(h => h.Hora == new TimeOnly(8, 0)).DentistasLibres);
        Assert.Equal(2, todos.Single(h => h.Hora == new TimeOnly(9, 0)).DentistasLibres.Count);

        Assert.DoesNotContain("8:00", Horas(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion, dentistaId: a)));
    }

    [Fact]
    public async Task DentistaInactivo_NoAtiende()
    {
        var (dentista, servicio, _) = await _f.DentistaConServicioAsync();
        await _f.ConDbAsync(db => db.Dentistas.Where(d => d.Id == dentista).ExecuteUpdateAsync(s => s.SetProperty(d => d.Activo, false)));

        Assert.Empty(await _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion));
    }

    [Fact]
    public async Task ServicioInactivo_YRangoInvalido_Rechazados()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        await _f.ConDbAsync(db => db.Servicios.Where(s => s.Id == servicio).ExecuteUpdateAsync(s => s.SetProperty(x => x.Activo, false)));

        await Assert.ThrowsAsync<ServicioNoDisponibleException>(() => _f.CalcularAsync(Lunes, Lunes, servicio, CanalReserva.Recepcion));
        await Assert.ThrowsAsync<RangoInvalidoException>(() => _f.CalcularAsync(Lunes, Lunes.AddDays(-1), servicio, CanalReserva.Recepcion));
    }
}

// Pruebas que cambian la configuración, los cierres o los dentistas: base propia
public class DisponibilidadConfiguracionTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public DisponibilidadConfiguracionTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(FabricaDisponibilidad.SabadoNoche);
    }

    private static string[] Horas(IEnumerable<HorarioDisponible> h) => h.Select(x => x.Hora.ToString("H:mm")).ToArray();

    [Fact]
    public async Task Cierres_TotalYParcial_YDescansoSoloDelSabado()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        await _f.ConDbAsync(db =>
        {
            db.AgendaCierres.Add(new AgendaCierre { ConfiguracionId = ConfiguracionAgenda.IdUnico, Fecha = new DateOnly(2026, 12, 1), Motivo = "Prueba" });
            db.AgendaCierres.Add(new AgendaCierre
            {
                ConfiguracionId = ConfiguracionAgenda.IdUnico, Fecha = new DateOnly(2026, 12, 2),
                HoraInicio = new TimeOnly(12, 0), HoraFin = new TimeOnly(16, 0), Motivo = "Prueba"
            });
            db.HorarioDescansos.Add(new HorarioDescanso
            {
                ConfiguracionId = ConfiguracionAgenda.IdUnico, DiaSemana = 6, HoraInicio = new TimeOnly(9, 0), HoraFin = new TimeOnly(10, 0)
            });
            return db.SaveChangesAsync();
        });

        Assert.Empty(await _f.CalcularAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 1), servicio, CanalReserva.Recepcion));
        Assert.Equal(["8:00", "9:00", "10:00"], Horas(await _f.CalcularAsync(new DateOnly(2026, 12, 2), new DateOnly(2026, 12, 2), servicio, CanalReserva.Recepcion)));
        Assert.Equal(["8:00", "10:00", "12:00", "13:00", "15:00"], Horas(await _f.CalcularAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 5), servicio, CanalReserva.Recepcion)));
    }

    [Fact]
    public async Task ServicioSinDentistas_LoAtiendeCualquiera_YAvisaUnaVezAlAdmin()
    {
        await _f.DentistaConServicioAsync(); // al menos un dentista activo
        var admin = await _f.CrearUsuarioAsync(Roles.Admin);
        var servicio = await _f.ConDbAsync(async db =>
        {
            var s = new Servicio { Nombre = "Sin dentistas", Precio = 100, Categoria = new CategoriaServicio { Nombre = "Cat-" + Guid.NewGuid().ToString("N") } };
            db.Servicios.Add(s);
            await db.SaveChangesAsync();
            return s.Id;
        });

        Assert.NotEmpty(await _f.CalcularAsync(new DateOnly(2026, 12, 3), new DateOnly(2026, 12, 3), servicio, CanalReserva.Recepcion));
        await _f.CalcularAsync(new DateOnly(2026, 12, 4), new DateOnly(2026, 12, 4), servicio, CanalReserva.Recepcion);

        var avisos = await _f.ConDbAsync(db => db.Notificaciones.AsNoTracking()
            .Where(n => n.UsuarioId == admin.Id && n.Tipo == DisponibilidadService.TipoAvisoConfiguracion).ToListAsync());
        var aviso = Assert.Single(avisos);
        Assert.Equal("interna", aviso.Canal);
        Assert.False(aviso.Leida);
        Assert.Equal(FabricaDisponibilidad.SabadoNoche.UtcDateTime, aviso.CreadoEn);
    }
}

// Zona con horario de verano, aislada (México ya no lo usa): la hora inexistente se omite
public class DisponibilidadHorarioDeVeranoTests
{
    [Fact]
    public void HoraInexistente_SeOmite()
    {
        var nuevaYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var horario = new HorarioAtencion(
            Enumerable.Range(1, 7).Select(d => new HorarioAtencion.Dia((short)d, new TimeOnly(0, 0), new TimeOnly(5, 0))), [], [], nuevaYork);

        // 14-mar-2027: a las 2:00 el reloj salta a las 3:00
        var inicios = horario.InicioPosibles(new DateOnly(2027, 3, 14), 60, 1);

        Assert.Equal([new TimeOnly(0, 0), new TimeOnly(1, 0), new TimeOnly(3, 0), new TimeOnly(4, 0)], inicios.Select(i => i.Hora));
        Assert.Equal(new DateTime(2027, 3, 14, 7, 0, 0, DateTimeKind.Utc), inicios.Single(i => i.Hora == new TimeOnly(3, 0)).InicioUtc);
    }
}
