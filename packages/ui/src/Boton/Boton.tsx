import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Icono, type NombreIcono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import estilos from './Boton.module.css';

export type VarianteBoton = 'principal' | 'secundario' | 'peligro' | 'texto';

export interface BotonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: VarianteBoton;
  /** Ícono a la izquierda del texto (de 18 px, como en el diseño). */
  icono?: NombreIcono;
  /** 48 px de alto (botón de Iniciar sesión). */
  grande?: boolean;
  anchoCompleto?: boolean;
  /** Deshabilita el botón y muestra el indicador si la espera pasa de 300 ms. */
  cargando?: boolean;
  /** Texto mientras carga (por defecto "Guardando…"). */
  textoCargando?: string;
  children: ReactNode;
}

export function Boton({
  variante = 'secundario',
  icono,
  grande = false,
  anchoCompleto = false,
  cargando = false,
  textoCargando = 'Guardando…',
  type = 'button',
  disabled,
  className,
  children,
  ...resto
}: BotonProps) {
  return (
    <button
      type={type}
      className={clases(
        estilos.boton,
        estilos[variante],
        grande && estilos.grande,
        anchoCompleto && estilos.anchoCompleto,
        className,
      )}
      disabled={disabled || cargando}
      aria-busy={cargando || undefined}
      {...resto}
    >
      {cargando ? (
        <>
          <span className={estilos.indicador} aria-hidden="true" />
          <span>{textoCargando}</span>
        </>
      ) : (
        <>
          {icono && <Icono nombre={icono} tamano={18} />}
          <span>{children}</span>
        </>
      )}
    </button>
  );
}
