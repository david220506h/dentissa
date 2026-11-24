import { Aviso, Boton, EncabezadoPagina } from '@dentissa/ui';
import { useNavigate } from 'react-router-dom';
import estilos from './Estados.module.css';

// PROVISIONAL: Estados.dc.html (zip del arquitecto, pendiente) define estas pantallas. Mientras llega,
// solo usan componentes ya aprobados (encabezado y aviso) y se sustituyen al recibir el diseño.

export function Proximamente({ titulo }: { titulo: string }) {
  return (
    <div className={estilos.pagina}>
      <EncabezadoPagina titulo={titulo} />
      <Aviso tono="info" titulo="Próximamente">
        Esta sección se habilitará cuando su servicio esté listo.
      </Aviso>
    </div>
  );
}

export function SinPermiso() {
  const navegar = useNavigate();
  return (
    <div className={estilos.pagina}>
      <EncabezadoPagina titulo="Sin permiso" />
      <Aviso tono="advertencia" titulo="No tienes permiso para ver esta sección.">
        Si crees que es un error, pide a la administración que revise tu rol.
      </Aviso>
      <div>
        <Boton variante="secundario" icono="inicio" onClick={() => navegar('/')}>
          Ir a Inicio
        </Boton>
      </div>
    </div>
  );
}

export function NoEncontrado() {
  const navegar = useNavigate();
  return (
    <div className={estilos.pagina}>
      <EncabezadoPagina titulo="No encontramos esta página" />
      <Aviso tono="info" titulo="La dirección no existe en el panel." />
      <div>
        <Boton variante="secundario" icono="inicio" onClick={() => navegar('/')}>
          Ir a Inicio
        </Boton>
      </div>
    </div>
  );
}
