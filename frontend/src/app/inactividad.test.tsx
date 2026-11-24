import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Rutas } from './Rutas';
import { ProveedorSesion } from './sesion';
import { CLAVE_ACTIVIDAD } from './inactividad';

// Datos de ejemplo; sin tokens ni API
const servicio = vi.hoisted(() => ({
  usuario: null as null | { id: number; nombre: string; apellido: string; email: string; rol: string },
  logout: vi.fn(async () => undefined),
}));

vi.mock('../services/authService', () => ({
  MENSAJE_SESION_EXPIRADA: 'Tu sesión expiró. Inicia sesión de nuevo.',
  MENSAJE_SIN_PERMISO: 'No tienes permiso para realizar esta acción.',
  getUsuario: () => servicio.usuario,
  clearSession: vi.fn(),
  login: vi.fn(),
  logout: servicio.logout,
  authFetch: vi.fn(),
  esErrorDeAcceso: () => false,
}));

const MINUTO = 60_000;

function montar() {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter initialEntries={['/']}>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const avanzar = async (ms: number) => {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
};

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false, toFake: ['setInterval', 'clearInterval', 'setTimeout', 'clearTimeout', 'Date'] });
  localStorage.clear();
  servicio.usuario = { id: 1, nombre: 'Ana', apellido: 'Ejemplo', email: 'ana@ejemplo.test', rol: 'admin' };
  servicio.logout.mockClear();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('cierre por inactividad (15 min, aviso a los 14)', () => {
  it('a los 14 min aparece "¿Sigues ahí?" con la cuenta regresiva', async () => {
    montar();
    await avanzar(14 * MINUTO - 1000);
    expect(screen.queryByRole('dialog')).toBeNull();
    await avanzar(1000);
    const dialogo = screen.getByRole('dialog', { name: '¿Sigues ahí?' });
    expect(dialogo).toHaveTextContent('Por seguridad, cerraremos tu sesión en 60 segundos.');
    await avanzar(5000);
    expect(dialogo).toHaveTextContent('55 segundos');
  });

  it('a los 15 min cierra la sesión, revoca el token y avisa en el inicio de sesión', async () => {
    montar();
    await avanzar(15 * MINUTO);
    expect(servicio.logout).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeInTheDocument();
    expect(screen.getByText('Tu sesión expiró. Inicia sesión de nuevo.')).toBeInTheDocument();
  });

  it('"Mantener sesión" reinicia la cuenta', async () => {
    montar();
    await avanzar(14 * MINUTO);
    fireEvent.click(screen.getByRole('button', { name: 'Mantener sesión' }));
    await avanzar(14 * MINUTO - 1000);
    expect(screen.queryByRole('dialog')).toBeNull();
    expect(servicio.logout).not.toHaveBeenCalled();
  });

  it('Esc y "Cerrar" también mantienen la sesión', async () => {
    montar();
    await avanzar(14 * MINUTO);
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
    await avanzar(500);
    expect(screen.queryByRole('dialog')).toBeNull();
    await avanzar(14 * MINUTO);
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar' }));
    await avanzar(500);
    expect(screen.queryByRole('dialog')).toBeNull();
    expect(servicio.logout).not.toHaveBeenCalled();
  });

  it('"Cerrar sesión" sale de inmediato', async () => {
    montar();
    await avanzar(14 * MINUTO);
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
    await avanzar(0);
    expect(servicio.logout).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeInTheDocument();
  });

  it('la actividad reinicia la cuenta; con el aviso abierto ya no cuenta', async () => {
    montar();
    await avanzar(10 * MINUTO);
    fireEvent.keyDown(window, { key: 'a' });
    await avanzar(10 * MINUTO);
    expect(screen.queryByRole('dialog')).toBeNull();
    await avanzar(4 * MINUTO);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    fireEvent.pointerDown(document.body);
    await avanzar(60_000);
    expect(servicio.logout).toHaveBeenCalledTimes(1);
  });

  it('la actividad en otra pestaña mantiene viva esta', async () => {
    montar();
    await avanzar(13 * MINUTO);
    localStorage.setItem(CLAVE_ACTIVIDAD, String(Date.now()));
    window.dispatchEvent(new StorageEvent('storage', { key: CLAVE_ACTIVIDAD }));
    await avanzar(13 * MINUTO);
    expect(screen.queryByRole('dialog')).toBeNull();
    expect(servicio.logout).not.toHaveBeenCalled();
  });
});
