import {
  HojaMas,
  Icono,
  MenuInferior,
  MenuLateral,
  SinConexion,
  type ComponenteEnlace,
  type ElementoMenu,
  type GrupoMenu,
} from '@dentissa/ui';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { obtenerJson } from './api';
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

// Menú inferior del celular (Movil.dc.html): cuatro secciones principales; el resto va en la hoja "Más" (Movil2)
const INFERIOR: IdSeccion[] = ['inicio', 'agenda', 'solicitudes', 'pacientes'];

type Contadores = Partial<Record<IdSeccion, number>>;

const aElemento = (s: Seccion, contadores: Contadores = {}): ElementoMenu => {
  const n = contadores[s.id];
  return {
    id: s.id,
    etiqueta: s.etiqueta,
    icono: s.icono,
    href: s.ruta,
    ...(n ? { contador: { valor: n, descripcion: n === 1 ? '1 solicitud por revisar' : `${n} solicitudes por revisar` } } : {}),
  };
};

export function agrupar(secciones: Seccion[], contadores: Contadores = {}): GrupoMenu[] {
  return (['Principal', 'Clínica', 'Administración'] as const).map((titulo) => ({
    titulo,
    elementos: secciones.filter((s) => s.grupo === titulo).map((s) => aElemento(s, contadores)),
  }));
}

/** Sección activa según la ruta ("/" solo es Inicio si la ruta es exactamente "/"). */
export function seccionActiva(ruta: string): string | undefined {
  return SECCIONES.filter((s) => (s.ruta === '/' ? ruta === '/' : ruta === s.ruta || ruta.startsWith(`${s.ruta}/`)))[0]?.id;
}

export function Disposicion() {
  const { usuario, cerrar } = useSesion();
  const { enLinea, reintentar } = useConexion();
  const ubicacion = useLocation();
  const navegar = useNavigate();
  const [masAbierta, setMasAbierta] = useState(false);
  // Contador de Solicitudes: pendientes del sitio por revisar (solo recepción); se actualiza cada minuto
  const veSolicitudes = !!usuario && seccionesDe(usuario.rol).some((s) => s.id === 'solicitudes');
  const solicitudes = useQuery({
    queryKey: ['solicitudes'],
    queryFn: () => obtenerJson<unknown[]>('/solicitudes'),
    enabled: veSolicitudes,
    refetchInterval: 60_000,
  });
  const contadores: Contadores = { solicitudes: solicitudes.data?.length ?? 0 };

  // La hoja solo existe con el menú inferior: si la ventana pasa a 1024 px o más, se cierra
  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return; // entornos sin matchMedia (pruebas)
    const ancho = window.matchMedia('(min-width: 1024px)');
    const alCambiar = (e: MediaQueryListEvent) => e.matches && setMasAbierta(false);
    ancho.addEventListener('change', alCambiar);
    return () => ancho.removeEventListener('change', alCambiar);
  }, []);

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

  const inferior = permitidas.filter((s) => INFERIOR.includes(s.id)).map((s) => aElemento(s, contadores));
  // Hoja "Más": lo que el rol puede ver según la matriz de Main y no cabe en el menú inferior
  const enHoja: ElementoMenu[] = permitidas.filter((s) => !INFERIOR.includes(s.id)).map((s) => aElemento(s, contadores));

  return (
    <div className={estilos.raiz}>
      <a href="#contenido" className={estilos.saltar}>
        Saltar al contenido
      </a>
      <div className={estilos.lateral}>
        <MenuLateral
          grupos={agrupar(permitidas, contadores)}
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
          {/* Con la hoja abierta, solo "Más" se marca como activo (Movil2) */}
          <MenuInferior
            elementos={inferior}
            enlace={EnlaceRouter}
            mas={{ abierta: masAbierta, alActivar: () => setMasAbierta((a) => !a) }}
            {...(activa && !masAbierta ? { activo: activa } : {})}
          />
          <HojaMas
            abierta={masAbierta}
            alCerrar={() => setMasAbierta(false)}
            usuario={{ nombre, rol: NOMBRE_ROL[usuario.rol] }}
            elementos={enHoja}
            alCerrarSesion={() => {
              setMasAbierta(false);
              void alCerrarSesion();
            }}
            enlace={EnlaceRouter}
          />
        </div>
      </div>
      <AvisoInactividad />
    </div>
  );
}
