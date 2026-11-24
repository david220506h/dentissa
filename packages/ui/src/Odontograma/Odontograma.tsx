import { Icono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import { SoloLector } from '../utilidades/SoloLector';
import estilos from './Odontograma.module.css';

/** Estados de una pieza dental (Expediente.dc.html). */
export type EstadoPieza = 'sano' | 'caries' | 'restaurado' | 'endodoncia' | 'corona' | 'ausente';

export const ESTADOS_PIEZA: Record<EstadoPieza, { codigo: string; texto: string }> = {
  sano: { codigo: '–', texto: 'Sano' },
  caries: { codigo: 'C', texto: 'Caries' },
  restaurado: { codigo: 'R', texto: 'Restaurado' },
  endodoncia: { codigo: 'E', texto: 'Endodoncia' },
  corona: { codigo: 'Co', texto: 'Corona' },
  ausente: { codigo: 'Au', texto: 'Ausente' },
};

// Numeración internacional (FDI), vista de frente: la derecha de la persona queda a la izquierda.
const SUPERIOR_DERECHA = [18, 17, 16, 15, 14, 13, 12, 11];
const SUPERIOR_IZQUIERDA = [21, 22, 23, 24, 25, 26, 27, 28];
const INFERIOR_DERECHA = [48, 47, 46, 45, 44, 43, 42, 41];
const INFERIOR_IZQUIERDA = [31, 32, 33, 34, 35, 36, 37, 38];

export interface OdontogramaProps {
  /** Estado de cada pieza por su número; las que no aparecen se muestran como sanas. */
  piezas: Partial<Record<number, EstadoPieza>>;
  /** Pieza seleccionada (aria-pressed y anillo del diseño). */
  seleccionada?: number;
  /** Si no se pasa, el odontograma es de solo lectura (sin botones). */
  alSeleccionar?: (pieza: number) => void;
}

/**
 * Odontograma presentacional: recibe los datos, no los pide. En esta fase solo vive en el catálogo;
 * el expediente clínico llegará con su backend.
 */
export function Odontograma({ piezas, seleccionada, alSeleccionar }: OdontogramaProps) {
  const pieza = (numero: number) => {
    const estado = piezas[numero] ?? 'sano';
    const { codigo, texto } = ESTADOS_PIEZA[estado];
    const contenido = (
      <>
        {/* Nombre: "Diente 36, caries". Incluye el número visible (WCAG 2.5.3); el código va oculto porque el estado ya está en el nombre */}
        <span className={estilos.numero}>
          {/* El espacio va fuera del texto oculto: al inicio de la línea se colapsa y no se ve */}
          <SoloLector>Diente</SoloLector> {numero}
          <SoloLector>{`, ${texto.toLocaleLowerCase('es-MX')}`}</SoloLector>
        </span>
        <Icono nombre="diente" tamano={22} trazo={1.6} />
        <span className={estilos.codigoPieza} aria-hidden="true">
          {estado === 'sano' ? ' ' : codigo}
        </span>
      </>
    );
    return alSeleccionar ? (
      <button
        key={numero}
        type="button"
        className={clases(estilos.pieza, estilos[estado])}
        aria-pressed={seleccionada === numero}
        onClick={() => alSeleccionar(numero)}
      >
        {contenido}
      </button>
    ) : (
      <div key={numero} className={clases(estilos.pieza, estilos[estado])}>
        {contenido}
      </div>
    );
  };

  const lados = (
    <div className={estilos.lados} aria-hidden="true">
      <span>Derecha de la persona</span>
      <span>Izquierda de la persona</span>
    </div>
  );

  return (
    <div>
      <div className={estilos.cabecera}>
        <p className={estilos.subtitulo}>Numeración internacional. Cada diente lleva su código además del color.</p>
        <ul className={estilos.leyenda} aria-label="Códigos del odontograma">
          {(Object.keys(ESTADOS_PIEZA) as EstadoPieza[]).map((e) => (
            <li key={e} className={clases(estilos.leyendaElemento, estilos[e])}>
              <span className={estilos.codigo}>{ESTADOS_PIEZA[e].codigo}</span>
              {ESTADOS_PIEZA[e].texto}
            </li>
          ))}
        </ul>
      </div>
      <div className={estilos.desplazable} role="region" aria-label="Odontograma" tabIndex={0}>
        <div className={estilos.arcadas}>
          {lados}
          <div className={estilos.arcada}>
            <div className={estilos.cuadrante} role="group" aria-label="Superior, derecha de la persona">{SUPERIOR_DERECHA.map(pieza)}</div>
            <div className={estilos.separador} aria-hidden="true" />
            <div className={estilos.cuadrante} role="group" aria-label="Superior, izquierda de la persona">{SUPERIOR_IZQUIERDA.map(pieza)}</div>
          </div>
          <div className={estilos.arcada}>
            <div className={estilos.cuadrante} role="group" aria-label="Inferior, derecha de la persona">{INFERIOR_DERECHA.map(pieza)}</div>
            <div className={estilos.separador} aria-hidden="true" />
            <div className={estilos.cuadrante} role="group" aria-label="Inferior, izquierda de la persona">{INFERIOR_IZQUIERDA.map(pieza)}</div>
          </div>
          {lados}
        </div>
      </div>
    </div>
  );
}
