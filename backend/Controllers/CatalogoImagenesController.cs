using backend.Constants;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    // Imágenes del catálogo de servicios (públicas). No es un almacén genérico:
    // las fotos del expediente clínico nunca deben pasar por aquí.
    [ApiController]
    [Route("api/catalogo/imagenes")]
    public class CatalogoImagenesController : ControllerBase
    {
        // 5 MB de imagen + margen para el resto del cuerpo multipart
        private const long LimiteCuerpo = 5 * 1024 * 1024 + 64 * 1024;

        private readonly ICatalogoImagenesService _imagenes;

        public CatalogoImagenesController(ICatalogoImagenesService imagenes)
        {
            _imagenes = imagenes;
        }

        // ── POST: api/catalogo/imagenes ───────────────────────
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [RequestSizeLimit(LimiteCuerpo)]
        [RequestFormLimits(MultipartBodyLengthLimit = LimiteCuerpo)]
        public async Task<IActionResult> Subir(IFormFile archivo)
        {
            try
            {
                var nombre = await _imagenes.GuardarAsync(archivo);
                return Ok(new { nombre, url = $"/api/catalogo/imagenes/{nombre}" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // ── GET: api/catalogo/imagenes/{nombre} ───────────────
        [HttpGet("{nombre}")]
        [AllowAnonymous]
        public IActionResult Obtener(string nombre)
        {
            var imagen = _imagenes.Obtener(nombre);
            if (imagen == null) return NotFound();

            Response.Headers.CacheControl = "public, max-age=86400";
            return PhysicalFile(imagen.Value.Ruta, imagen.Value.ContentType);
        }
    }
}
