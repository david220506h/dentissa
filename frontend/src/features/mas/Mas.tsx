import { Boton, EncabezadoPagina, GruposMenu } from '@dentissa/ui';
import { useNavigate } from 'react-router-dom';
import { EnlaceRouter, agrupar } from '../../app/Disposicion';
import { seccionesDe } from '../../app/permisos';
import { useSesion } from '../../app/sesion';
import estilos from './Mas.module.css';

// PROVISIONAL: la hoja "Más" por rol está en Movil2.dc.html (zip del arquitecto, pendiente).
// Mientras tanto, "Más" es una página con los mismos grupos del menú lateral, que sí están diseñados.

const EN_MENU_INFERIOR = new Set(['inicio', 'agenda', 'solicitudes', 'pacientes']);

export function Mas() {
  const { usuario, cerrar } = useSesion();
  const navegar = useNavigate();
  if (!usuario) return null;
  const resto = seccionesDe(usuario.rol).filter((s) => !EN_MENU_INFERIOR.has(s.id));
  return (
    <div className={estilos.pagina}>
      <EncabezadoPagina titulo="Más" />
      {resto.length > 0 && (
        <div className={estilos.tarjeta}>
          <GruposMenu grupos={agrupar(resto)} etiqueta="Más secciones" enlace={EnlaceRouter} />
        </div>
      )}
      <div>
        <Boton
          variante="secundario"
          icono="cerrarSesion"
          onClick={async () => {
            await cerrar();
            navegar('/iniciar-sesion', { replace: true });
          }}
        >
          Cerrar sesión
        </Boton>
      </div>
    </div>
  );
}
