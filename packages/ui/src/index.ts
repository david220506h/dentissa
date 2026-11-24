// Componentes de interfaz de Dentissa (docs/design/panel). Requieren @dentissa/tokens cargado.
export { Icono, NOMBRES_ICONOS, type IconoProps, type NombreIcono } from './Icono/Icono';
export { Boton, type BotonProps, type VarianteBoton } from './Boton/Boton';
export {
  Insignia,
  InsigniaEstadoCita,
  ESTADOS_CITA,
  type EstadoCita,
  type InsigniaProps,
  type TonoInsignia,
} from './Insignia/Insignia';
export { Contador, type ContadorProps } from './Contador/Contador';
export { Avatar, iniciales, type AvatarProps } from './Avatar/Avatar';
export { Tarjeta, TarjetaIndicador, type TarjetaProps, type TarjetaIndicadorProps } from './Tarjeta/Tarjeta';
export {
  CampoTexto,
  CampoContrasena,
  CampoSeleccion,
  CampoAreaTexto,
  Busqueda,
  type CampoTextoProps,
  type CampoContrasenaProps,
  type CampoSeleccionProps,
  type CampoAreaTextoProps,
  type BusquedaProps,
} from './Campo/Campo';
export { Aviso, type AvisoProps, type TonoAviso } from './Aviso/Aviso';
export {
  GruposMenu,
  MenuLateral,
  MenuInferior,
  type ComponenteEnlace,
  type ElementoMenu,
  type GrupoMenu,
  type GruposMenuProps,
  type MenuLateralProps,
  type MenuInferiorProps,
} from './Navegacion/Navegacion';
export {
  EncabezadoPagina,
  Tabla,
  Paginacion,
  Pestanas,
  GrupoVista,
  SinConexion,
  type EncabezadoPaginaProps,
  type TablaProps,
  type PaginacionProps,
  type PestanaDef,
  type PestanasProps,
  type GrupoVistaProps,
  type SinConexionProps,
} from './Pagina/Pagina';
export { SoloLector } from './utilidades/SoloLector';
export { clases } from './utilidades/clases';
