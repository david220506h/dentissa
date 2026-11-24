import { useId, type ReactNode } from 'react';
import { Dialog, Heading, Modal, ModalOverlay } from 'react-aria-components';
import { Icono, type NombreIcono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import estilos from './Dialogo.module.css';

export interface DialogoProps {
  abierto: boolean;
  /** Se llama con Esc, con "Cerrar" y al tocar fuera (si `cerrarFuera`). */
  alCerrar: () => void;
  titulo: string;
  /** Ícono en círculo junto al título (Estados: reloj en "¿Sigues ahí?"). */
  icono?: NombreIcono;
  /** Texto principal; se enlaza como descripción del diálogo. */
  children: ReactNode;
  /** Botones de la parte inferior, alineados a la derecha. */
  acciones?: ReactNode;
  /** "chico" ≈ 420 px (Estados); "normal" 620 px (CitaFormularios). */
  tamano?: 'chico' | 'normal';
  cerrarFuera?: boolean;
  /** Etiqueta del botón de la esquina (Estados: "Cerrar"). */
  etiquetaCerrar?: string;
}

/**
 * Diálogo modal (Estados.dc.html y CitaFormularios.dc.html). React Aria atrapa el foco, cierra con Esc,
 * deja el resto de la página inerte y devuelve el foco al botón que lo abrió.
 * Al abrir, el foco va al diálogo (el lector anuncia título y descripción) y Tab recorre sus controles,
 * empezando por "Cerrar". Ninguna acción se activa sin que la persona la elija.
 */
export function Dialogo({
  abierto,
  alCerrar,
  titulo,
  icono,
  children,
  acciones,
  tamano = 'normal',
  cerrarFuera = false,
  etiquetaCerrar = 'Cerrar',
}: DialogoProps) {
  const idCuerpo = useId();
  return (
    <ModalOverlay
      isOpen={abierto}
      onOpenChange={(abrir) => !abrir && alCerrar()}
      isDismissable={cerrarFuera}
      className={estilos.velo}
    >
      <Modal className={clases(estilos.modal, estilos[tamano])}>
        <Dialog className={estilos.dialogo} aria-describedby={idCuerpo}>
          <div className={estilos.encabezado}>
            <div className={estilos.tituloGrupo}>
              {icono && (
                <span className={estilos.icono}>
                  <Icono nombre={icono} tamano={22} />
                </span>
              )}
              <Heading slot="title" className={estilos.titulo}>
                {titulo}
              </Heading>
            </div>
            <button type="button" className={estilos.cerrar} aria-label={etiquetaCerrar} onClick={alCerrar}>
              <Icono nombre="cerrar" tamano={20} />
            </button>
          </div>
          <div id={idCuerpo} className={estilos.cuerpo}>
            {children}
          </div>
          {acciones && <div className={estilos.acciones}>{acciones}</div>}
        </Dialog>
      </Modal>
    </ModalOverlay>
  );
}
