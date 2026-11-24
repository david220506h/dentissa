// Sitio público: lógica pura del formulario de solicitud. Mismas reglas que POST /api/publico/solicitudes
// (el backend es la autoridad).

export interface ServicioPublico {
  id: number;
  nombre: string;
  precio: number;
  duracionSlots: number;
}

export interface DiaDisponible {
  fecha: string;
  horas: string[];
}

export interface FormularioSolicitud {
  servicioId: string;
  fecha: string;
  hora: string;
  nombre: string;
  apellido: string;
  telefono: string;
  correo: string;
  motivo: string;
  aceptaAviso: boolean;
  sitioWeb: string;
}

export const FORMULARIO_VACIO: FormularioSolicitud = {
  servicioId: '',
  fecha: '',
  hora: '',
  nombre: '',
  apellido: '',
  telefono: '',
  correo: '',
  motivo: '',
  aceptaAviso: false,
  sitioWeb: '',
};

const LETRAS = /^[\p{L}][\p{L}\p{M} '.-]*$/u;

export function errores(f: FormularioSolicitud): Partial<Record<keyof FormularioSolicitud, string>> {
  const e: Partial<Record<keyof FormularioSolicitud, string>> = {};
  if (!f.servicioId) e.servicioId = 'Elige un servicio.';
  if (!f.fecha) e.fecha = 'Elige una fecha.';
  if (!f.hora) e.hora = 'Elige un horario libre.';
  if (f.nombre.trim().length < 2 || !LETRAS.test(f.nombre.trim())) e.nombre = 'Escribe tu nombre (solo letras).';
  if (f.apellido.trim().length < 2 || !LETRAS.test(f.apellido.trim())) e.apellido = 'Escribe tu apellido (solo letras).';
  const digitos = f.telefono.replace(/\D/g, '');
  if (!/^[0-9 +()\-.]+$/.test(f.telefono.trim()) || digitos.length < 10 || digitos.length > 15)
    e.telefono = 'El teléfono debe tener de 10 a 15 dígitos.';
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.correo.trim())) e.correo = 'Escribe un correo válido.';
  if (f.motivo.length > 500) e.motivo = 'Máximo 500 caracteres.';
  if (!f.aceptaAviso) e.aceptaAviso = 'Para enviar la solicitud, acepta el aviso de privacidad.';
  return e;
}

export function cuerpo(f: FormularioSolicitud) {
  return {
    servicioId: Number(f.servicioId),
    fecha: f.fecha,
    hora: f.hora,
    nombre: f.nombre.trim(),
    apellido: f.apellido.trim(),
    telefono: f.telefono.trim(),
    correo: f.correo.trim(),
    motivo: f.motivo.trim() || null,
    aceptaAviso: f.aceptaAviso,
    sitioWeb: f.sitioWeb,
  };
}

/** "2026-11-24" sumando días (fechas locales de la clínica, sin horas). */
export function sumarDias(iso: string, dias: number): string {
  const [a, m, d] = iso.split('-').map(Number);
  const f = new Date(Date.UTC(a!, m! - 1, d! + dias));
  return f.toISOString().slice(0, 10);
}

/** Hoy en la zona de la clínica. */
export function hoyEnClinica(ahora = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Mexico_City' }).format(ahora);
}

export const precio = (n: number) =>
  n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 });

/** "Lunes 30 de noviembre" */
export function fechaLarga(iso: string): string {
  const [a, m, d] = iso.split('-').map(Number);
  const t = new Date(Date.UTC(a!, m! - 1, d!)).toLocaleDateString('es-MX', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC' });
  return t.charAt(0).toUpperCase() + t.slice(1);
}
