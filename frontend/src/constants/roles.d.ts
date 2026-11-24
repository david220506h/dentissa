// Tipos de constants/roles.js (fuente única de los nombres de rol del frontend, según CLAUDE.md).
export const ROLES: {
  readonly ADMIN: 'admin';
  readonly ASISTENTE: 'asistente';
  readonly DENTISTA: 'dentista';
  readonly PACIENTE: 'paciente';
};
export const ROLES_RECEPCION: string[];
export const ROLES_PANEL: string[];
export function puedeVerPanel(usuario: { rol: string } | null | undefined): boolean;
