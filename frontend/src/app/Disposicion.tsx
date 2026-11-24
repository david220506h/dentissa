import { Icono, MenuInferior, MenuLateral, SinConexion, type ComponenteEnlace, type ElementoMenu, type GrupoMenu } from '@dentissa/ui';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { AvisoInactividad } from './AvisoInactividad';
import { useConexion } from './conexion';
import { NOMBRE_ROL, SECCIONES, seccionesDe, type IdSeccion, type Seccion } from './permisos';
import { useSesion } from './sesion';
import estilos from './Disposicion.module.css';

/** Enlace del enrutador para los componentes de @dentissa/ui (que no dependen de React Router). */
export const EnlaceRouter: ComponenteEnlace = ({ href, children, ...resto }) => (
  <Link to={href} {...resto}>
    {children}
  </Link>
);

// Menú inferior del celular (Movil.dc.html): cuatro secciones principales y "Más"
const INFERIOR: IdSeccion[] = ['inicio', 'agenda', 'solicitudes', 'pacientes'];

const aElemento = (s: Seccion): ElementoMenu => ({ id: s.id, etiqueta: s.etiqueta, icono: s.icono, href: s.ruta });

export function agrupar(secciones: Seccion[]): GrupoMenu[] {
  return (['Principal', 'Clínica', 'Administración'] as const).map((titulo) => ({
    titulo,
    elementos: secciones.filter((s) => s.grupo === titulo).map(aElemento),
  }));
}

/** Sección activa según la ruta ("/" solo es Inicio si la ruta es exactamente "/"). */
export function seccionActiva(ruta: string): string | undefined {
  if (ruta === '/mas') return 'mas';
  return SECCIONES.filter((s) => (s.ruta === '/' ? ruta === '/' : ruta === s.ruta || ruta.startsWith(`${s.ruta}/`)))[0]?.id;
}

export function Disposicion() {
  const { usuario, cerrar } = useSesion();
  const { enLinea, reintentar } = useConexion();
  const ubicacion = useLocation();
  const navegar = useNavigate();

  if (!usuario) return null;
  // Sin conexión: la pantalla reemplaza al panel; no se muestra ningún dato guardado
  if (!enLinea) return <SinConexion alReintentar={reintentar} />;

  const permitidas = seccionesDe(usuario.rol);
  const activa = seccionActiva(ubicacion.pathname);
  const nombre = `${usuario.nombre} ${usuario.apellido}`.trim();
  const alCerrarSesion = async () => {
    await cerrar();
    navegar('/iniciar-sesion', { replace: true });
  };

  const inferior: ElementoMenu[] = [
    ...permitidas.filter((s) => INFERIOR.includes(s.id)).map(aElemento),
    { id: 'mas', etiqueta: 'Más', icono: 'menu', href: '/mas' },
  ];

  return (
    <div className={estilos.raiz}>
      <a href="#contenido" className={estilos.saltar}>
        Saltar al contenido
      </a>
      <div className={estilos.lateral}>
        <MenuLateral
          grupos={agrupar(permitidas)}
          usuario={{ nombre, rol: NOMBRE_ROL[usuario.rol] }}
          alCerrarSesion={alCerrarSesion}
          enlace={EnlaceRouter}
          {...(activa ? { activo: activa } : {})}
        />
      </div>
      <div className={estilos.columna}>
        <header className={estilos.encabezadoMovil}>
          <span className={estilos.marca}>
            <span className={estilos.marcaIcono}>
              <Icono nombre="diente" />
            </span>
            Dentissa
          </span>
        </header>
        <main id="contenido" className={estilos.principal} tabIndex={-1}>
          <Outlet />
        </main>
        <div className={estilos.inferior}>
          <MenuInferior elementos={inferior} enlace={EnlaceRouter} {...(activa ? { activo: activa } : {})} />
        </div>
      </div>
      <AvisoInactividad />
    </div>
  );
}
