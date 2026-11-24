import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { Rutas } from '../../app/Rutas';
import { ProveedorSesion } from '../../app/sesion';
import { ultimoAcceso, type UsuarioApi } from './Equipo';

// Datos de ejemplo del diseño (Equipo.dc.html), con correos .test; no son reales
const USUARIOS: UsuarioApi[] = [
  { id: 1, nombre: 'Laura', apellido: 'Mendoza', email: 'laura@ejemplo.test', rol: 'admin', activo: true, ultimoAccesoEn: '2026-11-18T14:02:00Z' },
  { id: 2, nombre: 'Mariana', apellido: 'Soto', email: 'mariana@ejemplo.test', rol: 'dentista', activo: true },
  { id: 3, nombre: 'Karla', apellido: 'Reyes', email: 'karla@ejemplo.test', rol: 'asistente', activo: true },
  { id: 4, nombre: 'Jorge', apellido: 'Pineda', email: 'jorge@ejemplo.test', rol: 'asistente', activo: false },
  { id: 5, nombre: 'Paciente', apellido: 'Ejemplo', email: 'paciente@ejemplo.test', rol: 'paciente', activo: true },
];

const servicio = vi.hoisted(() => ({
  usuario: null as null | { id: number; nombre: string; apellido: string; email: string; rol: string },
  authFetch: vi.fn(),
}));

vi.mock('../../services/authService', () => ({
  API_URL: '/api',
  MENSAJE_SESION_EXPIRADA: 'Tu sesión expiró. Inicia sesión de nuevo.',
  MENSAJE_SIN_PERMISO: 'No tienes permiso para realizar esta acción.',
  getUsuario: () => servicio.usuario,
  clearSession: vi.fn(),
  login: vi.fn(),
  logout: vi.fn(async () => undefined),
  authFetch: servicio.authFetch,
  esErrorDeAcceso: () => false,
}));

const respuesta = (datos: unknown, status = 200) => ({ ok: status < 400, status, json: async () => datos }) as Response;

function montar() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/equipo']}>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  servicio.usuario = { id: 1, nombre: 'Laura', apellido: 'Mendoza', email: 'laura@ejemplo.test', rol: 'admin' };
  servicio.authFetch.mockReset();
  servicio.authFetch.mockImplementation((url: string, op?: RequestInit) =>
    Promise.resolve(op?.method === 'PATCH' ? respuesta({ mensaje: 'ok' }) : url === '/api/usuarios' ? respuesta(USUARIOS) : respuesta({}, 404)),
  );
});

const tabla = () => screen.getByRole('region', { name: 'Usuarios de la clínica' });
const editar = async (nombre: string) => {
  await userEvent.click(await screen.findByRole('button', { name: `Acceso de ${nombre}` }));
  return screen.getByRole('region', { name: 'Acceso' });
};

