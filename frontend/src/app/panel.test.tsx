import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { Rutas } from './Rutas';
import { ProveedorSesion } from './sesion';

// Datos de ejemplo, ninguno real
const servicio = vi.hoisted(() => ({
  usuario: null as null | { id: number; nombre: string; apellido: string; email: string; rol: string },
  login: vi.fn(),
  logout: vi.fn(async () => undefined),
}));

vi.mock('../services/authService', () => ({
  MENSAJE_SESION_EXPIRADA: 'Tu sesión expiró. Inicia sesión de nuevo.',
  MENSAJE_SIN_PERMISO: 'No tienes permiso para realizar esta acción.',
  getUsuario: () => servicio.usuario,
  clearSession: vi.fn(),
  login: servicio.login,
  logout: servicio.logout,
  authFetch: vi.fn(),
  esErrorDeAcceso: () => false,
}));

const ejemplo = (rol: string) => ({ id: 1, nombre: 'Ana', apellido: 'Ejemplo', email: 'ana@ejemplo.test', rol });

function montar(ruta: string) {
  const qc = new QueryClient();
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[ruta]}>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  servicio.usuario = null;
  servicio.login.mockReset();
  servicio.logout.mockClear();
});

describe('inicio de sesión', () => {
  it('sin sesión, cualquier ruta lleva al inicio de sesión', async () => {
    montar('/agenda');
    expect(await screen.findByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeInTheDocument();
    expect(screen.queryByText('¿Olvidaste tu contraseña?')).not.toBeInTheDocument();
  });

  it('valida en el navegador antes de llamar a la API', async () => {
    montar('/iniciar-sesion');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect(screen.getByLabelText('Correo electrónico')).toHaveAccessibleDescription('Escribe un correo válido.');
    expect(screen.getByLabelText('Correo electrónico')).toHaveFocus();
    expect(servicio.login).not.toHaveBeenCalled();
  });

  it('una cuenta de paciente no entra y su sesión se cierra', async () => {
    servicio.login.mockResolvedValue(ejemplo('paciente'));
    montar('/iniciar-sesion');
    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'ana@ejemplo.test');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'clave-de-prueba');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Acceso solo para el personal de la clínica.');
    expect(servicio.logout).toHaveBeenCalledTimes(1);
    expect(screen.getByLabelText('Contraseña')).toHaveValue('');
  });

  it('credenciales incorrectas: mensaje del servicio, sin revelar si la cuenta existe', async () => {
    servicio.login.mockRejectedValue(new Error('Credenciales incorrectas'));
    montar('/iniciar-sesion');
    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'ana@ejemplo.test');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'x');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Credenciales incorrectas');
  });

  it('el personal entra a la ruta que pidió', async () => {
    servicio.login.mockResolvedValue(ejemplo('asistente'));
    montar('/pagos');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana@ejemplo.test');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'clave-de-prueba');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect(await screen.findByRole('heading', { level: 1, name: 'Pagos' })).toBeInTheDocument();
  });

  it('sin violaciones de accesibilidad', async () => {
    const { container } = montar('/iniciar-sesion');
    await sinViolacionesAxe(container);
  });
});

describe('panel por rol', () => {
  it('asistente: menú según la matriz y "Sin permiso" fuera de ella', async () => {
    servicio.usuario = ejemplo('asistente');
    montar('/equipo');
    expect(await screen.findByRole('heading', { level: 1, name: 'Sin permiso' })).toBeInTheDocument();
    const menu = screen.getAllByRole('navigation')[0]!;
    expect(within(menu).queryByRole('link', { name: /Equipo y usuarios/ })).not.toBeInTheDocument();
    expect(within(menu).getByRole('link', { name: /Pagos/ })).toBeInTheDocument();
  });

  it('dentista: solo Inicio', async () => {
    servicio.usuario = ejemplo('dentista');
    montar('/agenda');
    expect(await screen.findByRole('heading', { level: 1, name: 'Sin permiso' })).toBeInTheDocument();
  });

  it('Servicios y Ofertas son rutas separadas, "Próximamente"', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/ofertas');
    expect(await screen.findByRole('heading', { level: 1, name: 'Ofertas y contenido' })).toBeInTheDocument();
    expect(screen.getByText('Próximamente')).toBeInTheDocument();
  });

  it('ruta inexistente: página no encontrada', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/no-existe');
    expect(await screen.findByRole('heading', { level: 1, name: 'No encontramos esta página' })).toBeInTheDocument();
  });

  it('cerrar sesión revoca el token en el servidor y vuelve al inicio de sesión', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/');
    await userEvent.click(screen.getAllByRole('button', { name: /Cerrar sesión/ })[0]!);
    expect(servicio.logout).toHaveBeenCalledTimes(1);
    expect(await screen.findByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeInTheDocument();
  });

  it('sesión expirada: vuelve al inicio de sesión con aviso', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/');
    window.dispatchEvent(new Event('session-expired'));
    expect(await screen.findByText('Tu sesión expiró. Inicia sesión de nuevo.')).toBeInTheDocument();
  });
});
