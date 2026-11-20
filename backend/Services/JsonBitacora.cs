using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace backend.Services
{
    // Serialización de bitácora e historial: conserva acentos y "ñ" legibles
    // (sigue escapando los caracteres especiales de HTML)
    public static class JsonBitacora
    {
        private static readonly JsonSerializerOptions Opciones = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        public static string Serializar(object valor) => JsonSerializer.Serialize(valor, Opciones);
    }
}
