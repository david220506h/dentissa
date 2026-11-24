// Servicio centralizado para manejar autenticación y tokens
// En desarrollo se usa "/api" a través del proxy de Vite (ver vite.config.js).
// Para otro destino, definir VITE_API_URL en frontend/.env.local
export const API_URL = import.meta.env.VITE_API_URL || "/api";

export const MENSAJE_SIN_PERMISO = "No tienes permiso para realizar esta acción.";
export const MENSAJE_SESION_EXPIRADA = "Tu sesión expiró. Inicia sesión de nuevo.";
const MENSAJE_DEMASIADOS_INTENTOS = "Demasiados intentos. Espera un momento e inténtalo de nuevo.";

// Guardar tokens tras login
export const saveSession = (authResponse) => {
  localStorage.setItem("accessToken", authResponse.accessToken);
  localStorage.setItem("refreshToken", authResponse.refreshToken);
  localStorage.setItem("tokenExpiration", authResponse.accessTokenExpiration);
  localStorage.setItem("usuario", JSON.stringify(authResponse.usuario));
};

// Obtener usuario guardado
export const getUsuario = () => {
  const u = localStorage.getItem("usuario");
  return u ? JSON.parse(u) : null;
};

// Limpiar sesión
export const clearSession = () => {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem("tokenExpiration");
  localStorage.removeItem("usuario");
};

// Verificar si el access token está expirado
const isTokenExpired = () => {
  const expiration = localStorage.getItem("tokenExpiration");
  if (!expiration) return true;
  return new Date(expiration) <= new Date();
};

// Renovar el access token usando el refresh token
const refreshAccessToken = async () => {
  const refreshToken = localStorage.getItem("refreshToken");
  if (!refreshToken) return false;

  try {
    const response = await fetch(`${API_URL}/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken })
    });

    if (!response.ok) {
      clearSession();
      return false;
    }

    const data = await response.json();
    saveSession(data);
    return true;
  } catch {
    clearSession();
    return false;
  }
};

const buildConfig = (options) => {
  const accessToken = localStorage.getItem("accessToken");

  // Con FormData el navegador pone el Content-Type (multipart con boundary)
  const esFormData = options.body instanceof FormData;

  return {
    ...options,
    headers: {
      ...(esFormData ? {} : { "Content-Type": "application/json" }),
      Authorization: `Bearer ${accessToken}`,
      ...options.headers
    }
  };
};

// Sesión terminada: App escucha "session-expired" y lleva al inicio de sesión
const terminarSesion = (notificar) => {
  clearSession();
  if (notificar) window.dispatchEvent(new Event("session-expired"));
  return new Error(MENSAJE_SESION_EXPIRADA);
};

// Fetch autenticado: renueva el token si expiró o si el servidor lo rechaza (401).
// Un 403 se convierte en un error con mensaje claro.
export const authFetch = async (url, options = {}, { notificar = true } = {}) => {
  // Si el token expiró, intentar renovarlo
  if (isTokenExpired() && !(await refreshAccessToken())) throw terminarSesion(notificar);

  let response = await fetch(url, buildConfig(options));

  // Token rechazado por el servidor (revocado, rol cambiado...): un intento de renovación
  if (response.status === 401) {
    if (!(await refreshAccessToken())) throw terminarSesion(notificar);
    response = await fetch(url, buildConfig(options));
    if (response.status === 401) throw terminarSesion(notificar);
  }

  if (response.status === 403) throw new Error(MENSAJE_SIN_PERMISO);

  return response;
};

// Login
export const login = async (email, password) => {
  const response = await fetch(`${API_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

  if (response.status === 429) throw new Error(MENSAJE_DEMASIADOS_INTENTOS);
  if (!response.ok) throw new Error("Credenciales incorrectas");

  const data = await response.json();
  saveSession(data);
  return data.usuario;
};

// Register
export const register = async (nombre, apellido, email, telefono, password) => {
  const response = await fetch(`${API_URL}/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ nombre, apellido, email, telefono, password })
  });

  if (response.status === 429) throw new Error(MENSAJE_DEMASIADOS_INTENTOS);
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Error al registrar");
  }

  return await response.json();
};

// Logout
export const logout = async () => {
  try {
    // Sin aviso de "sesión expirada": el usuario está cerrando sesión a propósito
    await authFetch(`${API_URL}/auth/logout`, { method: "POST" }, { notificar: false });
  } catch {
    // Si el token ya no era válido, la sesión local se limpia igual
  } finally {
    clearSession();
  }
};

// true si el error viene de authFetch por sesión expirada o falta de permiso
export const esErrorDeAcceso = (err) =>
  [MENSAJE_SIN_PERMISO, MENSAJE_SESION_EXPIRADA].includes(err?.message);
