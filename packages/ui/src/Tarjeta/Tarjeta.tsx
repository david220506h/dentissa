import { useId, type HTMLAttributes, type ReactNode } from 'react';
import { Icono, type NombreIcono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import estilos from './Tarjeta.module.css';

export interface TarjetaProps extends HTMLAttributes<HTMLElement> {
  /** Título de la tarjeta (18 px · 700). Se usa como nombre accesible de la sección. */
  titulo?: string;
  /** Nivel del título; por defecto h2 (bajo el h1 de la página). */
  nivelTitulo?: 2 | 3;
  /** Acción a la derecha del título (p. ej., "Ver agenda completa"). */
  accion?: ReactNode;
  /** Relleno de 24 px en lugar de 20 px (tarjetas con lista, como "Agenda de hoy"). */
  holgada?: boolean;
  children: ReactNode;
}

export function Tarjeta({ titulo, nivelTitulo = 2, accion, holgada = false, className, children, ...resto }: TarjetaProps) {
  const idTitulo = useId();
  const Titulo = nivelTitulo === 2 ? 'h2' : 'h3';
  return (
    <section
      className={clases(estilos.tarjeta, holgada && estilos.holgada, className)}
      aria-labelledby={titulo ? idTitulo : undefined}
      {...resto}
    >
      {(titulo || accion) && (
        <div className={estilos.encabezado}>
          {titulo && (
            <Titulo id={idTitulo} className={estilos.titulo}>
              {titulo}
            </Titulo>
          )}
          {accion}
        </div>
      )}
      {children}
    </section>
  );
}

export interface TarjetaIndicadorProps {
  etiqueta: string;
  /** Solo datos reales: si no hay dato, la tarjeta no se muestra (decisión 5 del plan). */
  valor: number;
  icono: NombreIcono;
  detalle?: string;
}

/** Indicador de Inicio ("Citas de hoy: 5 · de 6 horarios del día"). */
export function TarjetaIndicador({ etiqueta, valor, icono, detalle }: TarjetaIndicadorProps) {
  return (
    <section className={estilos.tarjeta} aria-label={etiqueta}>
      <div className={estilos.indicador}>
        <div>
          <div className={estilos.indicadorEtiqueta}>{etiqueta}</div>
          <div className={estilos.indicadorValor}>{valor}</div>
        </div>
        <span className={estilos.indicadorIcono}>
          <Icono nombre={icono} tamano={22} />
        </span>
      </div>
      {detalle && <div className={estilos.indicadorDetalle}>{detalle}</div>}
    </section>
  );
}
