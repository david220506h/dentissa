import { useQueryClient } from '@tanstack/react-query';
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { MENSAJE_SESION_EXPIRADA, clearSession, getUsuario, login, logout, type UsuarioSesion } from '../services/authService';
import { esRolPanel, type RolPanel } from './permisos';

export interface UsuarioPanel extends UsuarioSesion {
  rol: RolPanel;
}

/** La cuenta existe pero no es del personal (p. ej., un paciente): el panel no la deja entrar. */
export class CuentaSinAccesoAlPanel extends Error {
  constructor() {
    super('Acceso solo para el personal de la clínica.');
  }
}

interface ValorSesion {
  usuario: UsuarioPanel | null;
  /** Aviso para el inicio de sesión (p. ej., "Tu sesión expiró…"). */
  aviso: string | null;
  limpiarAviso: () => void;
  iniciar: (correo: string, contrasena: string) => Promise<UsuarioPanel>;
  cerrar: () => Promise<void>;
}

const ContextoSesion = createContext<ValorSesion | null>(null);

function usuarioGuardado(): UsuarioPanel | null {
  const u = getUsuario();
  return u && esRolPanel(u.rol) ? (u as UsuarioPanel) : null;
}

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [usuario, setUsuario] = useState<UsuarioPanel | null>(usuarioGuardado);
  const [aviso, setAviso] = useState<string | null>(null);

  // authService avisa cuando la sesión ya no se puede renovar
  useEffect(() => {
    const alExpirar = () => {
      queryClient.clear();
      setUsuario(null);
      setAviso(MENSAJE_SESION_EXPIRADA);
    };
    window.addEventListener('session-expired', alExpirar);
    return () => window.removeEventListener('session-expired', alExpirar);
  }, [queryClient]);

  const iniciar = useCallback(async (correo: string, contrasena: string) => {
    const u = await login(correo.trim(), contrasena);
    if (!esRolPanel(u.rol)) {
      // Cierra la sesión recién creada (revoca el token de renovación) y no entra al panel
      await logout();
      throw new CuentaSinAccesoAlPanel();
    }
    setAviso(null);
    const delPanel = u as UsuarioPanel;
    setUsuario(delPanel);
    return delPanel;
  }, []);

  const cerrar = useCallback(async () => {
    // logout() revoca el token de renovación en el servidor y limpia la sesión local
    await logout();
    clearSession();
    queryClient.clear();
    setUsuario(null);
  }, [queryClient]);

  const valor = useMemo(
    () => ({ usuario, aviso, limpiarAviso: () => setAviso(null), iniciar, cerrar }),
    [usuario, aviso, iniciar, cerrar],
  );
  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>;
}

export function useSesion(): ValorSesion {
  const v = useContext(ContextoSesion);
  if (!v) throw new Error('useSesion debe usarse dentro de ProveedorSesion');
  return v;
}
