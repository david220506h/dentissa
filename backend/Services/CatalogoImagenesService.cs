using System.Text.RegularExpressions;

namespace backend.Services
{
    public class CatalogoImagenesService : ICatalogoImagenesService
    {
        // Solo nombres generados por el servidor: 32 hex + extensión permitida
        private static readonly Regex NombreValido =
            new("^[0-9a-f]{32}\\.(jpg|png|webp)$", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> ContentTypes = new()
        {
            [".jpg"]  = "image/jpeg",
            [".png"]  = "image/png",
            [".webp"] = "image/webp",
        };

        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly string _carpeta;
        private readonly long _tamanoMaximo;

        public CatalogoImagenesService(IConfiguration config, IWebHostEnvironment env)
        {
            var ruta = config["CatalogoImagenes:Ruta"];
            if (string.IsNullOrWhiteSpace(ruta))
                ruta = Path.Combine("storage", "catalogo");

            _carpeta = Path.GetFullPath(Path.Combine(env.ContentRootPath, ruta));
            _tamanoMaximo = config.GetValue<long?>("CatalogoImagenes:TamanoMaximoBytes") ?? 5 * 1024 * 1024;

            Directory.CreateDirectory(_carpeta);
        }

        public async Task<string> GuardarAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                throw new ArgumentException("Selecciona una imagen.");

            if (archivo.Length > _tamanoMaximo)
                throw new ArgumentException($"La imagen supera el tamaño máximo de {_tamanoMaximo / (1024 * 1024)} MB.");

            var extensionOriginal = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extensionOriginal))
                throw new ArgumentException("Formato no permitido. Usa JPG, PNG o WEBP.");

            // El tipo real se decide por la firma de los bytes, no por el nombre ni el Content-Type
            var cabecera = new byte[12];
            int leidos;
            await using (var lectura = archivo.OpenReadStream())
                leidos = await lectura.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);

            var extension = DetectarExtension(cabecera.AsSpan(0, leidos))
                ?? throw new ArgumentException("El archivo no es una imagen JPG, PNG o WEBP válida.");

            var nombre = Guid.NewGuid().ToString("N") + extension;
            var destino = Path.Combine(_carpeta, nombre);

            await using (var salida = new FileStream(destino, FileMode.CreateNew, FileAccess.Write))
            await using (var entrada = archivo.OpenReadStream())
                await entrada.CopyToAsync(salida);

            return nombre;
        }

        public (string Ruta, string ContentType)? Obtener(string nombre)
        {
            if (string.IsNullOrEmpty(nombre) || !NombreValido.IsMatch(nombre))
                return null;

            var ruta = Path.Combine(_carpeta, nombre);
            if (!File.Exists(ruta))
                return null;

            return (ruta, ContentTypes[Path.GetExtension(nombre)]);
        }

        private static string? DetectarExtension(ReadOnlySpan<byte> b)
        {
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
                return ".jpg";

            if (b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return ".png";

            // RIFF....WEBP
            if (b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8))
                return ".webp";

            return null;
        }
    }
}
