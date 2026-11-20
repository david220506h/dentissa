using System.Net;
using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Cada prueba usa una base propia: la regla depende de cuántos admins hay
public class UltimoAdministradorTests
{
    [Fact]
    public async Task NoSePuedeDesactivarEliminarNiCambiarDeRolAlUltimoAdmin()
    {
        using var factory = new DentissaFactory();
        var unico = await factory.CrearUsuarioAsync(Roles.Admin);

        await Assert.ThrowsAsync<UltimoAdministradorException>(() => Modificar(factory, unico.Id, u => u.Activo = false));
        await Assert.ThrowsAsync<UltimoAdministradorException>(() => Modificar(factory, unico.Id, u => u.Rol = Roles.Asistente));
        await Assert.ThrowsAsync<UltimoAdministradorException>(() => factory.ConDbAsync(async db =>
        {
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == unico.Id));
            return await db.SaveChangesAsync();
        }));

        var sigue = await factory.ConDbAsync(db => db.Usuarios.SingleAsync(u => u.Id == unico.Id));
        Assert.True(sigue.Activo);
        Assert.Equal(Roles.Admin, sigue.Rol);
        Assert.Null(sigue.EliminadoEn);
    }

    [Fact]
    public async Task ConOtroAdminActivo_SiSePuedeDesactivar()
    {
        using var factory = new DentissaFactory();
        var primero = await factory.CrearUsuarioAsync(Roles.Admin);
        var segundo = await factory.CrearUsuarioAsync(Roles.Admin);
        var cliente = await factory.ClienteDeAsync(segundo);

        var res = await cliente.PatchAsync($"/api/Usuarios/{primero.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        // Ahora el segundo es el último admin activo
        await Assert.ThrowsAsync<UltimoAdministradorException>(() => Modificar(factory, segundo.Id, u => u.Activo = false));
    }

    [Fact]
    public async Task UltimoAdminActivo_PorApi_Devuelve409()
    {
        using var factory = new DentissaFactory();
        var activo = await factory.CrearUsuarioAsync(Roles.Admin);
        var otro = await factory.CrearUsuarioAsync(Roles.Admin);
        var cliente = await factory.ClienteDeAsync(otro);
        // "otro" queda desactivado pero conserva su token de acceso vigente
        await Modificar(factory, otro.Id, u => u.Activo = false);

        var res = await cliente.PatchAsync($"/api/Usuarios/{activo.Id}/desactivar", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    private static Task<int> Modificar(DentissaFactory factory, int id, Action<Usuario> cambio)
        => factory.ConDbAsync(async db =>
        {
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == id);
            cambio(usuario);
            return await db.SaveChangesAsync();
        });
}
