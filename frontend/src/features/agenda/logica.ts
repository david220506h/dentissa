import type { CitaApi } from '../inicio/datos';

// Fechas como texto "YYYY-MM-DD" (días de la clínica). La aritmética se hace en UTC para que el cambio de
// horario o la zona del navegador no muevan el día.

export interface CitaAgenda extends CitaApi {
  dentistaId: number;
  pacienteId: number;
}

export interface Dentista {
  id: number;
  nombre: string;
  apellido: string;
  titulo?: string | null;
}

const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
const MESES_CORTOS = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const DIAS_CORTOS = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
const DIAS = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'];

const aFecha = (iso: string) => {
  const [a, m, d] = iso.split('-').map(Number);
  return new Date(Date.UTC(a!, m! - 1, d!));
};
const aIso = (f: Date) => f.toISOString().slice(0, 10);

export const sumarDias = (iso: string, dias: number) => {
  const f = aFecha(iso);
  f.setUTCDate(f.getUTCDate() + dias);
  return aIso(f);
};

/** "18/11/2026" (API) → "2026-11-18". */
export const isoDeApi = (fecha: string) => {
  const [d, m, a] = fecha.split('/');
  return `${a}-${m}-${d}`;
};

/** Lunes a sábado de la semana que contiene la fecha (el domingo pertenece a la semana que termina). */
export function semanaDe(iso: string): string[] {
  const dia = aFecha(iso).getUTCDay(); // 0 = domingo
  const lunes = sumarDias(iso, dia === 0 ? -6 : 1 - dia);
  return Array.from({ length: 6 }, (_, i) => sumarDias(lunes, i));
}

export const diaCorto = (iso: string) => DIAS_CORTOS[aFecha(iso).getUTCDay()]!;
export const numeroDia = (iso: string) => aFecha(iso).getUTCDate();

/** "16 – 21 nov 2026" o "30 nov – 5 dic 2026" (barra de la agenda). */
export function rangoCorto(dias: string[]): string {
  const [i, f] = [aFecha(dias[0]!), aFecha(dias[dias.length - 1]!)];
  const mesF = MESES_CORTOS[f.getUTCMonth()];
  if (i.getUTCMonth() === f.getUTCMonth()) return `${i.getUTCDate()} – ${f.getUTCDate()} ${mesF} ${f.getUTCFullYear()}`;
  const anioI = i.getUTCFullYear() !== f.getUTCFullYear() ? ` ${i.getUTCFullYear()}` : '';
  return `${i.getUTCDate()} ${MESES_CORTOS[i.getUTCMonth()]}${anioI} – ${f.getUTCDate()} ${mesF} ${f.getUTCFullYear()}`;
}

/** "Semana del 16 al 21 de noviembre de 2026" (subtítulo). */
export function rangoLargo(dias: string[]): string {
  const [i, f] = [aFecha(dias[0]!), aFecha(dias[dias.length - 1]!)];
  const mesF = `${MESES[f.getUTCMonth()]} de ${f.getUTCFullYear()}`;
  if (i.getUTCMonth() === f.getUTCMonth()) return `Semana del ${i.getUTCDate()} al ${f.getUTCDate()} de ${mesF}`;
  const anioI = i.getUTCFullYear() !== f.getUTCFullYear() ? ` de ${i.getUTCFullYear()}` : '';
  return `Semana del ${i.getUTCDate()} de ${MESES[i.getUTCMonth()]}${anioI} al ${f.getUTCDate()} de ${mesF}`;
}

/** "Miércoles 18 de noviembre de 2026" (vista Día). */
export function diaLargo(iso: string): string {
  const f = aFecha(iso);
  const dia = DIAS[f.getUTCDay()]!;
  return `${dia.charAt(0).toLocaleUpperCase('es-MX')}${dia.slice(1)} ${f.getUTCDate()} de ${MESES[f.getUTCMonth()]} de ${f.getUTCFullYear()}`;
}

/** "Mié 2 dic 2026, 9:00" (resumen de la cita en los diálogos). */
export function fechaHoraCorta(iso: string, hora: string): string {
  const f = aFecha(iso);
  return `${DIAS_CORTOS[f.getUTCDay()]} ${f.getUTCDate()} ${MESES_CORTOS[f.getUTCMonth()]} ${f.getUTCFullYear()}, ${hora.replace(/^0(\d)/, '$1')}`;
}

const aMinutos = (hhmm: string) => {
  const [h, m] = hhmm.split(':').map(Number);
  return h! * 60 + m!;
};
const aHora = (min: number) => `${String(Math.floor(min / 60)).padStart(2, '0')}:${String(min % 60).padStart(2, '0')}`;
export const duracionMinutos = (c: Pick<CitaApi, 'inicio' | 'fin'>) => (new Date(c.fin).getTime() - new Date(c.inicio).getTime()) / 60_000;

