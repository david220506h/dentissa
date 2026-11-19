namespace backend.Services
{
    // Imágenes públicas del catálogo de servicios. No usar para fotos del
    // expediente clínico: esas irán a un almacenamiento privado distinto.
    public interface ICatalogoImagenesService
    {
        // Valida y guarda la imagen; devuelve el nombre generado.
        // Lanza ArgumentException con un mensaje para el usuario si no es válida.
        Task<string> GuardarAsync(IFormFile archivo);

        // Devuelve la ruta física y el tipo de contenido, o null si el nombre
        // no es válido o el archivo no existe.
        (string Ruta, string ContentType)? Obtener(string nombre);
    }
}
