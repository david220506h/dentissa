import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { Rutas } from '../../app/Rutas';
import { ProveedorSesion } from '../../app/sesion';
import type { CitaAgenda } from '../agenda/logica';

// Datos de ejemplo del diseño (Agenda.dc.html), no reales. "Ahora": miércoles 18 nov 2026, 9:30 en la clínica.
const AHORA = new Date('2026-11-18T15:30:00Z');
const TODOS = ['08:00', '09:00', '10:00', '12:00', '13:00', '15:00'];

const cita = (id: number, paciente: string, fecha: string, hora: string, estado: CitaAgenda['estado'], horas = 1, dentistaId = 1): CitaAgenda => {
  const [d, m, a] = fecha.split('/');
  const inicio = new Date(`${a}-${m}-${d}T${hora}:00-06:00`);
  return {
    id,
    paciente,
    servicio: 'Resina dental',
    dentista: dentistaId === 1 ? 'Dra. Ana Torres' : 'Dr. Luis Lara',
    dentistaId,
    pacienteId: id,
    fecha,
    hora,
    inicio: inicio.toISOString(),
    fin: new Date(inicio.getTime() + horas * 3_600_000).toISOString(),
    estado,
    servicioId: 7,
  };
};

const CITAS = [
  cita(1, 'Rosa Aguilar', '18/11/2026', '08:00', 'confirmada'),
  cita(2, 'Ernesto Vega', '18/11/2026', '09:00', 'pendiente'),
  cita(3, 'Mario Delgado', '18/11/2026', '12:00', 'confirmada', 2),
  cita(4, 'Andrea Cruz', '16/11/2026', '08:00', 'completada'),
  cita(5, 'Sofía Rangel', '18/11/2026', '15:00', 'cancelada'),
  cita(6, 'Hugo Paredes', '18/11/2026', '10:00', 'confirmada', 1, 2),
];

