using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using backend.Entities;

namespace backend.Services.Correo
{
    /// <summary>Datos que una plantilla puede usar. Nunca datos de salud, CURP, motivos ni el nombre del dentista (7.2).</summary>
    public sealed record DatosCorreo(
        string? Servicio,
        DateOnly? Fecha,
        TimeOnly? Hora,
        DateOnly? FechaAnterior = null,
        TimeOnly? HoraAnterior = null,
        string? EnlaceConfirmar = null,
        string? EnlaceGestionar = null);

    /// <summary>Plantillas en español de México y género neutro (diseño, 7.2): texto plano y HTML.</summary>
    public static class PlantillasCorreoRender
    {
        private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-MX");

        public static string Fecha(DateOnly f) => f.ToString("dddd d 'de' MMMM 'de' yyyy", Es);

        public static string Titulo(string plantilla) => plantilla switch
        {
            PlantillasCorreo.SolicitudRecibida       => "Recibimos tu solicitud de cita",
            PlantillasCorreo.Confirmacion            => "Tu cita está confirmada",
            PlantillasCorreo.Recordatorio            => "Recordatorio: tu cita es mañana",
            PlantillasCorreo.Cambio                  => "Tu cita cambió de horario",
            PlantillasCorreo.Cancelacion             => "Tu cita fue cancelada",
            PlantillasCorreo.SolicitudCambioRecibida => "Recibimos tu solicitud de cambio",
            PlantillasCorreo.Vencimiento             => "Tu solicitud de cita no se confirmó a tiempo",
            PlantillasCorreo.TopePendientes          => "Ya tienes solicitudes de cita pendientes",
            PlantillasCorreo.IntentoRegistro         => "Alguien intentó crear una cuenta con tu correo",
            _ => throw new ArgumentException("Plantilla desconocida", nameof(plantilla)),
        };

