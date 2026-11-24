import type { HTMLAttributes, ReactNode } from 'react';
import { Icono, type NombreIcono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import estilos from './Insignia.module.css';

/** Parejas de color del diseño (las mismas de los estados de la cita). */
export type TonoInsignia = 'verde' | 'amarillo' | 'azul' | 'rojo' | 'gris' | 'rosa';

export interface InsigniaProps extends HTMLAttributes<HTMLSpanElement> {
  tono: TonoInsignia;
  /** Ícono de 14 px antes del texto. (La alergia no es una insignia: usa ChipAlergia.) */
  icono?: NombreIcono;
  /** Punto de color antes del texto (insignias de estado de la cita). */
  punto?: boolean;
  children: ReactNode;
}

/** Insignia genérica: "Con cuenta" (azul), "Invitado" (gris)… La alergia usa ChipAlergia, un componente aparte. */
export function Insignia({ tono, icono, punto = false, className, children, ...resto }: InsigniaProps) {
  return (
    <span className={clases(estilos.insignia, estilos[tono], punto && estilos.conPunto, className)} {...resto}>
      {punto && <span className={estilos.punto} aria-hidden="true" />}
      {icono && <Icono nombre={icono} tamano={14} trazo={2} />}
      {children}
    </span>
  );
}

/** Estados de la cita tal como los envía la API. */
export type EstadoCita = 'pendiente' | 'confirmada' | 'completada' | 'cancelada' | 'no_asistio';

export const ESTADOS_CITA: Record<EstadoCita, { texto: string; tono: TonoInsignia }> = {
  confirmada: { texto: 'Confirmada', tono: 'verde' },
  pendiente: { texto: 'Pendiente', tono: 'amarillo' },
  completada: { texto: 'Completada', tono: 'azul' },
  cancelada: { texto: 'Cancelada', tono: 'rojo' },
  no_asistio: { texto: 'No asistió', tono: 'gris' },
};

export interface InsigniaEstadoCitaProps extends Omit<HTMLAttributes<HTMLSpanElement>, 'children'> {
  estado: EstadoCita;
}

export function InsigniaEstadoCita({ estado, ...resto }: InsigniaEstadoCitaProps) {
  const { texto, tono } = ESTADOS_CITA[estado];
  return (
    <Insignia tono={tono} punto {...resto}>
      {texto}
    </Insignia>
  );
}
