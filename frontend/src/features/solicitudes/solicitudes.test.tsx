import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { Rutas } from '../../app/Rutas';
import { ProveedorSesion } from '../../app/sesion';
import type { Solicitud } from './Solicitudes';

// Solicitudes inventadas (correos .test)
const SOLICITUDES: Solicitud[] = [
  {
    id: 41, version: 7, paciente: 'Ana Ejemplo', telefono: '55 1234 5678', correo: 'ana@ejemplo.test', pacienteId: 3,
    servicio: 'Limpieza dental', fecha: '2026-12-01', hora: '12:00', motivo: 'Revisión anual', dentista: 'Dra. Mariana Soto',
    creadaEn: '2026-11-24T15:00:00Z', expiraEn: '2026-11-25T03:00:00Z',
  },
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
let pendientes: Solicitud[] = [];

function montar() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/solicitudes']}>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const llamadas = () => servicio.authFetch.mock.calls.map(([url, op]) => ({ url: url as string, metodo: (op as RequestInit | undefined)?.method ?? 'GET', cuerpo: (op as RequestInit | undefined)?.body }));

beforeEach(() => {
  pendientes = [...SOLICITUDES];
  servicio.usuario = { id: 1, nombre: 'Karla', apellido: 'Reyes', email: 'karla@ejemplo.test', rol: 'asistente' };
  servicio.authFetch.mockReset();
  servicio.authFetch.mockImplementation((url: string, op?: RequestInit) => {
    if (op?.method === 'PATCH') {
      pendientes = [];
      return Promise.resolve(respuesta({ mensaje: 'ok' }));
    }
    if (url === '/api/solicitudes') return Promise.resolve(respuesta(pendientes));
    return Promise.resolve(respuesta([], 200));
  });
});

describe('Solicitudes', () => {
  it('lista con contacto, servicio, horario y contador en el menú', async () => {
    const { container } = montar();
    const lista = await screen.findByRole('list', { name: 'Solicitudes por revisar' });
    expect(lista).toHaveTextContent('Ana Ejemplo');
    expect(lista).toHaveTextContent('Limpieza dental · 1 dic 2026, 12:00');
    expect(lista).toHaveTextContent('ana@ejemplo.test');
    expect(screen.getAllByText('1 solicitud por revisar', { exact: false }).length).toBeGreaterThan(0);
    await sinViolacionesAxe(container);
  });

  it('confirmar: PATCH confirmar con la versión y aviso', async () => {
    montar();
    await userEvent.click(await screen.findByRole('button', { name: 'Confirmar la solicitud de Ana Ejemplo' }));
    expect(await screen.findByText(/Cita confirmada: Ana Ejemplo/)).toBeInTheDocument();
    const patch = llamadas().find((l) => l.metodo === 'PATCH')!;
    expect(patch.url).toBe('/api/citas/41/confirmar');
    expect(JSON.parse(patch.cuerpo as string)).toEqual({ canal: 'panel', version: 7 });
    expect(await screen.findByText('No hay solicitudes por revisar')).toBeInTheDocument();
  });

  it('rechazar: motivo neutro y PATCH cancelar', async () => {
    montar();
    await userEvent.click(await screen.findByRole('button', { name: 'Rechazar la solicitud de Ana Ejemplo' }));
    const dialogo = screen.getByRole('dialog', { name: 'Rechazar solicitud' });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Rechazar' }));
    expect(await screen.findByText(/Se rechazó la solicitud de Ana Ejemplo/)).toBeInTheDocument();
    const patch = llamadas().find((l) => l.metodo === 'PATCH')!;
    expect(patch.url).toBe('/api/citas/41/cancelar');
    expect(JSON.parse(patch.cuerpo as string)).toEqual({ motivo: 'La clínica no tiene ese horario disponible', canal: 'panel', version: 7 });
  });

  it('el dentista no ve Solicitudes', async () => {
    servicio.usuario = { ...servicio.usuario!, rol: 'dentista' };
    montar();
    expect(screen.queryByRole('list', { name: 'Solicitudes por revisar' })).toBeNull();
    expect(llamadas().some((l) => l.url === '/api/solicitudes')).toBe(false);
  });
});
