using System.Security.Cryptography;
using System.Text;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Agenda;

/// <summary>La cita ya no admite un enlace de ese propósito (cancelada, terminada o ya vencida).</summary>
public class EnlaceNoDisponibleException : InvalidOperationException
{
    public EnlaceNoDisponibleException() : base("La cita ya no admite enlaces.") { }
}

/// <summary>
/// Tokens de los enlaces del correo (diseño de la agenda, sección 5). Se emiten al enviar el correo; la base solo
/// guarda el hash. Un token es de un solo uso, vence y un reenvío invalida los anteriores del mismo propósito.
/// </summary>
public interface ITokensCitaService
{
    /// <summary>Emite un token nuevo e invalida los anteriores. Devuelve el token en claro: nunca se registra.</summary>
    Task<string> EmitirAsync(DentissaDbContext db, int citaId, string proposito, CancellationToken ct = default);

    /// <summary>Token vigente (sin usar, sin invalidar y sin vencer), sin consumirlo; null si no lo es.</summary>
    Task<CitaToken?> BuscarVigenteAsync(DentissaDbContext db, string? token, string proposito, CancellationToken ct = default);

    /// <summary>
    /// Consume el token en la transacción del cambio: UPDATE condicional (usado_en IS NULL, sin invalidar y sin
    /// vencer). Devuelve la cita, o null si el token no es válido o ya se usó.
    /// </summary>
    Task<int?> ConsumirAsync(DentissaDbContext db, string? token, string proposito, CancellationToken ct = default);
}

public class TokensCitaService : ITokensCitaService
{
    public const int BytesToken = 32;

    private readonly TimeProvider _tiempo;

    public TokensCitaService(TimeProvider tiempo) => _tiempo = tiempo;

    private DateTime Ahora => _tiempo.GetUtcNow().UtcDateTime;

    /// <summary>32 bytes aleatorios en base64url (sin relleno).</summary>
    public static string Generar() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(BytesToken))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>SHA-256 del token, en hexadecimal minúsculo (64 caracteres).</summary>
    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Confirmar: hasta min(bloqueo_expira_en, inicio). Gestionar: hasta el inicio (5.1).</summary>
    public static DateTime? Vencimiento(Cita cita, string proposito) => proposito switch
    {
        PropositosToken.Confirmar when cita.Estado == EstadosCita.Pendiente =>
            cita.BloqueoExpiraEn is { } b && b < cita.Inicio ? b : cita.Inicio,
        PropositosToken.Gestionar when EstadosCita.Activos.Contains(cita.Estado) => cita.Inicio,
        _ => null,
    };

    public async Task<string> EmitirAsync(DentissaDbContext db, int citaId, string proposito, CancellationToken ct = default)
    {
        if (!PropositosToken.Todos.Contains(proposito))
            throw new ArgumentException("Propósito de enlace inválido");
        var cita = await db.Citas.AsNoTracking().SingleAsync(c => c.Id == citaId, ct);
        var ahora = Ahora;
        if (Vencimiento(cita, proposito) is not { } expira || expira <= ahora)
            throw new EnlaceNoDisponibleException();

        await db.CitaTokens
            .Where(t => t.CitaId == citaId && t.Proposito == proposito && t.UsadoEn == null && t.InvalidadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.InvalidadoEn, ahora), ct);

        var token = Generar();
        db.CitaTokens.Add(new CitaToken
        {
            CitaId = citaId,
            Proposito = proposito,
            TokenHash = Hash(token),
            ExpiraEn = expira,
            CreadoEn = ahora,
        });
        await db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<CitaToken?> BuscarVigenteAsync(DentissaDbContext db, string? token, string proposito, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return null;
        var hash = Hash(token.Trim());
        var ahora = Ahora;
        return await db.CitaTokens.AsNoTracking().FirstOrDefaultAsync(t =>
            t.TokenHash == hash && t.Proposito == proposito && t.UsadoEn == null && t.InvalidadoEn == null && t.ExpiraEn > ahora, ct);
    }

    public async Task<int?> ConsumirAsync(DentissaDbContext db, string? token, string proposito, CancellationToken ct = default)
    {
        var vigente = await BuscarVigenteAsync(db, token, proposito, ct);
        if (vigente is null) return null;
        var ahora = Ahora;
        // Un solo uso: solo una de dos peticiones simultáneas actualiza la fila
        var usados = await db.CitaTokens
            .Where(t => t.Id == vigente.Id && t.UsadoEn == null && t.InvalidadoEn == null && t.ExpiraEn > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora), ct);
        return usados == 1 ? vigente.CitaId : null;
    }
}
