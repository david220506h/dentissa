import type { EstadoCita } from '@dentissa/ui';

/** Lo que Inicio usa de CitaRespuestaDto (GET /api/citas y /api/citas/mi-agenda). */
export interface CitaApi {
  id: number;
  paciente: string;
  servicio: string;
  dentista: string;
  /** Fecha local de la clínica, "dd/MM/yyyy". */
  fecha: string;
  /** Hora local de la clínica, "HH:mm". */
  hora: string;
  /** ISO 8601 en UTC. */
  inicio: string;
  fin: string;
  estado: EstadoCita;
}

/**
 * Zona de la clínica. Es la misma que `Clinica:ZonaHoraria` del backend (appsettings); la API ya
 * entrega fecha y hora locales, y esta constante solo sirve para saber qué día es "hoy" y para la hora
 * de fin. Cuando la configuración llegue por la API (fase de Configuración), se lee de ahí.
 */
export const ZONA_CLINICA = 'America/Mexico_City';

const partes = (fecha: Date, opciones: Intl.DateTimeFormatOptions) =>
  Object.fromEntries(
    new Intl.DateTimeFormat('es-MX', { timeZone: ZONA_CLINICA, ...opciones })
      .formatToParts(fecha)
      .map((p) => [p.type, p.value]),
  );

/** "Hoy" en la clínica con el formato de la API: "18/11/2026". */
export function hoyEnClinica(ahora: Date): string {
  const p = partes(ahora, { day: '2-digit', month: '2-digit', year: 'numeric' });
  return `${p.day}/${p.month}/${p.year}`;
}

/** Fecha para pedir mi-agenda: "2026-11-18". */
export function hoyIsoEnClinica(ahora: Date): string {
  const [d, m, a] = hoyEnClinica(ahora).split('/');
  return `${a}-${m}-${d}`;
}

const conMayuscula = (t: string) => t.charAt(0).toLocaleUpperCase('es-MX') + t.slice(1);

/** "Miércoles 18 de noviembre de 2026" (escritorio) o sin el año (celular), como en el diseño. */
export function fechaLarga(ahora: Date, conAnio: boolean): string {
  const texto = new Intl.DateTimeFormat('es-MX', {
    timeZone: ZONA_CLINICA,
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    ...(conAnio ? { year: 'numeric' } : {}),
  }).format(ahora);
  return conMayuscula(texto.replace(',', ''));
}

/** "Buenos días" hasta las 12:00, "Buenas tardes" hasta las 19:00 y "Buenas noches" después (hora de la clínica). */
export function saludo(ahora: Date): string {
  const hora = Number(partes(ahora, { hour: '2-digit', hourCycle: 'h23' }).hour);
  if (hora < 12) return 'Buenos días';
  if (hora < 19) return 'Buenas tardes';
  return 'Buenas noches';
}

/** "08:00" → "8:00", como escribe el diseño. */
export const horaCorta = (hhmm: string) => hhmm.replace(/^0(\d)/, '$1');

/** Hora local de la clínica de un instante UTC: "14:00". */
function horaDe(iso: string): string {
  const p = partes(new Date(iso), { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' });
  return horaCorta(`${p.hour}:${p.minute}`);
}

/** "8:00" para una cita de un horario; "12:00 a 14:00" si ocupa más de uno (Inicio). */
export function rangoHorario(cita: Pick<CitaApi, 'hora' | 'inicio' | 'fin'>): string {
  const minutos = (new Date(cita.fin).getTime() - new Date(cita.inicio).getTime()) / 60_000;
  return minutos > 60 ? `${horaCorta(cita.hora)} a ${horaDe(cita.fin)}` : horaCorta(cita.hora);
}

/** Citas de hoy, en orden, sin las canceladas (liberan el horario). */
export function citasDeHoy(citas: CitaApi[], ahora: Date): CitaApi[] {
  const hoy = hoyEnClinica(ahora);
  return citas
    .filter((c) => c.fecha === hoy && c.estado !== 'cancelada')
    .sort((a, b) => new Date(a.inicio).getTime() - new Date(b.inicio).getTime());
}

/** Citas pendientes que aún no ocurren: recepción debe confirmarlas (correo o llamada). */
export function citasSinConfirmar(citas: CitaApi[], ahora: Date): number {
  return citas.filter((c) => c.estado === 'pendiente' && new Date(c.inicio).getTime() > ahora.getTime()).length;
}
