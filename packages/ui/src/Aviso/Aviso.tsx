import type { ReactNode } from 'react';
import { Icono, type NombreIcono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import estilos from './Aviso.module.css';

export type TonoAviso = 'info' | 'exito' | 'advertencia' | 'error';

// Íconos del diseño para cada aviso (Main · Avisos). En Estados, una advertencia lleva el triángulo de
// alerta: cuando el contenido lo pide, el ícono se cambia con la propiedad `icono`.
const ICONO: Record<TonoAviso, NombreIcono> = {
  info: 'informacion',
  exito: 'confirmar',
  advertencia: 'reloj',
  error: 'alerta',
};

interface PropsBase {
  /** Título en negrita (Main). Sin título, el aviso es un solo mensaje (Estados). */
  titulo?: string;
  children?: ReactNode;
  /**
   * El aviso aparece como respuesta a una acción ("Cita confirmada", "No se pudo guardar"):
   * se anuncia al lector de pantalla (status, o alert si es error o advertencia) y entra con una
   * transición breve. Los avisos fijos de una pantalla no se anuncian.
   */
  anunciar?: boolean;
  icono?: NombreIcono;
  className?: string;
}

/**
 * Solo los avisos informativos y de éxito se pueden cerrar (Estados): los errores y las advertencias
 * se quedan hasta resolverse, por eso `alCerrar` no existe para esos tonos.
 */
export type AvisoProps = PropsBase &
  ({ tono: 'info' | 'exito'; alCerrar?: () => void } | { tono: 'advertencia' | 'error'; alCerrar?: never });

export function Aviso({ tono, titulo, children, anunciar = false, alCerrar, icono, className }: AvisoProps) {
  const rol = anunciar ? (tono === 'error' || tono === 'advertencia' ? 'alert' : 'status') : undefined;
  const cerrable = alCerrar && (tono === 'info' || tono === 'exito');
  return (
    <div role={rol} className={clases(estilos.aviso, estilos[tono], anunciar && estilos.anunciado, className)}>
      <span className={estilos.icono}>
        <Icono nombre={icono ?? ICONO[tono]} tamano={18} trazo={2} />
      </span>
      <div className={estilos.cuerpo}>
        {titulo && <div className={estilos.titulo}>{titulo}</div>}
        {children && <div className={estilos.texto}>{children}</div>}
      </div>
      {cerrable && (
        <button type="button" className={estilos.cerrar} aria-label="Cerrar aviso" onClick={alCerrar}>
          <Icono nombre="cerrar" tamano={18} trazo={2} />
        </button>
      )}
    </div>
  );
}
