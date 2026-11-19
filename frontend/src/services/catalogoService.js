// Imágenes del catálogo de servicios (se suben al backend, nunca a terceros)
import { authFetch } from "./authService";

const API = "/api/catalogo/imagenes";

export const TIPOS_IMAGEN = ["image/jpeg", "image/png", "image/webp"];
export const TAMANO_MAXIMO_IMAGEN = 5 * 1024 * 1024;

// Validación rápida en el navegador; la validación definitiva la hace el backend
export const validarImagen = (file) => {
  if (!TIPOS_IMAGEN.includes(file.type)) return "Formato no permitido. Usa JPG, PNG o WEBP.";
  if (file.size > TAMANO_MAXIMO_IMAGEN) return "La imagen supera el tamaño máximo de 5 MB.";
  return null;
};

// Sube la imagen y devuelve la URL con la que se sirve
export const subirImagenCatalogo = async (file) => {
  const body = new FormData();
  body.append("archivo", file);

  const res = await authFetch(API, { method: "POST", body });

  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new Error(data?.mensaje || "No se pudo subir la imagen");
  }

  const data = await res.json();
  return data.url;
};
