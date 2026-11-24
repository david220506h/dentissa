// Tipos del cliente de autenticación (authService.js). La lógica vive solo en el .js, que también usa la
// landing; este archivo solo describe su interfaz para el código TypeScript del panel nuevo.

export interface UsuarioSesion {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  telefono?: string | null;
  rol: string;
  fechaRegistro?: string | null;
}

/** Base de la API: VITE_API_URL o "/api" (proxy de Vite en desarrollo). */
export const API_URL: string;
export const MENSAJE_SIN_PERMISO: string;
export const MENSAJE_SESION_EXPIRADA: string;

export function getUsuario(): UsuarioSesion | null;
export function clearSession(): void;
export function authFetch(
  url: string,
  options?: RequestInit,
  opciones?: { notificar?: boolean },
): Promise<Response>;
export function login(email: string, password: string): Promise<UsuarioSesion>;
export function logout(): Promise<void>;
export function esErrorDeAcceso(err: unknown): boolean;
