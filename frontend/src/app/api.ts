import { API_URL, authFetch } from '../services/authService';

/** Error de la API con su código HTTP, para decidir qué mensaje mostrar. */
export class ErrorApi extends Error {
  constructor(
    readonly estado: number,
    mensaje: string,
  ) {
    super(mensaje);
  }
}

/**
 * GET autenticado que devuelve JSON. authFetch renueva el token, y si la sesión ya no se puede renovar
 * avisa ("session-expired") y el panel vuelve al inicio de sesión. Un 403 llega como error con mensaje claro.
 */
export async function obtenerJson<T>(ruta: string): Promise<T> {
  const respuesta = await authFetch(`${API_URL}${ruta}`);
  if (!respuesta.ok) throw new ErrorApi(respuesta.status, `La API respondió ${respuesta.status}`);
  return (await respuesta.json()) as T;
}
