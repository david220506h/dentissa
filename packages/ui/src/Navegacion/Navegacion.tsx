import type { ComponentType, ReactNode } from 'react';
import { Avatar } from '../Avatar/Avatar';
import { Contador } from '../Contador/Contador';
import { Icono, type NombreIcono } from '../Icono/Icono';
import { SoloLector } from '../utilidades/SoloLector';
import estilos from './Navegacion.module.css';

/**
 * Enlace que pone la aplicación (p. ej., el NavLink de React Router). Los componentes no dependen
 * del enrutador: reciben el componente de enlace por propiedades.
 */
export type ComponenteEnlace = ComponentType<{
  href: string;
  className?: string;
  'aria-current'?: 'page';
  children: ReactNode;
}>;

const EnlaceNativo: ComponenteEnlace = ({ href, children, ...resto }) => (
  <a href={href} {...resto}>
    {children}
  </a>
);

export interface ElementoMenu {
  id: string;
  etiqueta: string;
  icono: NombreIcono;
  href: string;
  /** Número a la derecha (p. ej., solicitudes por revisar). Solo con datos reales. */
  contador?: { valor: number; descripcion: string };
}

export interface GrupoMenu {
  titulo: string;
  elementos: ElementoMenu[];
}

interface PropsComunes {
  /** id del elemento de la sección actual (aria-current="page"). */
  activo?: string;
  enlace?: ComponenteEnlace;
}

export interface GruposMenuProps extends PropsComunes {
  grupos: GrupoMenu[];
  /** Nombre accesible de la navegación. */
  etiqueta: string;
}

/** Grupos de secciones (Principal, Clínica, Administración). Se usa en el menú lateral y en "Más". */
export function GruposMenu({ grupos, etiqueta, activo, enlace: Enlace = EnlaceNativo }: GruposMenuProps) {
  return (
    <nav aria-label={etiqueta} className={estilos.grupos}>
      {grupos
        .filter((g) => g.elementos.length > 0)
        .map((grupo) => (
          <div key={grupo.titulo}>
            <div className={estilos.grupoTitulo} aria-hidden="true">
              {grupo.titulo}
            </div>
            <ul className={estilos.grupo} aria-label={grupo.titulo}>
              {grupo.elementos.map((e) => (
                <li key={e.id}>
                  <Enlace
                    href={e.href}
                    className={estilos.elemento}
                    {...(e.id === activo ? { 'aria-current': 'page' as const } : {})}
                  >
                    <Icono nombre={e.icono} />
                    <span className={estilos.elementoTexto}>{e.etiqueta}</span>
                    {e.contador && <Contador valor={e.contador.valor} descripcion={e.contador.descripcion} />}
                  </Enlace>
                </li>
              ))}
            </ul>
          </div>
        ))}
    </nav>
  );
}

export interface MenuLateralProps extends PropsComunes {
  grupos: GrupoMenu[];
  usuario: { nombre: string; rol: string };
  alCerrarSesion: () => void;
}

/** Menú lateral del panel (Inicio.dc.html), desde 1024 px. */
export function MenuLateral({ grupos, usuario, alCerrarSesion, activo, enlace }: MenuLateralProps) {
  return (
    <aside className={estilos.lateral}>
      <div className={estilos.marca}>
        <span className={estilos.marcaIcono}>
          <Icono nombre="diente" tamano={24} />
        </span>
        <div>
          <div className={estilos.marcaNombre}>Dentissa</div>
          <div className={estilos.marcaSubtitulo}>Panel de la clínica</div>
        </div>
      </div>
      <GruposMenu grupos={grupos} etiqueta="Menú principal" {...(activo ? { activo } : {})} {...(enlace ? { enlace } : {})} />
      <div className={estilos.pie}>
        <Avatar nombre={usuario.nombre} />
        <div className={estilos.pieDatos}>
          <div className={estilos.pieNombre}>{usuario.nombre}</div>
          <div className={estilos.pieRol}>{usuario.rol}</div>
        </div>
        <button type="button" className={estilos.botonIcono} aria-label="Cerrar sesión" onClick={alCerrarSesion}>
          <Icono nombre="cerrarSesion" />
        </button>
      </div>
    </aside>
  );
}

export interface MenuInferiorProps extends PropsComunes {
  /** Hasta cinco entradas (Movil: Inicio, Agenda, Solicitudes, Pacientes, Más). */
  elementos: ElementoMenu[];
}

/** Menú inferior del celular (Movil.dc.html), por debajo de 1024 px. */
export function MenuInferior({ elementos, activo, enlace: Enlace = EnlaceNativo }: MenuInferiorProps) {
  return (
    <nav aria-label="Menú móvil" className={estilos.inferior}>
      {elementos.map((e) => (
        <Enlace
          key={e.id}
          href={e.href}
          className={estilos.inferiorElemento}
          {...(e.id === activo ? { 'aria-current': 'page' as const } : {})}
        >
          <span className={estilos.inferiorIcono}>
            <Icono nombre={e.icono} tamano={22} />
          </span>
          {e.etiqueta}
          {e.contador && (
            <span className={estilos.inferiorContador}>
              <span aria-hidden="true">{e.contador.valor}</span>
              <SoloLector>{e.contador.descripcion}</SoloLector>
            </span>
          )}
        </Enlace>
      ))}
    </nav>
  );
}
