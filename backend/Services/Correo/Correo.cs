using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace backend.Services.Correo
{
    /// <summary>
    /// Configuración del correo (diseño de la agenda, 7.1). Sin valores en el repositorio: se define con
    /// user-secrets o variables de entorno (en desarrollo, apuntando a Mailpit; ver docs/correo/desarrollo.md).
    /// </summary>
    public class OpcionesCorreo
    {
        public bool Habilitado { get; set; }
        public string? Remitente { get; set; }
        public string? NombreRemitente { get; set; }
        public OpcionesSmtp Smtp { get; set; } = new();

        // Trabajador de la bandeja de salida
        public int IntervaloSegundos { get; set; } = 30;
        public int Lote { get; set; } = 20;
        public int ArrendamientoMinutos { get; set; } = 10;
        // Una fila más antigua ya no se envía (p. ej., tras tener el correo apagado): se descarta
        public int MaxAntiguedadHoras { get; set; } = 48;
        public int RecordatoriosMinutos { get; set; } = 60;
    }

    public class OpcionesSmtp
    {
        public string? Host { get; set; }
        public int Puerto { get; set; } = 587;
        // Ninguna | StartTls | SslAlConectar | Auto
        public string Seguridad { get; set; } = "StartTls";
        public string? Usuario { get; set; }
        public string? Password { get; set; }
    }

    /// <summary>Datos de la clínica para los correos (decisión 21). UrlPublica vacía: los correos van sin enlaces.</summary>
    public class OpcionesClinica
    {
        public string? Nombre { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? WhatsApp { get; set; }
        public string? UrlPublica { get; set; }
    }

    public sealed record MensajeCorreo(string Para, string Asunto, string CuerpoTexto, string CuerpoHtml);

    public interface IEmailSender
    {
        Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default);
    }

    /// <summary>Envío por SMTP con MailKit. En desarrollo, Mailpit; el proveedor real se decide antes del despliegue.</summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IOptionsMonitor<OpcionesCorreo> _opciones;

        public SmtpEmailSender(IOptionsMonitor<OpcionesCorreo> opciones) => _opciones = opciones;

        public static SecureSocketOptions Seguridad(string? valor) => valor?.Trim().ToLowerInvariant() switch
        {
            "ninguna" => SecureSocketOptions.None,
            "sslalconectar" => SecureSocketOptions.SslOnConnect,
            "auto" => SecureSocketOptions.Auto,
            _ => SecureSocketOptions.StartTls,
        };

        public async Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default)
        {
            var o = _opciones.CurrentValue;
            if (string.IsNullOrWhiteSpace(o.Smtp.Host) || string.IsNullOrWhiteSpace(o.Remitente))
                throw new InvalidOperationException("Correo sin configurar: faltan Correo:Smtp:Host o Correo:Remitente.");

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(o.NombreRemitente ?? "", o.Remitente));
            mime.To.Add(MailboxAddress.Parse(mensaje.Para));
            mime.Subject = mensaje.Asunto;
            mime.Body = new BodyBuilder { TextBody = mensaje.CuerpoTexto, HtmlBody = mensaje.CuerpoHtml }.ToMessageBody();

            using var cliente = new SmtpClient();
            await cliente.ConnectAsync(o.Smtp.Host, o.Smtp.Puerto, Seguridad(o.Smtp.Seguridad), ct);
            if (!string.IsNullOrEmpty(o.Smtp.Usuario))
                await cliente.AuthenticateAsync(o.Smtp.Usuario, o.Smtp.Password ?? "", ct);
            await cliente.SendAsync(mime, ct);
            await cliente.DisconnectAsync(true, ct);
        }
    }
}
