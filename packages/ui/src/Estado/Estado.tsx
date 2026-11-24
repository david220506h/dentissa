import type { ReactNode } from 'react';
import { Icono, type NombreIcono } from '../Icono/Icono';
import estilos from './Estado.module.css';

export interface MensajeEstadoProps {
  icono: NombreIcono;
  titulo: string;
  /** Qué pasó y qué hacer, sin códigos técnicos (Estados). */
  texto: string;
  /** Siguiente paso: un botón ("Nueva cita", "Reintentar", "Volver al inicio"). */
  accion?: ReactNode;
  /** Nivel del título: h1 cuando el estado ocupa la página (Sin permiso), h2/h3 dentro de una sección. */
  nivel?: 1 | 2 | 3;
}

/** Vacío, error de carga y sin permiso (Estados.dc.html): ícono en círculo, título, texto y acción. */
export function MensajeEstado({ icono, titulo, texto, accion, nivel = 2 }: MensajeEstadoProps) {
  const Titulo = `h${nivel}` as const;
  return (
    <div className={estilos.mensaje}>
      <span className={estilos.circulo}>
        <Icono nombre={icono} tamano={26} />
      </span>
      <Titulo className={estilos.titulo}>{titulo}</Titulo>
      <p className={estilos.texto}>{texto}</p>
      {accion}
    </div>
  );
}

export interface CargandoListaProps {
  /** Lo que se carga, para el lector de pantalla ("Cargando citas"). */
  etiqueta: string;
  filas?: number;
}

/**
 * Esqueleto con la forma final de una lista (Estados). Estático: sin brillo ni pulso
 * (especificación de movimiento: "sin esqueletos con brillo"), así que no depende de reduced-motion.
 */
export function CargandoLista({ etiqueta, filas = 3 }: CargandoListaProps) {
  return (
    <div role="status" aria-label={etiqueta} className={estilos.lista}>
      {Array.from({ length: filas }, (_, i) => (
        <div key={i} className={estilos.fila}>
          <span aria-hidden="true" className={estilos.avatar} />
          <div className={estilos.lineas}>
            <span aria-hidden="true" className={estilos.lineaLarga} />
            <span aria-hidden="true" className={estilos.lineaCorta} />
          </div>
          <span aria-hidden="true" className={estilos.insignia} />
        </div>
      ))}
    </div>
  );
}
