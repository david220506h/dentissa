import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { Rutas } from '../../app/Rutas';
import { ProveedorSesion } from '../../app/sesion';
import type { PacienteDetalle, PaginaPacientes } from './logica';

// Datos inventados con correos .test; no son reales
const PAGINA: PaginaPacientes = {
  elementos: [
    {
      id: 7,
      nombre: 'Rosa',
      apellido: 'Aguilar',
      telefono: '5512345678',
      cuenta: 'con-cuenta',
      alergias: 'Penicilina',
      ultimaCita: { id: 1, fecha: '2026-10-02', hora: '09:00', estado: 'completada', servicio: 'Limpieza' },
      proximaCita: { id: 2, fecha: '2026-12-04', hora: '10:00', estado: 'confirmada', servicio: 'Resina' },
    },
    {
      id: 8,
      nombre: 'Tomás',
      apellido: 'Ibarra',
      telefono: null,
      cuenta: 'invitado',
      alergias: null,
      ultimaCita: null,
      proximaCita: null,
    },
  ],
  total: 2,
  pagina: 1,
  tamano: 10,
};

const DETALLE: PacienteDetalle = {
  ...PAGINA.elementos[0]!,
  correo: 'rosa@ejemplo.test',
  fechaNacimiento: '1990-05-14',
  sexo: 'femenino',
  proximasCitas: [{ id: 2, fecha: '2026-12-04', hora: '10:00', estado: 'confirmada', servicio: 'Resina' }],
};

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

function montar(ruta = '/pacientes') {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={[ruta]}>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const usuarioDe = (rol: string) => ({ id: 1, nombre: 'Karla', apellido: 'Reyes', email: 'karla@ejemplo.test', rol });
const llamadas = () => servicio.authFetch.mock.calls.map(([url, op]) => ({ url: url as string, metodo: (op as RequestInit | undefined)?.method ?? 'GET', cuerpo: (op as RequestInit | undefined)?.body }));

beforeEach(() => {
  servicio.usuario = usuarioDe('asistente');
  servicio.authFetch.mockReset();
  servicio.authFetch.mockImplementation((url: string, op?: RequestInit) => {
    if (op?.method === 'POST' && url === '/api/pacientes') {
      const cuerpo = JSON.parse(op.body as string);
      return Promise.resolve(respuesta({ ...DETALLE, ...cuerpo, id: 9, proximasCitas: [] }, 201));
    }
    if (op?.method === 'PUT' && url === '/api/pacientes/7') return Promise.resolve(respuesta({ ...DETALLE, ...JSON.parse(op.body as string) }));
    if (url.startsWith('/api/pacientes?')) return Promise.resolve(respuesta(PAGINA));
    if (url === '/api/pacientes/7') return Promise.resolve(respuesta(DETALLE));
    return Promise.resolve(respuesta({}, 404));
  });
});

describe('Pacientes', () => {
  it('lista con alergia, cuenta y próxima cita; sin expediente', async () => {
    const { container } = montar();
    const tabla = await screen.findByRole('region', { name: 'Pacientes' });
    const filas = within(tabla).getAllByRole('row').slice(1);
    expect(filas).toHaveLength(2);
    expect(filas[0]).toHaveTextContent('Rosa Aguilar');
    expect(filas[0]).toHaveTextContent('Penicilina');
    expect(filas[0]).toHaveTextContent('4 dic 2026');
    expect(filas[1]).toHaveTextContent('Sin registro');
    expect(filas[1]).toHaveTextContent('Invitado');
    expect(screen.queryByText(/odontograma|expediente clínico completo/i)).toBeNull();
    expect(screen.getByRole('button', { name: 'Nuevo paciente' })).toBeInTheDocument();
    await sinViolacionesAxe(container);
  });

  it('busca por nombre o teléfono desde 2 caracteres', async () => {
    montar();
    await screen.findByRole('region', { name: 'Pacientes' });
    await userEvent.type(screen.getByLabelText('Buscar paciente'), '55 12');
    await vi.waitFor(() => expect(llamadas().some((l) => l.url.includes('buscar=55%2012'))).toBe(true));
    expect(llamadas().every((l) => !l.url.includes('buscar=5&'))).toBe(true);
  });

  it('el dentista no da de alta ni edita', async () => {
    servicio.usuario = usuarioDe('dentista');
    montar();
    await screen.findByRole('region', { name: 'Pacientes' });
    expect(screen.queryByRole('button', { name: 'Nuevo paciente' })).toBeNull();
  });

  it('alta: valida en el panel y envía el cuerpo limpio', async () => {
    montar();
    await userEvent.click(await screen.findByRole('button', { name: 'Nuevo paciente' }));
    const dialogo = screen.getByRole('dialog', { name: 'Nuevo paciente' });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Guardar' }));
    expect(within(dialogo).getByText('Indica al menos un teléfono o un correo.')).toBeInTheDocument();
    expect(llamadas().some((l) => l.metodo === 'POST')).toBe(false);

    await userEvent.type(within(dialogo).getByLabelText('Nombre'), 'Elena');
    await userEvent.type(within(dialogo).getByLabelText('Apellido'), 'Campos');
    await userEvent.type(within(dialogo).getByLabelText('Correo'), 'elena@ejemplo.test');
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByText('Se registró a Elena Campos.')).toBeInTheDocument();
    const post = llamadas().find((l) => l.metodo === 'POST');
    expect(JSON.parse(post!.cuerpo as string)).toEqual({
      nombre: 'Elena',
      apellido: 'Campos',
      telefono: null,
      correo: 'elena@ejemplo.test',
      fechaNacimiento: null,
      sexo: null,
      alergias: null,
    });
  });

  it('ficha: datos, alergia y próximas citas; edición con PUT', async () => {
    const { container } = montar('/pacientes/7');
    expect(await screen.findByRole('heading', { name: 'Rosa Aguilar' })).toBeInTheDocument();
    expect(screen.getByText('rosa@ejemplo.test')).toBeInTheDocument();
    expect(screen.getByText('Alergia: Penicilina')).toBeInTheDocument();
    const proximas = screen.getByRole('region', { name: 'Próximas citas' });
    expect(proximas).toHaveTextContent('4 dic 2026, 10:00');
    expect(proximas).toHaveTextContent('Resina');
    await sinViolacionesAxe(container);

    await userEvent.click(screen.getByRole('button', { name: 'Editar' }));
    const dialogo = screen.getByRole('dialog', { name: 'Editar paciente' });
    const alergias = within(dialogo).getByLabelText('Alergias (opcional)');
    await userEvent.clear(alergias);
    await userEvent.type(alergias, 'Penicilina y látex');
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByText('Cambios guardados.')).toBeInTheDocument();
    const put = llamadas().find((l) => l.metodo === 'PUT');
    expect(JSON.parse(put!.cuerpo as string).alergias).toBe('Penicilina y látex');
  });

  it('ficha inexistente: mensaje claro', async () => {
    montar('/pacientes/99');
    expect(await screen.findByText('No encontramos a esta persona')).toBeInTheDocument();
  });
});
