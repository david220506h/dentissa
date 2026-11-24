import { useCallback, useEffect, useRef, useState } from 'react';

// Cierre por inactividad (plan 14.4.3; Estados.dc.html). Fijo en el frontend durante la Fase 3a:
// la administración lo podrá configurar, y el servidor lo aplicará también, en la fase de Configuración.
export const MINUTOS_INACTIVIDAD = 15;
export const SEGUNDOS_AVISO = 60;
const LIMITE_MS = MINUTOS_INACTIVIDAD * 60_000;
const AVISO_MS = LIMITE_MS - SEGUNDOS_AVISO * 1000;

// Momento de la última actividad, compartido entre pestañas del mismo navegador (no es dato de paciente):
// trabajar en una pestaña mantiene viva la sesión en las demás, y todas cierran a la vez.
export const CLAVE_ACTIVIDAD = 'dentissa:ultima-actividad';
const EVENTOS = ['pointerdown', 'keydown', 'wheel', 'touchstart', 'scroll'] as const;
// Mover el puntero cuenta, pero no más de una vez cada 5 s
const INTERVALO_MOVIMIENTO_MS = 5000;

function leerUltima(): number | null {
  try {
    const v = Number(localStorage.getItem(CLAVE_ACTIVIDAD));
    return Number.isFinite(v) && v > 0 ? v : null;
  } catch {
    return null;
  }
}

function guardarUltima(t: number) {
  try {
    localStorage.setItem(CLAVE_ACTIVIDAD, String(t));
  } catch {
    // Sin almacenamiento (modo privado): cada pestaña cuenta por su lado
  }
}

export interface EstadoInactividad {
  /** Mostrar "¿Sigues ahí?" */
  avisando: boolean;
  segundosRestantes: number;
  mantener: () => void;
}

/**
 * Cuenta la inactividad mientras `activo`. A los 14 min muestra el aviso; a los 15 llama a `alVencer`.
 * Con el aviso abierto, la actividad general ya no cuenta: solo "Mantener sesión" lo cierra.
 */
export function useInactividad(activo: boolean, alVencer: () => void): EstadoInactividad {
  // La referencia sirve a los manejadores de eventos; el estado, al render
  const ultima = useRef(0);
  const [reloj, setReloj] = useState(() => ({ ultima: Date.now(), ahora: Date.now() }));
  const vencido = useRef(false);
  const alVencerRef = useRef(alVencer);
  useEffect(() => {
    alVencerRef.current = alVencer;
  });

  const registrar = useCallback((t = Date.now()) => {
    ultima.current = t;
    guardarUltima(t);
    setReloj({ ultima: t, ahora: t });
  }, []);

  useEffect(() => {
    if (!activo) return;
    vencido.current = false;
    // El estado inicial ya trae la hora actual; solo se comparte con las otras pestañas
    ultima.current = Date.now();
    guardarUltima(ultima.current);
    let ultimoMovimiento = 0;

    const alActuar = () => {
      if (Date.now() - ultima.current >= AVISO_MS) return; // aviso abierto: solo cuenta "Mantener sesión"
      registrar();
    };
    const alMover = () => {
      const t = Date.now();
      if (t - ultimoMovimiento < INTERVALO_MOVIMIENTO_MS) return;
      ultimoMovimiento = t;
      alActuar();
    };
    const alCambiarOtraPestana = (e: StorageEvent) => {
      if (e.key !== CLAVE_ACTIVIDAD) return;
      const t = leerUltima();
      if (t && t > ultima.current) {
        ultima.current = t;
        setReloj({ ultima: t, ahora: Date.now() });
      }
    };
    const revisar = () => {
      const t = leerUltima();
      if (t && t > ultima.current) ultima.current = t;
      const n = Date.now();
      setReloj({ ultima: ultima.current, ahora: n });
      if (!vencido.current && n - ultima.current >= LIMITE_MS) {
        vencido.current = true;
        alVencerRef.current();
      }
    };

    for (const ev of EVENTOS) window.addEventListener(ev, alActuar, { passive: true, capture: true });
    window.addEventListener('pointermove', alMover, { passive: true });
    window.addEventListener('storage', alCambiarOtraPestana);
    const reloj = window.setInterval(revisar, 1000);
    return () => {
      for (const ev of EVENTOS) window.removeEventListener(ev, alActuar, { capture: true });
      window.removeEventListener('pointermove', alMover);
      window.removeEventListener('storage', alCambiarOtraPestana);
      window.clearInterval(reloj);
    };
  }, [activo, registrar]);

  const transcurrido = reloj.ahora - reloj.ultima;
  const avisando = activo && transcurrido >= AVISO_MS && transcurrido < LIMITE_MS;
  const segundosRestantes = Math.max(0, Math.ceil((LIMITE_MS - transcurrido) / 1000));
  return { avisando, segundosRestantes, mantener: () => registrar() };
}
