// Nombres de rol (deben coincidir con backend/Constants/Roles.cs).
// Ocultar pantallas es solo comodidad: la autorización real la hace el backend.
export const ROLES = {
  ADMIN: "admin",
  ASISTENTE: "asistente",
  DENTISTA: "dentista",
  PACIENTE: "paciente",
};

// Recepción: gestiona citas y pacientes
export const ROLES_RECEPCION = [ROLES.ADMIN, ROLES.ASISTENTE];

// El dentista entra al panel, pero por ahora solo ve Inicio
export const ROLES_PANEL = [ROLES.ADMIN, ROLES.ASISTENTE, ROLES.DENTISTA];

export const puedeVerPanel = (usuario) => !!usuario && ROLES_PANEL.includes(usuario.rol);
