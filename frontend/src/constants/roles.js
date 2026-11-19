// Nombres de rol (deben coincidir con backend/Constants/Roles.cs).
// Ocultar pantallas es solo comodidad: la autorización real la hace el backend.
export const ROLES = {
  ADMIN: "admin",
  SECRETARIA: "secretaria",
  CLIENTE: "cliente",
};

export const ROLES_PANEL = [ROLES.ADMIN, ROLES.SECRETARIA];

export const puedeVerPanel = (usuario) => !!usuario && ROLES_PANEL.includes(usuario.rol);