const servicio = vi.hoisted(() => ({
  usuario: null as null | { id: number; nombre: string; apellido: string; email: string; rol: string },
  authFetch: vi.fn(),
  horarioFalla: false,
  extra: null as null | ((url: string, op?: RequestInit) => Promise<Response> | null),
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

function apiSimulada(url: string, opciones?: RequestInit) {
  const extra = servicio.extra?.(url, opciones);
  if (extra) return extra;
  if (opciones?.method === 'PATCH') return Promise.resolve(respuesta({ mensaje: 'ok' }));
  if (url === '/api/citas' || url.startsWith('/api/citas?')) return Promise.resolve(respuesta(CITAS));
  if (url === '/api/citas/dentistas') {
    return Promise.resolve(
      respuesta([
        { id: 1, nombre: 'Ana', apellido: 'Torres', titulo: 'Dra.' },
        { id: 2, nombre: 'Luis', apellido: 'Lara', titulo: 'Dr.' },
      ]),
    );
  }
  if (url.startsWith('/api/citas/horarios-disponibles')) {
    if (servicio.horarioFalla) return Promise.resolve(respuesta({}, 503));
    return Promise.resolve(respuesta(url.includes('fecha=2026-11-18') ? ['10:00', '13:00'] : TODOS));
  }
  return Promise.resolve(respuesta({}, 404));
}

function montar() {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/agenda']}>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(AHORA);
  servicio.usuario = { id: 1, nombre: 'Laura', apellido: 'Ejemplo', email: 'laura@ejemplo.test', rol: 'asistente' };
  servicio.horarioFalla = false;
  servicio.extra = null;
  servicio.authFetch.mockReset();
  servicio.authFetch.mockImplementation(apiSimulada);
});

afterEach(() => {
  vi.useRealTimers();
});


// Formularios de cita (CitaFormularios.dc.html) sobre la Agenda, con la API simulada.
const SERVICIOS = [
  { id: 7, nombre: 'Limpieza dental', precio: 500, duracionSlots: 1 },
  { id: 8, nombre: 'Tratamiento de conductos', precio: 3000, duracionSlots: 2 },
];
const libre = (hora: string, dentistas = [{ id: 1, nombre: 'Dra. Ana Torres' }, { id: 2, nombre: 'Dr. Luis Lara' }]) => ({ hora, inicio: '', fin: '', dentistasLibres: dentistas });

const enviados = () =>
  servicio.authFetch.mock.calls
    .filter(([, op]) => op?.method && op.method !== 'GET')
    .map(([url, op]) => ({ url: url as string, metodo: op!.method, cuerpo: op!.body ? JSON.parse(op!.body as string) : undefined }));

function apiFormularios(conflictos = 0) {
  let restantes = conflictos;
  servicio.extra = (url, op) => {
    if (url === '/api/citas/servicios') return Promise.resolve(respuesta(SERVICIOS));
    if (url.startsWith('/api/agenda/disponibilidad'))
      return Promise.resolve(respuesta(restantes < conflictos ? [libre('12:00'), libre('13:00')] : [libre('10:00'), libre('12:00'), libre('13:00')]));
    if (url.startsWith('/api/pacientes?buscar='))
      return Promise.resolve(respuesta({ elementos: [{ id: 41, nombre: 'Rosa', apellido: 'Aguilar', telefono: '55 5555 0101', alergias: 'Penicilina' }] }));
    if (url === '/api/pacientes' && op?.method === 'POST')
      return Promise.resolve(respuesta({ id: 99, nombre: 'Nueva', apellido: 'Persona', telefono: '5550001111', alergias: null }, 201));
    if (url === '/api/citas' && op?.method === 'POST') {
      if (restantes-- > 0) return Promise.resolve(respuesta({ mensaje: 'El dentista ya tiene una cita en ese horario.', codigo: 'HORARIO_OCUPADO' }, 409));
      return Promise.resolve(respuesta({ id: 500, mensaje: 'Cita creada correctamente' }, 201));
    }
    return null;
  };
}

async function abrirNuevaCita() {
  montar();
  await screen.findByRole('region', { name: 'Citas de la semana' });
  await userEvent.click(screen.getByRole('button', { name: 'Nueva cita' }));
  return screen.findByRole('dialog', { name: 'Nueva cita' });
}

describe('Nueva cita', () => {
  it('paciente existente con su alergia, horario libre, dentista y "paciente presente" en el cuerpo', async () => {
    apiFormularios();
    const dialogo = await abrirNuevaCita();
    await userEvent.type(within(dialogo).getByLabelText('Paciente'), 'Rosa');
    await userEvent.click(await within(dialogo).findByRole('button', { name: /Rosa Aguilar/ }));
    expect(within(dialogo).getByText('Alergia: Penicilina')).toBeInTheDocument();
    await userEvent.selectOptions(await within(dialogo).findByLabelText('Horario libre'), '10:00');
    await userEvent.selectOptions(within(dialogo).getByLabelText('Dentista'), 'Dr. Luis Lara');
    await userEvent.click(within(dialogo).getByLabelText(/La persona está en la clínica/));
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Guardar cita' }));

    expect(await screen.findByText('Cita creada.')).toBeInTheDocument();
    expect(enviados()).toContainEqual({
      url: '/api/citas',
      metodo: 'POST',
      cuerpo: expect.objectContaining({ pacienteId: 41, servicioId: 7, fecha: '2026-11-18', hora: '10:00:00', dentistaId: 2, pacientePresente: true, confirmarAlCrear: false }),
    });
  });

  it('409: avisa con horarios alternativos y guarda en el que se elige, sin perder los datos', async () => {
    apiFormularios(1);
    const dialogo = await abrirNuevaCita();
    await userEvent.type(within(dialogo).getByLabelText('Paciente'), 'Rosa');
    await userEvent.click(await within(dialogo).findByRole('button', { name: /Rosa Aguilar/ }));
    await userEvent.selectOptions(await within(dialogo).findByLabelText('Horario libre'), '10:00');
    await userEvent.click(within(dialogo).getByLabelText(/Crear ya confirmada/));
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Guardar cita' }));

    expect(await within(dialogo).findByText(/Ese horario ya no está libre/)).toBeInTheDocument();
    expect(within(dialogo).getByText(/Tus datos siguen/)).toBeInTheDocument();
    expect(within(dialogo).getByLabelText('Elige otro horario libre')).toHaveValue('12:00');
    await userEvent.click(await within(dialogo).findByRole('button', { name: 'Guardar en 12:00' }));

    expect(await screen.findByText('Cita creada y confirmada.')).toBeInTheDocument();
    const posts = enviados().filter((e) => e.url === '/api/citas');
    expect(posts.map((p) => p.cuerpo.hora)).toEqual(['10:00:00', '12:00:00']);
    expect(posts[1]!.cuerpo).toMatchObject({ pacienteId: 41, confirmarAlCrear: true });
  });

  it('alta rápida: valida el teléfono antes de llamar a la API y luego crea a la persona', async () => {
    apiFormularios();
    const dialogo = await abrirNuevaCita();
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Crear paciente nuevo' }));
    await userEvent.type(within(dialogo).getByLabelText('Nombre'), 'Nueva');
    await userEvent.type(within(dialogo).getByLabelText('Apellido'), 'Persona');
    await userEvent.type(within(dialogo).getByLabelText('Teléfono'), '123');
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Crear paciente' }));
    expect(within(dialogo).getByText('El teléfono debe tener de 10 a 15 dígitos.')).toBeInTheDocument();
    expect(enviados().some((e) => e.url === '/api/pacientes')).toBe(false);

    await userEvent.clear(within(dialogo).getByLabelText('Teléfono'));
    await userEvent.type(within(dialogo).getByLabelText('Teléfono'), '55 5000 1111');
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Crear paciente' }));
    expect(await within(dialogo).findByText('Nueva Persona')).toBeInTheDocument();
    expect(enviados()).toContainEqual({ url: '/api/pacientes', metodo: 'POST', cuerpo: { nombre: 'Nueva', apellido: 'Persona', telefono: '55 5000 1111' } });
  });

  it('sin violaciones de accesibilidad con el formulario abierto', async () => {
    apiFormularios();
    await abrirNuevaCita();
    await sinViolacionesAxe(document.body);
  });
});

describe('Diálogo «Cita»: acciones según la hora', () => {
  it('una confirmada que ya empezó (más de 15 min): completar y no asistió', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    // Rosa Aguilar, 8:00; "ahora" 9:30
    await userEvent.click(within(screen.getByRole('region', { name: 'Citas de la semana' })).getByRole('button', { name: /Rosa Aguilar/ }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cita' });
    expect(within(dialogo).getByRole('button', { name: 'Completar' })).toBeInTheDocument();
    expect(within(dialogo).queryByRole('button', { name: 'Cambiar de dentista' })).toBeNull();
    await userEvent.click(within(dialogo).getByRole('button', { name: 'No asistió' }));
    expect(servicio.authFetch).toHaveBeenCalledWith('/api/citas/1/no-asistio', { method: 'PATCH', body: '{}' });
    expect(await screen.findByText('Inasistencia registrada.')).toBeInTheDocument();
  });

  it('reagendar conserva el estado y manda la fecha y hora nuevas', async () => {
    apiFormularios();
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    // Mario Delgado, 12:00 (aún no empieza), confirmada
    await userEvent.click(within(screen.getByRole('region', { name: 'Citas de la semana' })).getByRole('button', { name: /Mario Delgado/ }));
    await userEvent.click(within(await screen.findByRole('dialog', { name: 'Cita' })).getByRole('button', { name: 'Reagendar' }));
    const reagendar = await screen.findByRole('dialog', { name: 'Reagendar cita' });
    expect(within(reagendar).getByText('Confirmada')).toBeInTheDocument();
    await userEvent.selectOptions(await within(reagendar).findByLabelText('Horario libre'), '13:00');
    await userEvent.click(within(reagendar).getByRole('button', { name: 'Reagendar' }));
    expect(await screen.findByText('Cita reagendada.')).toBeInTheDocument();
    expect(enviados()).toContainEqual({
      url: '/api/citas/3/reagendar',
      metodo: 'PATCH',
      cuerpo: { fecha: '2026-11-18', hora: '13:00:00', estadoResultante: 'confirmada' },
    });
  });
});
