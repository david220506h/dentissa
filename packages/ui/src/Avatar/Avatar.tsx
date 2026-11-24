import { clases } from '../utilidades/clases';
import estilos from './Avatar.module.css';

export interface AvatarProps {
  /** Nombre completo; se muestran las iniciales del primer nombre y el primer apellido. */
  nombre: string;
  /** 36 px en tablas (Equipo), 40 px en menús y tarjetas, 44 px en detalles. */
  tamano?: 36 | 40 | 44;
  className?: string;
}

/** Iniciales de un nombre: "Laura Mendoza" → "LM"; "Valeria" → "V". */
export function iniciales(nombre: string): string {
  const partes = nombre.trim().split(/\s+/).filter(Boolean);
  const primera = partes[0]?.[0] ?? '';
  const segunda = partes.length > 1 ? (partes[1]?.[0] ?? '') : '';
  return (primera + segunda).toLocaleUpperCase('es-MX');
}

/** Decorativo: el nombre siempre aparece junto al avatar, así que se oculta a los lectores de pantalla. */
export function Avatar({ nombre, tamano = 40, className }: AvatarProps) {
  return (
    <span className={clases(estilos.avatar, estilos[`t${tamano}`], className)} aria-hidden="true">
      {iniciales(nombre)}
    </span>
  );
}
