import type { EstadoCita } from '@dentissa/ui';

// Pacientes (Pacientes.dc.html y Movil2 · Pacientes en tarjetas). Datos de GET /api/pacientes (Fase 2b).

export interface CitaResumen {
  id: number;
  /** Fecha local de la clínica, "YYYY-MM-DD". */
  fecha: string;
  hora: string;
  estado: EstadoCita;
  servicio: string;
}

export interface PacienteLista {
  id: number;
  nombre: string;
  apellido: string;
  telefono: string | null;
  cuenta: 'con-cuenta' | 'invitado';
  alergias: string | null;
  ultimaCita: CitaResumen | null;
  proximaCita: CitaResumen | null;
}

export interface PacienteDetalle extends Omit<PacienteLista, 'proximaCita'> {
  correo: string | null;
  fechaNacimiento: string | null;
  sexo: Sexo | null;
  proximasCitas: CitaResumen[];
}

export interface PaginaPacientes {
  elementos: PacienteLista[];
  total: number;
  pagina: number;
  tamano: number;
}

export type Sexo = 'femenino' | 'masculino' | 'otro' | 'no_especifica';

export const SEXOS: { valor: Sexo | ''; texto: string }[] = [
  { valor: '', texto: 'Sin especificar' },
  { valor: 'femenino', texto: 'Femenino' },
  { valor: 'masculino', texto: 'Masculino' },
  { valor: 'otro', texto: 'Otro' },
  { valor: 'no_especifica', texto: 'Prefiere no decir' },
];

export const TAMANO_PAGINA = 10;

const MESES_CORTOS = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

/** "2026-12-14" → "14 dic 2026". */
export function fechaCorta(iso: string): string {
  const [a, m, d] = iso.split('-').map(Number);
  return `${d} ${MESES_CORTOS[m! - 1]} ${a}`;
}

export const nombreCompleto = (p: { nombre: string; apellido: string }) => `${p.nombre} ${p.apellido}`;

/** Rango de la página para "Mostrando 1 a 10 de 31". */
export function rangoPagina(pagina: number, tamano: number, total: number, enPagina: number) {
  const desde = total === 0 ? 0 : (pagina - 1) * tamano + 1;
  return { desde, hasta: desde === 0 ? 0 : desde + enPagina - 1, hayAnterior: pagina > 1, haySiguiente: pagina * tamano < total };
}

/** La búsqueda se envía con 2 caracteres o más (la API rechaza menos). */
export const terminoValido = (t: string) => {
  const limpio = t.trim();
  return limpio.length >= 2 ? limpio : '';
};

export interface FormularioPaciente {
  nombre: string;
  apellido: string;
  telefono: string;
  correo: string;
  fechaNacimiento: string;
  sexo: Sexo | '';
  alergias: string;
}

export const FORMULARIO_VACIO: FormularioPaciente = { nombre: '', apellido: '', telefono: '', correo: '', fechaNacimiento: '', sexo: '', alergias: '' };

export function formularioDe(p: PacienteDetalle): FormularioPaciente {
  return {
    nombre: p.nombre,
    apellido: p.apellido,
    telefono: p.telefono ?? '',
    correo: p.correo ?? '',
    fechaNacimiento: p.fechaNacimiento ?? '',
    sexo: p.sexo ?? '',
    alergias: p.alergias ?? '',
  };
}

/** Validación del formulario con las mismas reglas que el backend (la autoridad). */
export function erroresFormulario(f: FormularioPaciente, hoy: string): Partial<Record<keyof FormularioPaciente, string>> {
  const e: Partial<Record<keyof FormularioPaciente, string>> = {};
  if (!f.nombre.trim()) e.nombre = 'Escribe el nombre.';
  if (!f.apellido.trim()) e.apellido = 'Escribe el apellido.';
  const digitos = f.telefono.replace(/\D/g, '');
  if (f.telefono.trim() && (digitos.length < 10 || digitos.length > 15)) e.telefono = 'El teléfono debe tener de 10 a 15 dígitos.';
  if (f.correo.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.correo.trim())) e.correo = 'Escribe un correo válido.';
  if (!f.telefono.trim() && !f.correo.trim()) e.telefono = 'Indica al menos un teléfono o un correo.';
  if (f.fechaNacimiento && (f.fechaNacimiento > hoy || f.fechaNacimiento < '1900-01-01')) e.fechaNacimiento = 'Revisa la fecha de nacimiento.';
  if (f.alergias.length > 1000) e.alergias = 'Máximo 1000 caracteres.';
  return e;
}

/** Cuerpo para POST y PUT /api/pacientes: vacíos como null. */
export function cuerpoDe(f: FormularioPaciente) {
  const o = (t: string) => (t.trim() ? t.trim() : null);
  return {
    nombre: f.nombre.trim(),
    apellido: f.apellido.trim(),
    telefono: o(f.telefono),
    correo: o(f.correo),
    fechaNacimiento: f.fechaNacimiento || null,
    sexo: f.sexo || null,
    alergias: o(f.alergias),
  };
}
