import type { ReactNode } from 'react';
import { Icono, type NombreIcono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import estilos from './Aviso.module.css';

export type TonoAviso = 'info' | 'exito' | 'advertencia' | 'error';

// Íconos del diseño para cada aviso (Main · Avisos)
const ICONO: Record<TonoAviso, NombreIcono> = {
  info: 'informacion',
  exito: 'confirmar',
  advertencia: 'reloj',
  error: 'alerta',
};

export interface AvisoProps {
  tono: TonoAviso;
  titulo: string;
  children?: ReactNode;
  /**
   * El aviso aparece como respuesta a una acción ("Cita confirmada", "No se pudo guardar"):
   * se anuncia al lector de pantalla (status, o alert si es error) y entra con una transición breve.
   * Los avisos fijos de una pantalla ("Esta lista solo muestra datos generales.") no se anuncian.
   */
  anunciar?: boolean;
  /** Si existe, muestra un botón "Cerrar aviso". */
  alCerrar?: () => void;
  className?: string;
}

export function Aviso({ tono, titulo, children, anunciar = false, alCerrar, className }: AvisoProps) {
  const rol = anunciar ? (tono === 'error' ? 'alert' : 'status') : undefined;
  return (
    <div role={rol} className={clases(estilos.aviso, estilos[tono], anunciar && estilos.anunciado, className)}>
      <span className={estilos.icono}>
        <Icono nombre={ICONO[tono]} tamano={18} trazo={2} />
      </span>
      <div className={estilos.cuerpo}>
        <div className={estilos.titulo}>{titulo}</div>
        {children && <div className={estilos.texto}>{children}</div>}
      </div>
      {alCerrar && (
        <button type="button" className={estilos.cerrar} aria-label="Cerrar aviso" onClick={alCerrar}>
          <Icono nombre="cerrar" tamano={18} />
        </button>
      )}
    </div>
  );
}
