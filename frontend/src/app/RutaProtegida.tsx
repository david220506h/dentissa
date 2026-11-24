import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { puedeVer, type IdSeccion } from './permisos';
import { useSesion } from './sesion';
import { SinPermiso } from '../features/estados/Estados';

/** Sin sesión → inicio de sesión. Con sesión pero sin permiso para la sección → "Sin permiso". */
export function RutaProtegida({ seccion, children }: { seccion?: IdSeccion; children: ReactNode }) {
  const { usuario } = useSesion();
  const ubicacion = useLocation();
  if (!usuario) return <Navigate to="/iniciar-sesion" replace state={{ desde: ubicacion.pathname }} />;
  if (seccion && !puedeVer(usuario.rol, seccion)) return <SinPermiso />;
  return <>{children}</>;
}