describe('Equipo y usuarios', () => {
  it('lista solo al personal, con rol (nombre neutro) y estado; sin pacientes', async () => {
    montar();
    await screen.findByRole('button', { name: 'Acceso de Laura Mendoza' });
    const filas = within(tabla()).getAllByRole('row').slice(1);
    expect(filas).toHaveLength(4);
    expect(filas[0]).toHaveTextContent('Laura Mendoza');
    expect(filas[0]).toHaveTextContent('Administración');
    expect(filas[3]).toHaveTextContent('Inactivo');
    expect(within(tabla()).queryByText('Paciente Ejemplo')).toBeNull();
  });

  it('sin controles sin backend: invitar, editar datos ni enlace de contraseña', async () => {
    montar();
    const panel = await editar('Karla Reyes');
    expect(screen.queryByRole('button', { name: /Invitar/ })).toBeNull();
    expect(screen.queryByRole('link', { name: /Invitar/ })).toBeNull();
    expect(within(panel).queryByRole('textbox')).toBeNull();
    expect(within(panel).queryByRole('button', { name: /Guardar/ })).toBeNull();
    expect(within(panel).queryByRole('button', { name: /Enviar enlace/ })).toBeNull();
    expect(screen.queryByRole('button', { name: /^Editar/ })).toBeNull();
  });

  it('desactivar llama a PATCH /desactivar y avisa', async () => {
    montar();
    const panel = await editar('Karla Reyes');
    expect(within(panel).getByRole('heading', { name: 'Acceso' })).toHaveFocus();
    await userEvent.click(within(panel).getByRole('button', { name: 'Desactivar usuario' }));
    expect(servicio.authFetch).toHaveBeenCalledWith('/api/usuarios/3/desactivar', { method: 'PATCH' });
    expect(await screen.findByText('La cuenta de Karla Reyes quedó inactiva. Sus sesiones se cerraron.')).toBeInTheDocument();
  });

  it('una cuenta inactiva se puede activar', async () => {
    montar();
    const panel = await editar('Jorge Pineda');
    await userEvent.click(within(panel).getByRole('button', { name: 'Activar usuario' }));
    expect(servicio.authFetch).toHaveBeenCalledWith('/api/usuarios/4/activar', { method: 'PATCH' });
  });

  it('la última administración activa no se puede desactivar (el backend también lo impide)', async () => {
    servicio.usuario = { id: 3, nombre: 'Karla', apellido: 'Reyes', email: 'karla@ejemplo.test', rol: 'admin' };
    montar();
    const panel = await editar('Laura Mendoza');
    expect(within(panel).getByText(/Es la única cuenta de administración activa/)).toBeInTheDocument();
    expect(within(panel).getByRole('button', { name: 'Desactivar usuario' })).toBeDisabled();
  });

  it('nadie se desactiva a sí mismo', async () => {
    const conOtraAdmin = [...USUARIOS, { id: 6, nombre: 'Otra', apellido: 'Admin', email: 'otra@ejemplo.test', rol: 'admin', activo: true }];
    servicio.authFetch.mockImplementation((url: string) => Promise.resolve(url === '/api/usuarios' ? respuesta(conOtraAdmin) : respuesta({}, 404)));
    montar();
    const panel = await editar('Laura Mendoza');
    expect(within(panel).getByText('No puedes desactivar tu propia cuenta.')).toBeInTheDocument();
    expect(within(panel).getByRole('button', { name: 'Desactivar usuario' })).toBeDisabled();
  });

  it('si la API rechaza (409 del último administrador), se muestra su mensaje', async () => {
    servicio.authFetch.mockImplementation((url: string, op?: RequestInit) =>
      Promise.resolve(
        op?.method === 'PATCH'
          ? respuesta({ mensaje: 'No se puede desactivar al último administrador activo' }, 409)
          : url === '/api/usuarios'
            ? respuesta(USUARIOS)
            : respuesta({}, 404),
      ),
    );
    montar();
    const panel = await editar('Karla Reyes');
    await userEvent.click(within(panel).getByRole('button', { name: 'Desactivar usuario' }));
    expect(await within(panel).findByRole('alert')).toHaveTextContent('No se puede desactivar al último administrador activo');
  });

  it('la asistente no entra (solo administración)', async () => {
    servicio.usuario = { ...servicio.usuario!, rol: 'asistente' };
    montar();
    expect(await screen.findByRole('heading', { name: 'No tienes acceso a esta sección' })).toBeInTheDocument();
    expect(servicio.authFetch).not.toHaveBeenCalledWith('/api/usuarios');
  });

  it('error al cargar: mensaje de Estados con Reintentar', async () => {
    servicio.authFetch.mockResolvedValue(respuesta({}, 500));
    montar();
    expect(await screen.findByRole('heading', { name: 'No pudimos cargar los usuarios' })).toBeInTheDocument();
  });

  it('sin violaciones de accesibilidad', async () => {
    const { container } = montar();
    await editar('Karla Reyes');
    await sinViolacionesAxe(container);
  });
});

describe('último acceso', () => {
  // "Ahora": miércoles 18 nov 2026, 9:30 en la clínica
  const AHORA = new Date('2026-11-18T15:30:00Z');

  it('hoy, ayer o la fecha, en la zona de la clínica; sin accesos si nunca entró', () => {
    expect(ultimoAcceso('2026-11-18T14:02:00Z', AHORA)).toBe('Hoy, 8:02');
    expect(ultimoAcceso('2026-11-18T00:40:00Z', AHORA)).toBe('Ayer, 18:40');
    expect(ultimoAcceso('2026-10-02T17:00:00Z', AHORA)).toBe('2 oct 2026');
    expect(ultimoAcceso(null, AHORA)).toBe('Sin accesos');
    expect(ultimoAcceso(undefined, AHORA)).toBe('Sin accesos');
  });

  it('se muestra en la tabla y en el panel de acceso', async () => {
    montar();
    const panel = await editar('Laura Mendoza');
    expect(within(tabla()).getByRole('columnheader', { name: 'Último acceso' })).toBeInTheDocument();
    expect(within(tabla()).getAllByText('Sin accesos').length).toBeGreaterThan(0);
    expect(within(panel).getByText('Último acceso')).toBeInTheDocument();
  });
});