        /// <summary>Devuelve asunto, texto y HTML. Cada línea es un párrafo; los enlaces van aparte.</summary>
        public static (string Asunto, string Texto, string Html) Renderizar(string plantilla, DatosCorreo d, OpcionesClinica c)
        {
            var clinica = string.IsNullOrWhiteSpace(c.Nombre) ? "la clínica" : c.Nombre.Trim();
            var llamanos = string.IsNullOrWhiteSpace(c.Telefono) ? "llámanos" : $"llámanos al {c.Telefono.Trim()}";
            var cuando = d.Fecha is { } f && d.Hora is { } h ? $"{Fecha(f)} a las {h:HH\\:mm}" : null;
            var lineas = new List<string> { "Hola:" };
            var enlaces = new List<(string Texto, string Url)>();
            void Detalle()
            {
                if (d.Servicio is not null) lineas.Add($"Servicio: {d.Servicio}");
                if (cuando is not null) lineas.Add($"Cuándo: {cuando}");
            }
            void Gestionar()
            {
                if (d.EnlaceGestionar is not null) enlaces.Add(("Para cambiar o cancelar tu cita, abre este enlace", d.EnlaceGestionar));
            }

            switch (plantilla)
            {
                case PlantillasCorreo.SolicitudRecibida:
                    lineas.Add($"Recibimos tu solicitud de cita en {clinica}.");
                    Detalle();
                    lineas.Add("Tu cita aún no está confirmada.");
                    if (d.EnlaceConfirmar is not null) enlaces.Add(("Para confirmarla, abre este enlace", d.EnlaceConfirmar));
                    else lineas.Add("Te llamaremos para confirmarla.");
                    Gestionar();
                    break;
                case PlantillasCorreo.Confirmacion:
                case PlantillasCorreo.Recordatorio:
                    lineas.Add(plantilla == PlantillasCorreo.Confirmacion
                        ? $"Tu cita en {clinica} está confirmada."
                        : $"Te recordamos tu cita de mañana en {clinica}.");
                    Detalle();
                    if (!string.IsNullOrWhiteSpace(c.Direccion)) lineas.Add($"Dirección: {c.Direccion.Trim()}");
                    Gestionar();
                    if (d.EnlaceGestionar is null) lineas.Add($"Si necesitas cambiarla o cancelarla, {llamanos}.");
                    break;
                case PlantillasCorreo.Cambio:
                    lineas.Add($"Tu cita en {clinica} cambió de horario.");
                    if (d.FechaAnterior is { } fa && d.HoraAnterior is { } ha) lineas.Add($"Antes: {Fecha(fa)} a las {ha:HH\\:mm}");
                    if (cuando is not null) lineas.Add($"Ahora: {cuando}");
                    if (d.Servicio is not null) lineas.Add($"Servicio: {d.Servicio}");
                    Gestionar();
                    break;
                case PlantillasCorreo.Cancelacion:
                    lineas.Add(cuando is null ? $"Cancelamos tu cita en {clinica}." : $"Cancelamos tu cita en {clinica} del {cuando}.");
                    lineas.Add($"Si fue un error, {llamanos}.");
                    break;
                case PlantillasCorreo.SolicitudCambioRecibida:
                    lineas.Add("Recibimos tu solicitud de cambio para tu cita.");
                    Detalle();
                    lineas.Add("Te llamaremos para confirmarlo.");
                    break;
                case PlantillasCorreo.Vencimiento:
                    lineas.Add(cuando is null
                        ? "Tu solicitud de cita no se confirmó a tiempo, así que liberamos el horario."
                        : $"Tu solicitud de cita del {cuando} no se confirmó a tiempo, así que liberamos el horario.");
                    lineas.Add($"Puedes solicitar otro horario o {llamanos}.");
                    break;
                case PlantillasCorreo.TopePendientes:
                    lineas.Add("Ya tienes solicitudes de cita pendientes de confirmar.");
                    lineas.Add($"Confírmalas con el enlace de su correo o {llamanos}.");
                    break;
                case PlantillasCorreo.IntentoRegistro:
                    lineas.Add($"Alguien intentó crear una cuenta en {clinica} con tu correo.");
                    lineas.Add("Si fuiste tú, inicia sesión o recupera tu contraseña. Si no, puedes ignorar este mensaje.");
                    break;
                default:
                    throw new ArgumentException("Plantilla desconocida", nameof(plantilla));
            }

            var pie = new List<string> { clinica };
            if (!string.IsNullOrWhiteSpace(c.Direccion)) pie.Add(c.Direccion.Trim());
            if (!string.IsNullOrWhiteSpace(c.Telefono)) pie.Add($"Teléfono: {c.Telefono.Trim()}");
            if (!string.IsNullOrWhiteSpace(c.WhatsApp)) pie.Add($"WhatsApp: {c.WhatsApp.Trim()}");
            pie.Add("Este correo se envió automáticamente; no lo respondas.");

            var texto = new StringBuilder();
            foreach (var l in lineas) texto.AppendLine(l).AppendLine();
            foreach (var (t, u) in enlaces) texto.AppendLine($"{t}:").AppendLine(u).AppendLine();
            texto.AppendLine("—");
            foreach (var p in pie) texto.AppendLine(p);

            var e = HtmlEncoder.Default;
            var html = new StringBuilder("<!doctype html><html lang=\"es-MX\"><body style=\"font-family:Arial,sans-serif;line-height:1.5\">");
            foreach (var l in lineas) html.Append("<p>").Append(e.Encode(l)).Append("</p>");
            foreach (var (t, u) in enlaces) html.Append("<p><a href=\"").Append(e.Encode(u)).Append("\">").Append(e.Encode(t)).Append("</a></p>");
            html.Append("<hr><p style=\"color:#555;font-size:13px\">");
            html.Append(string.Join("<br>", pie.Select(p => e.Encode(p))));
            html.Append("</p></body></html>");

            return (Titulo(plantilla), texto.ToString(), html.ToString());
        }
    }
}
