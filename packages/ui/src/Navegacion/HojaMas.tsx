import { Dialog, Modal, ModalOverlay } from 'react-aria-components';
import { Avatar } from '../Avatar/Avatar';
import { Icono } from '../Icono/Icono';
import type { ComponenteEnlace, ElementoMenu } from './Navegacion';
import estilos from './HojaMas.module.css';

export interface HojaMasProps {
  abierta: boolean;
  alCerrar: () => void;
  usuario: { nombre: string; rol: string };
  /** Lo que no cabe en el menú inferior, ya filtrado por el rol. */
  elementos: ElementoMenu[];
  alCerrarSesion: () => void;
  enlace: ComponenteEnlace;
}

/**
 * Hoja "Más" del celular (Movil2.dc.html): sube desde abajo sobre el menú inferior, con el perfil, las
 * secciones que el rol puede ver y "Cerrar sesión". React Aria atrapa el foco, cierra con Esc o al tocar
 * el fondo, y devuelve el foco al botón "Más". Al elegir una sección, la aplicación la cierra.
 */
export function HojaMas({ abierta, alCerrar, usuario, elementos, alCerrarSesion, enlace: Enlace }: HojaMasProps) {
  return (
    <ModalOverlay
      isOpen={abierta}
      onOpenChange={(abrir) => !abrir && alCerrar()}
      isDismissable
      className={estilos.velo}
    >
      <Modal className={estilos.modal}>
        <Dialog aria-label="Más opciones" className={estilos.hoja}>
          <span aria-hidden="true" className={estilos.asa} />
          <div className={estilos.perfil}>
            <Avatar nombre={usuario.nombre} />
            <div>
              <div className={estilos.nombre}>{usuario.nombre}</div>
              <div className={estilos.rol}>{usuario.rol}</div>
            </div>
          </div>
          {elementos.length > 0 && (
            <nav aria-label="Más secciones">
              <ul className={estilos.lista}>
                {elementos.map((e) => (
                  <li key={e.id}>
                    <Enlace href={e.href} className={estilos.elemento} onClick={alCerrar}>
                      <span className={estilos.icono}>
                        <Icono nombre={e.icono} tamano={22} />
                      </span>
                      {e.etiqueta}
                    </Enlace>
                  </li>
                ))}
              </ul>
            </nav>
          )}
          <button type="button" className={estilos.cerrarSesion} onClick={alCerrarSesion}>
            <Icono nombre="cerrarSesion" tamano={22} />
            Cerrar sesión
          </button>
        </Dialog>
      </Modal>
    </ModalOverlay>
  );
}
