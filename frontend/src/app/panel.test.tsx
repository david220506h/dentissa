import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
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
    expect(await screen.findByRole('heading', { level: 1, name: 'No tienes acceso a esta sección' })).toBeInTheDocument();
    expect(screen.getByText('Tu rol no incluye Equipo y usuarios. Si lo necesitas, pídeselo a la administración.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Volver al inicio' })).toBeInTheDocument();
    const menu = screen.getAllByRole('navigation')[0]!;
    expect(within(menu).queryByRole('link', { name: /Equipo y usuarios/ })).not.toBeInTheDocument();
    expect(within(menu).getByRole('link', { name: /Pagos/ })).toBeInTheDocument();
  });

  it('dentista: solo Inicio', async () => {
    servicio.usuario = ejemplo('dentista');
    montar('/agenda');
    expect(await screen.findByRole('heading', { level: 1, name: 'No tienes acceso a esta sección' })).toBeInTheDocument();
  });

  it('Servicios y Ofertas son rutas separadas, "Próximamente"', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/ofertas');
    expect(await screen.findByRole('heading', { level: 1, name: 'Ofertas y contenido' })).toBeInTheDocument();
    expect(screen.getByText(/^Próximamente./)).toBeInTheDocument();
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

describe('hoja "Más" por rol (Movil2; manda la matriz de Main)', () => {
  const abrirHoja = async () => {
    await userEvent.click(await screen.findByRole('button', { name: 'Más' }));
    return screen.findByRole('dialog', { name: 'Más opciones' });
  };
  const enlacesDe = (hoja: HTMLElement) => within(hoja).queryAllByRole('link').map((a) => a.textContent);

  it('administración: todo lo que no está en el menú inferior, incluidos Expediente y Ofertas', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/');
    const hoja = await abrirHoja();
    expect(enlacesDe(hoja)).toEqual([
      'Expediente clínico',
      'Servicios',
      'Ofertas y contenido',
      'Inventario',
      'Pagos',
      'Reportes',
      'Equipo y usuarios',
      'Configuración',
      'Auditoría',
    ]);
    expect(within(hoja).getByText('Ana Ejemplo')).toBeInTheDocument();
    expect(within(hoja).getByText('Administración')).toBeInTheDocument();
    // Con la hoja abierta el resto de la página queda inerte y fuera del árbol accesible
    expect(screen.getByRole('button', { name: 'Más', hidden: true })).toHaveAttribute('aria-expanded', 'true');
  });

  it('asistente: Inventario y Pagos (sin Servicios: Main no se lo da, aunque Movil2 lo dibuje)', async () => {
    servicio.usuario = ejemplo('asistente');
    montar('/');
    expect(enlacesDe(await abrirHoja())).toEqual(['Inventario', 'Pagos']);
  });

  it('dentista: solo perfil y "Cerrar sesión" por ahora', async () => {
    servicio.usuario = ejemplo('dentista');
    montar('/');
    const hoja = await abrirHoja();
    expect(enlacesDe(hoja)).toEqual([]);
    expect(within(hoja).getByRole('button', { name: 'Cerrar sesión' })).toBeInTheDocument();
  });

  it('elegir una sección navega y cierra la hoja', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/');
    const hoja = await abrirHoja();
    await userEvent.click(within(hoja).getByRole('link', { name: 'Pagos' }));
    expect(await screen.findByRole('heading', { level: 1, name: 'Pagos' })).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
  });

  it('Esc la cierra y el foco vuelve a "Más"', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/');
    await abrirHoja();
    await userEvent.keyboard('{Escape}');
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
    await waitFor(() => expect(screen.getByRole('button', { name: 'Más' })).toHaveFocus());
    expect(screen.getByRole('button', { name: 'Más' })).toHaveAttribute('aria-expanded', 'false');
  });

  it('"Cerrar sesión" desde la hoja revoca el token y vuelve al inicio de sesión', async () => {
    servicio.usuario = ejemplo('asistente');
    montar('/');
    const hoja = await abrirHoja();
    await userEvent.click(within(hoja).getByRole('button', { name: 'Cerrar sesión' }));
    expect(servicio.logout).toHaveBeenCalledTimes(1);
    expect(await screen.findByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeInTheDocument();
  });

  it('sin violaciones de accesibilidad con la hoja abierta', async () => {
    servicio.usuario = ejemplo('admin');
    montar('/');
    await abrirHoja();
    await sinViolacionesAxe(document.body);
  });
});
