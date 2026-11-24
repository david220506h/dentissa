import type { CitaAgenda } from '../agenda/logica';

// Lógica pura de los formularios de cita (CitaFormularios.dc.html). El backend es la autoridad: aquí solo se
// decide qué mostrar y se valida antes de enviar (validación doble, CLAUDE.md).

export type RolPanel = 'admin' | 'asistente' | 'dentista';

/** Horario libre de GET /api/agenda/disponibilidad (reglas de recepción, con dentistas libres). */
export interface HorarioLibre {
  hora: string;
  inicio: string;
  fin: string;
  dentistasLibres: { id: number; nombre: string }[];
}

export interface Servicio {
  id: number;
  nombre: string;
  precio: number;
  duracionSlots: number;
}

export interface AccionesCita {
  confirmar: boolean;
  cancelar: boolean;
  reagendar: boolean;
  cambiarDentista: boolean;
  completar: boolean;
  noAsistio: boolean;
}

/** "No asistió" se marca desde inicio + 15 min (decisión 7 de la agenda). */
export const MINUTOS_NO_ASISTIO = 15;

/**
 * Acciones del diálogo «Cita» según rol, estado y hora (diseño de la agenda, 4.2). Recepción (admin y
 * asistente) gestiona todo; el dentista solo completa o marca inasistencia en sus citas.
 */
export function accionesDe(cita: Pick<CitaAgenda, 'estado' | 'inicio'>, rol: RolPanel, ahora: Date): AccionesCita {
  const activa = cita.estado === 'pendiente' || cita.estado === 'confirmada';
  const recepcion = rol === 'admin' || rol === 'asistente';
  const inicio = new Date(cita.inicio).getTime();
  const empezo = ahora.getTime() >= inicio;
  const paraInasistencia = ahora.getTime() >= inicio + MINUTOS_NO_ASISTIO * 60_000;
  return {
    confirmar: recepcion && cita.estado === 'pendiente',
    cancelar: recepcion && activa,
    reagendar: recepcion && activa,
    cambiarDentista: recepcion && activa && !empezo,
    completar: activa && empezo,
    noAsistio: activa && paraInasistencia,
  };
}

/** Horarios libres que se ofrecen tras un 409, sin el que ya se intentó (máximo 3, en orden). */
export function alternativas(libres: HorarioLibre[], intentado: string, maximo = 3): string[] {
  return libres.map((h) => h.hora).filter((h) => h !== intentado).slice(0, maximo);
}

/** Motivo que se guarda en la cita: el motivo elegido y, si la hay, la nota. Solo vive en la cita. */
export function motivoCancelacion(motivo: string, nota: string): string {
  const n = nota.trim();
  return n ? `${motivo}. ${n}` : motivo;
}

export const MAX_NOTA = 300;

/** "12:00 a 14:00" para una cita de varios horarios; "12:00" si dura un horario. */
export function rangoHoras(hora: string, minutos: number): string {
  const corta = (h: string) => h.replace(/^0(\d)/, '$1');
  if (minutos <= 60) return corta(hora);
  const [h, m] = hora.split(':').map(Number);
  const fin = h! * 60 + m! + minutos;
  const finTexto = `${String(Math.floor(fin / 60)).padStart(2, '0')}:${String(fin % 60).padStart(2, '0')}`;
  return `${corta(hora)} a ${corta(finTexto)}`;
}

export interface AltaRapida {
  nombre: string;
  apellido: string;
  telefono: string;
}

/** Validación del alta rápida (la misma regla que el backend: nombre, apellido y teléfono de 10 a 15 dígitos). */
export function erroresAltaRapida(a: AltaRapida): Partial<Record<keyof AltaRapida, string>> {
  const errores: Partial<Record<keyof AltaRapida, string>> = {};
  if (!a.nombre.trim()) errores.nombre = 'Escribe el nombre.';
  if (!a.apellido.trim()) errores.apellido = 'Escribe el apellido.';
  const digitos = a.telefono.replace(/\D/g, '');
  if (digitos.length < 10 || digitos.length > 15) errores.telefono = 'El teléfono debe tener de 10 a 15 dígitos.';
  return errores;
}

/** "HH:mm" → "HH:mm:00" (TimeOnly de la API). */
export const horaApi = (hora: string) => (hora.length === 5 ? `${hora}:00` : hora);

/** Texto del estado que tendrá la cita tras reagendar: conserva el actual (confirmada sigue confirmada). */
export const ESTADO_TRAS_REAGENDAR: Record<'pendiente' | 'confirmada', string> = {
  pendiente: 'Pendiente',
  confirmada: 'Confirmada',
};
