import type { NombreIcono } from '@dentissa/ui';
import { ROLES, ROLES_PANEL } from '../constants/roles';

// Matriz de permisos del panel (docs/design/panel/Main.dc.html · "Menú por rol").
// Es la única fuente del menú y de las guardas de ruta. El backend es la autoridad: esto solo
// oculta lo que no corresponde y evita pantallas que la API rechazaría.

export type RolPanel = typeof ROLES.ADMIN | typeof ROLES.ASISTENTE | typeof ROLES.DENTISTA;

export type IdSeccion =
  | 'inicio'
  | 'agenda'
  | 'solicitudes'
  | 'pacientes'
  | 'expediente'
  | 'servicios'
  | 'ofertas'
  | 'inventario'
  | 'pagos'
  | 'reportes'
  | 'equipo'
  | 'configuracion'
  | 'auditoria';

export interface Seccion {
  id: IdSeccion;
  etiqueta: string;
  icono: NombreIcono;
  ruta: string;
  grupo: 'Principal' | 'Clínica' | 'Administración';
  /** Lo que dice Main.dc.html para cada rol ("—" = sin acceso). */
  matriz: Record<RolPanel, string>;
}

export const SECCIONES: Seccion[] = [
  { id: 'inicio', etiqueta: 'Inicio', icono: 'inicio', ruta: '/', grupo: 'Principal',
    matriz: { admin: 'Sí', asistente: 'Sí', dentista: 'Sí' } },
  { id: 'agenda', etiqueta: 'Agenda', icono: 'agenda', ruta: '/agenda', grupo: 'Principal',
    matriz: { admin: 'Completa', asistente: 'Completa', dentista: 'Solo la propia' } },
  { id: 'solicitudes', etiqueta: 'Solicitudes', icono: 'solicitudes', ruta: '/solicitudes', grupo: 'Principal',
    matriz: { admin: 'Sí', asistente: 'Sí', dentista: '—' } },
  { id: 'pacientes', etiqueta: 'Pacientes', icono: 'pacientes', ruta: '/pacientes', grupo: 'Principal',
    matriz: { admin: 'Sí', asistente: 'Sí', dentista: 'Los asignados' } },
  { id: 'expediente', etiqueta: 'Expediente clínico', icono: 'expediente', ruta: '/expediente', grupo: 'Clínica',
    matriz: { admin: 'Solo lectura', asistente: '—', dentista: 'Edita' } },
  { id: 'servicios', etiqueta: 'Servicios', icono: 'servicios', ruta: '/servicios', grupo: 'Clínica',
    matriz: { admin: 'Sí', asistente: '—', dentista: '—' } },
  { id: 'ofertas', etiqueta: 'Ofertas y contenido', icono: 'ofertas', ruta: '/ofertas', grupo: 'Clínica',
    matriz: { admin: 'Sí', asistente: '—', dentista: '—' } },
  { id: 'inventario', etiqueta: 'Inventario', icono: 'inventario', ruta: '/inventario', grupo: 'Clínica',
    matriz: { admin: 'Completo', asistente: 'Consulta y movimientos', dentista: 'Consulta y consumo' } },
  { id: 'pagos', etiqueta: 'Pagos', icono: 'pagos', ruta: '/pagos', grupo: 'Clínica',
    matriz: { admin: 'Completo', asistente: 'Registra', dentista: '—' } },
  { id: 'reportes', etiqueta: 'Reportes', icono: 'reportes', ruta: '/reportes', grupo: 'Administración',
    matriz: { admin: 'Completos', asistente: '—', dentista: 'Los propios' } },
  { id: 'equipo', etiqueta: 'Equipo y usuarios', icono: 'equipo', ruta: '/equipo', grupo: 'Administración',
    matriz: { admin: 'Sí', asistente: '—', dentista: '—' } },
  { id: 'configuracion', etiqueta: 'Configuración', icono: 'configuracion', ruta: '/configuracion', grupo: 'Administración',
    matriz: { admin: 'Sí', asistente: '—', dentista: '—' } },
  { id: 'auditoria', etiqueta: 'Auditoría', icono: 'auditoria', ruta: '/auditoria', grupo: 'Administración',
    matriz: { admin: 'Solo lectura', asistente: '—', dentista: '—' } },
];

/**
 * Restricción temporal (plan de la Fase 3a, decisión 14.2): el dentista solo ve lo que ya tiene pantalla y
 * backend para su rol: Inicio y Pacientes (los asignados, Fase 2b). Su agenda, expediente, inventario y
 * reportes se agregan al tenerlos.
 */
const TEMPORAL: Partial<Record<RolPanel, ReadonlySet<IdSeccion>>> = {
  [ROLES.DENTISTA]: new Set<IdSeccion>(['inicio', 'pacientes']),
};

/** Roles que entran al panel: la lista vive en constants/roles.js. */
export function esRolPanel(rol: string | undefined | null): rol is RolPanel {
  return !!rol && ROLES_PANEL.includes(rol);
}

/** ¿Puede este rol ver la sección? (matriz de Main, más la restricción temporal del dentista). */
export function puedeVer(rol: RolPanel, seccion: IdSeccion): boolean {
  const temporal = TEMPORAL[rol];
  if (temporal && !temporal.has(seccion)) return false;
  const s = SECCIONES.find((x) => x.id === seccion);
  return !!s && s.matriz[rol] !== '—';
}

export function seccionesDe(rol: RolPanel): Seccion[] {
  return SECCIONES.filter((s) => puedeVer(rol, s.id));
}

/** Texto del rol en el pie del menú (neutro en género). */
export const NOMBRE_ROL: Record<RolPanel, string> = {
  admin: 'Administración',
  asistente: 'Asistente',
  dentista: 'Dentista',
};
