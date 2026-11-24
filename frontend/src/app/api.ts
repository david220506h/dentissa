import { API_URL, authFetch } from '../services/authService';

/** Error de la API con su código HTTP y, si lo trae, el código de la API ("HORARIO_OCUPADO"). */
export class ErrorApi extends Error {
  constructor(
    readonly estado: number,
    mensaje: string,
    readonly codigo?: string,
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
  if (!respuesta.ok) {
    const datos = (await respuesta.json().catch(() => null)) as { mensaje?: string; codigo?: string } | null;
    throw new ErrorApi(respuesta.status, datos?.mensaje ?? `La API respondió ${respuesta.status}`, datos?.codigo);
  }
  return (await respuesta.json()) as T;
}

/**
 * Envío autenticado (PATCH, POST, PUT, DELETE). Si la API responde con error, usa su mensaje ("Solo se puede
 * cancelar una cita pendiente o confirmada") y su código; si no trae mensaje, uno genérico.
 */
export async function enviarJson<T = unknown>(ruta: string, metodo: 'POST' | 'PUT' | 'PATCH' | 'DELETE', cuerpo?: unknown): Promise<T> {
  const respuesta = await authFetch(`${API_URL}${ruta}`, {
    method: metodo,
    ...(cuerpo === undefined ? {} : { body: JSON.stringify(cuerpo) }),
  });
  const datos = (await respuesta.json().catch(() => null)) as { mensaje?: string; codigo?: string; detail?: string } | null;
  if (!respuesta.ok)
    throw new ErrorApi(respuesta.status, datos?.mensaje ?? datos?.detail ?? 'No se pudo completar la acción. Inténtalo de nuevo.', datos?.codigo);
  return datos as T;
}

export const mensajeDe = (e: unknown) => (e instanceof Error ? e.message : 'No se pudo completar la acción. Inténtalo de nuevo.');