/** Horarios de 60 minutos que ocupa una cita: "12:00" de 2 h → ["12:00", "13:00"]. */
export function horariosDeCita(c: Pick<CitaApi, 'hora' | 'inicio' | 'fin'>): string[] {
  const inicio = aMinutos(c.hora);
  const n = Math.max(1, Math.round(duracionMinutos(c) / 60));
  return Array.from({ length: n }, (_, i) => aHora(inicio + i * 60));
}

export interface HorarioDeducido {
  /** Horarios de atención del día, "HH:mm", en orden. */
  horarios: string[];
  /** Horas de descanso dentro del día, "HH:mm", en orden. */
  descansos: string[];
}

/**
 * TEMPORAL (decisión 14.1.4): deduce los horarios y los descansos con la API actual, porque aún no hay un
 * endpoint con la configuración de la agenda. Se reemplaza por esa configuración en la Fase 2 (commit 17).
 *
 * Entrada: los horarios libres de varios días futuros (GET /api/citas/horarios-disponibles; conviene empezar
 * mañana, para que la anticipación de hoy no esconda horas) y las citas de esos mismos días.
 * - Un horario de atención es una hora que estuvo libre u ocupada por una cita en **alguno** de esos días.
 * - Un descanso es una hora entre el primer y el último horario que **nunca** estuvo libre **ni** ocupada.
 * Así no se confunde con descanso un hueco con otra causa: un horario lleno (está ocupado por citas), la
 * anticipación de hoy (hoy no entra) o un día cerrado (otro día sí lo tiene libre).
 * Devuelve null si no hay datos suficientes (agenda sin configurar o sin días con horarios).
 */
export function deducirHorario(libresPorDia: Array<{ fecha: string; libres: string[] }>, citas: CitaAgenda[]): HorarioDeducido | null {
  const fechas = new Set(libresPorDia.map((d) => d.fecha));
  const atendidos = new Set<string>();
  for (const d of libresPorDia) for (const h of d.libres) atendidos.add(h);
  for (const c of citas) {
    if (c.estado === 'cancelada' || !fechas.has(isoDeApi(c.fecha))) continue;
    for (const h of horariosDeCita(c)) atendidos.add(h);
  }
  if (atendidos.size === 0) return null;
  const minutos = [...atendidos].map(aMinutos).sort((a, b) => a - b);
  const descansos: string[] = [];
  for (let m = minutos[0]!; m < minutos[minutos.length - 1]!; m += 60) if (!atendidos.has(aHora(m))) descansos.push(aHora(m));
  return { horarios: minutos.map(aHora), descansos };
}

export type FilaSemana = { tipo: 'horario' | 'descanso'; hora: string };

/** Filas de la cuadrícula: horarios y descansos en orden, más las horas con citas fuera del patrón. */
export function filasDelDia(horario: HorarioDeducido | null, citas: CitaAgenda[]): FilaSemana[] {
  const horas = new Map<string, FilaSemana['tipo']>();
  for (const h of horario?.horarios ?? []) horas.set(h, 'horario');
  for (const h of horario?.descansos ?? []) horas.set(h, 'descanso');
  // Una cita en una hora que el patrón no tiene (p. ej., una cita antigua) igual se muestra
  for (const c of citas) for (const h of horariosDeCita(c)) if (!horas.has(h)) horas.set(h, 'horario');
  return [...horas].sort(([a], [b]) => aMinutos(a) - aMinutos(b)).map(([hora, tipo]) => ({ hora, tipo }));
}

/** Fila inicial y número de filas que ocupa una cita en la cuadrícula. */
export function ubicacion(c: CitaAgenda, filas: FilaSemana[]): { fila: number; filas: number } | null {
  const inicio = filas.findIndex((f) => f.hora === horariosDeCita(c)[0]);
  if (inicio < 0) return null;
  const fin = aMinutos(c.hora) + Math.max(60, Math.round(duracionMinutos(c)));
  let n = 0;
  while (inicio + n < filas.length && aMinutos(filas[inicio + n]!.hora) < fin) n++;
  return { fila: inicio, filas: Math.max(1, n) };
}

/** Citas visibles de un día: sin canceladas (liberan el horario) y del dentista elegido. */
export function citasDelDia(citas: CitaAgenda[], iso: string, dentistaId: number | null): CitaAgenda[] {
  return citas
    .filter((c) => isoDeApi(c.fecha) === iso && c.estado !== 'cancelada' && (dentistaId === null || c.dentistaId === dentistaId))
    .sort((a, b) => new Date(a.inicio).getTime() - new Date(b.inicio).getTime());
}

export const nombreDentista = (d: Dentista) => [d.titulo, d.nombre, d.apellido].filter(Boolean).join(' ');

/** Motivos de cancelación de CitaFormularios.dc.html (se guardan en la cita y en su historial). */
export const MOTIVOS_CANCELACION = [
  'La persona avisó que no puede asistir',
  'La clínica debe reprogramar',
  'Duplicada o error de captura',
  'Otro',
] as const;
