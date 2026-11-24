import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { SECCIONES, puedeVer, type IdSeccion } from './permisos';
import { useSesion } from './sesion';
import { SinPermiso } from '../features/estados/Estados';

/** Sin sesión → inicio de sesión. Con sesión pero sin permiso para la sección → "Sin permiso". */
export function RutaProtegida({ seccion, children }: { seccion?: IdSeccion; children: ReactNode }) {
  const { usuario } = useSesion();
  const ubicacion = useLocation();
  if (!usuario) return <Navigate to="/iniciar-sesion" replace state={{ desde: ubicacion.pathname }} />;
  if (seccion && !puedeVer(usuario.rol, seccion)) {
    const nombre = SECCIONES.find((s) => s.id === seccion)?.etiqueta;
    return <SinPermiso {...(nombre ? { seccion: nombre } : {})} />;
  }
  return <>{children}</>;
}
