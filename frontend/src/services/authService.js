// Servicio centralizado para manejar autenticación y tokens
// solo para desarrollo a distancia 
// const API_URL = "https://receiving-talented-path-stroke.trycloudflare.com/api";


// desallo en local
const API_URL = "http://192.168.1.17:5058/api";

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

// Fetch autenticado: renueva el token automáticamente si expiró
export const authFetch = async (url, options = {}) => {
  // Si el token expiró, intentar renovarlo
  if (isTokenExpired()) {
    const renewed = await refreshAccessToken();
    if (!renewed) {
      // Refresh también expiró → sesión terminada
      window.dispatchEvent(new Event("session-expired"));
      throw new Error("Sesión expirada");
    }
  }

  const accessToken = localStorage.getItem("accessToken");

  // Con FormData el navegador pone el Content-Type (multipart con boundary)
  const esFormData = options.body instanceof FormData;

  const config = {
    ...options,
    headers: {
      ...(esFormData ? {} : { "Content-Type": "application/json" }),
      Authorization: `Bearer ${accessToken}`,
      ...options.headers
    }
  };

  return fetch(url, config);
};

// Login
export const login = async (email, password) => {
  const response = await fetch(`${API_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

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

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Error al registrar");
  }

  return await response.json();
};

// Logout
export const logout = async () => {
  try {
    await authFetch(`${API_URL}/auth/logout`, { method: "POST" });
  } finally {
    clearSession();
  }
};