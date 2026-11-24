import type { ReactNode } from 'react';
import { Tab, TabList, TabPanel, Tabs } from 'react-aria-components';
import { Boton } from '../Boton/Boton';
import { Icono } from '../Icono/Icono';
import { SoloLector } from '../utilidades/SoloLector';
import estilos from './Pagina.module.css';

export interface EncabezadoPaginaProps {
  titulo: string;
  subtitulo?: string;
  /** Botones a la derecha; solo los que tengan backend (decisión 5). */
  acciones?: ReactNode;
}

/** Encabezado de cada pantalla: el único h1 de la página. */
export function EncabezadoPagina({ titulo, subtitulo, acciones }: EncabezadoPaginaProps) {
  return (
    <header className={estilos.encabezado}>
      <div>
        <h1 className={estilos.titulo}>{titulo}</h1>
        {subtitulo && <p className={estilos.subtitulo}>{subtitulo}</p>}
      </div>
      {acciones && <div className={estilos.acciones}>{acciones}</div>}
    </header>
  );
}

export interface TablaProps {
  /** Título accesible de la tabla (caption para lectores de pantalla). */
  etiqueta: string;
  /** Encabezados de columna; una cadena vacía para la columna de acciones lleva texto oculto. */
  columnas: string[];
  /** Ancho mínimo antes de desplazarse en horizontal (el diseño usa 640–860 px). */
  anchoMinimo?: number;
  /** Filas (<tr>…</tr>). */
  children: ReactNode;
  /** Pie de la tarjeta (p. ej., la paginación). */
  pie?: ReactNode;
}

/** Tabla dentro de una tarjeta, con desplazamiento horizontal propio en pantallas angostas. */
export function Tabla({ etiqueta, columnas, anchoMinimo = 640, children, pie }: TablaProps) {
  return (
    <div className={estilos.tablaTarjeta}>
      {/* Región desplazable con foco para que se pueda recorrer con el teclado */}
      <div className={estilos.tablaDesplazable} role="region" aria-label={etiqueta} tabIndex={0}>
        <table className={estilos.tabla} style={{ minWidth: anchoMinimo }}>
          <caption>
            <SoloLector>{etiqueta}</SoloLector>
          </caption>
          <thead>
            <tr>
              {columnas.map((c, i) => (
                <th key={`${c}-${i}`} scope="col">
                  {c || <SoloLector>Acciones</SoloLector>}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>{children}</tbody>
        </table>
      </div>
      {pie}
    </div>
  );
}

export interface PaginacionProps {
  desde: number;
  hasta: number;
  total?: number;
  alAnterior: () => void;
  alSiguiente: () => void;
  hayAnterior: boolean;
  haySiguiente: boolean;
}

export function Paginacion({ desde, hasta, total, alAnterior, alSiguiente, hayAnterior, haySiguiente }: PaginacionProps) {
  return (
    <div className={estilos.paginacion}>
      <span aria-live="polite">
        Mostrando {desde} a {hasta}
        {total !== undefined ? ` de ${total}` : ''}
      </span>
      <span className={estilos.paginacionBotones}>
        <Boton icono="anterior" disabled={!hayAnterior} onClick={alAnterior}>
          Anterior
        </Boton>
        <Boton icono="siguiente" disabled={!haySiguiente} onClick={alSiguiente}>
          Siguiente
        </Boton>
      </span>
    </div>
  );
}

export interface PestanaDef {
  id: string;
  etiqueta: string;
  contador?: number;
  /** Qué cuenta el contador, para lectores de pantalla (p. ej., "solicitudes"). */
  descripcionContador?: string;
  contenido: ReactNode;
}

export interface PestanasProps {
  etiqueta: string;
  pestanas: PestanaDef[];
  seleccionada?: string;
  alCambiar?: (id: string) => void;
}

/** Pestañas (Solicitudes): teclado con flechas, Inicio y Fin (React Aria). Cambiar no se anima. */
export function Pestanas({ etiqueta, pestanas, seleccionada, alCambiar }: PestanasProps) {
  return (
    <Tabs
      {...(seleccionada !== undefined ? { selectedKey: seleccionada } : {})}
      {...(alCambiar ? { onSelectionChange: (k) => alCambiar(String(k)) } : {})}
    >
      <TabList aria-label={etiqueta} className={estilos.pestanas}>
        {pestanas.map((p) => (
          <Tab key={p.id} id={p.id} className={estilos.pestana}>
            {p.etiqueta}
            {p.contador !== undefined && (
              <span className={estilos.contadorPestana}>
                <span aria-hidden="true">{p.contador}</span>
                <SoloLector>{`, ${p.contador} ${p.descripcionContador ?? 'elementos'}`}</SoloLector>
              </span>
            )}
          </Tab>
        ))}
      </TabList>
      {pestanas.map((p) => (
        <TabPanel key={p.id} id={p.id}>
          {p.contenido}
        </TabPanel>
      ))}
    </Tabs>
  );
}

export interface GrupoVistaProps<T extends string> {
  etiqueta: string;
  opciones: Array<{ valor: T; texto: string }>;
  valor: T;
  alCambiar: (valor: T) => void;
}

/** Botones de alternancia (Agenda: Día / Semana), con aria-pressed. */
export function GrupoVista<T extends string>({ etiqueta, opciones, valor, alCambiar }: GrupoVistaProps<T>) {
  return (
    <div role="group" aria-label={etiqueta} className={estilos.grupoVista}>
      {opciones.map((o) => (
        <button
          key={o.valor}
          type="button"
          className={estilos.opcionVista}
          aria-pressed={o.valor === valor}
          onClick={() => alCambiar(o.valor)}
        >
          {o.texto}
        </button>
      ))}
    </div>
  );
}

export interface SinConexionProps {
  alReintentar: () => void;
}

/** "Sin conexión con la clínica" (Movil): reemplaza al panel; no muestra datos guardados. */
export function SinConexion({ alReintentar }: SinConexionProps) {
  return (
    <main className={estilos.sinConexion}>
      <div className={estilos.sinConexionContenido}>
        <span className={estilos.sinConexionIcono}>
          <Icono nombre="sinConexion" tamano={34} />
        </span>
        <h1 className={estilos.sinConexionTitulo}>Sin conexión con la clínica</h1>
        <p className={estilos.sinConexionTexto}>
          Dentissa no guarda datos de pacientes en este teléfono, así que no puedes ver ni cambiar citas hasta recuperar
          la conexión.
        </p>
        <Boton variante="principal" icono="reintentar" anchoCompleto className={estilos.reintentar} onClick={alReintentar}>
          Reintentar
        </Boton>
        <p className={estilos.sinConexionNota}>Revisa que estés en el wifi de la clínica o que tengas datos móviles.</p>
      </div>
    </main>
  );
}
