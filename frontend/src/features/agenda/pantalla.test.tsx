import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { Rutas } from '../../app/Rutas';
import { ProveedorSesion } from '../../app/sesion';
import type { CitaAgenda } from './logica';

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
  servicio.authFetch.mockReset();
  servicio.authFetch.mockImplementation(apiSimulada);
});

afterEach(() => {
  vi.useRealTimers();
});

const semana = () => screen.getByRole('region', { name: 'Citas de la semana' });

describe('Agenda: semana', () => {
  it('cuadrícula de lunes a sábado con descansos deducidos y citas del dentista elegido', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await waitFor(() => expect(within(semana()).getAllByText('Descanso')).toHaveLength(2));
    expect(screen.getAllByText('Semana del 16 al 21 de noviembre de 2026').length).toBeGreaterThan(0);
    for (const d of ['Lun', 'Mar', 'Mié · hoy', 'Jue', 'Vie', 'Sáb']) expect(within(semana()).getByText(d)).toBeInTheDocument();
    expect(within(semana()).getByRole('button', { name: /Rosa Aguilar/ })).toBeInTheDocument();
    expect(within(semana()).getByRole('button', { name: /Andrea Cruz.*Completada/ })).toBeInTheDocument();
    // La cancelada libera el horario; la del otro dentista no aparece con la Dra. Torres
    expect(within(semana()).queryByText('Sofía Rangel')).toBeNull();
    expect(within(semana()).queryByText('Hugo Paredes')).toBeNull();
  });

  it('el filtro de dentista cambia las citas', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await userEvent.selectOptions(screen.getByLabelText('Dentista'), 'Dr. Luis Lara');
    expect(within(semana()).getByRole('button', { name: /Hugo Paredes/ })).toBeInTheDocument();
    expect(within(semana()).queryByText('Rosa Aguilar')).toBeNull();
  });

  it('semana siguiente y "Hoy"', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await userEvent.click(screen.getByRole('button', { name: 'Semana siguiente' }));
    expect(screen.getAllByText('23 – 28 nov 2026').length).toBeGreaterThan(0);
    await userEvent.click(screen.getByRole('button', { name: 'Hoy' }));
    expect(screen.getAllByText('16 – 21 nov 2026').length).toBeGreaterThan(0);
  });

  it('recepción tiene "Nueva cita"; una completada no tiene acciones', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    expect(screen.getByRole('button', { name: 'Nueva cita' })).toBeInTheDocument();
    for (const nombre of ['Agendar', 'Mes']) expect(screen.queryByRole('button', { name: nombre })).toBeNull();
    expect(screen.queryByRole('searchbox')).toBeNull();
    await userEvent.click(within(semana()).getByRole('button', { name: /Andrea Cruz/ }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cita' });
    for (const nombre of [/Completar/, /No asistió/, /Reagendar/, /Historial/, /Cancelar cita/, /Confirmar cita/]) {
      expect(within(dialogo).queryByRole('button', { name: nombre })).toBeNull();
    }
  });
});

describe('Agenda: confirmar y cancelar', () => {
  it('confirmar una cita pendiente llama a PATCH /confirmar y avisa', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await userEvent.click(within(semana()).getByRole('button', { name: /Ernesto Vega/ }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cita' });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar cita' }));
    expect(servicio.authFetch).toHaveBeenCalledWith('/api/citas/2/confirmar', { method: 'PATCH', body: '{}' });
    expect(await screen.findByText('Cita confirmada.')).toBeInTheDocument();
  });

  it('una confirmada no se confirma; al cancelar, el motivo elegido y la nota van en el cuerpo', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await userEvent.click(within(semana()).getByRole('button', { name: /Rosa Aguilar/ }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cita' });
    expect(within(dialogo).queryByRole('button', { name: 'Confirmar cita' })).toBeNull();
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar cita' }));
    const cancelar = await screen.findByRole('dialog', { name: 'Cancelar cita' });
    expect(within(cancelar).queryByText(/Avisar/)).toBeNull();
    await userEvent.selectOptions(within(cancelar).getByLabelText('Motivo de la cancelación'), 'La clínica debe reprogramar');
    await userEvent.type(within(cancelar).getByLabelText(/Nota/), 'Agenda de la dentista');
    await userEvent.click(within(cancelar).getByRole('button', { name: 'Cancelar cita' }));
    expect(servicio.authFetch).toHaveBeenCalledWith('/api/citas/1/cancelar', {
      method: 'PATCH',
      body: JSON.stringify({ motivo: 'La clínica debe reprogramar. Agenda de la dentista' }),
    });
    expect(await screen.findByText('Cita cancelada.')).toBeInTheDocument();
  });

  it('si la API rechaza, el diálogo muestra su mensaje', async () => {
    servicio.authFetch.mockImplementation((url: string, op?: RequestInit) =>
      op?.method === 'PATCH' ? Promise.resolve(respuesta({ mensaje: 'Solo se puede confirmar una cita pendiente' }, 400)) : apiSimulada(url, op),
    );
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await userEvent.click(within(semana()).getByRole('button', { name: /Ernesto Vega/ }));
    await userEvent.click(within(await screen.findByRole('dialog', { name: 'Cita' })).getByRole('button', { name: 'Confirmar cita' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Solo se puede confirmar una cita pendiente');
  });
});

describe('Agenda: día, errores y permisos', () => {
  it('vista Día: libres del día, descansos y citas; las de varios horarios no repiten fila', async () => {
    montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await userEvent.click(screen.getByRole('button', { name: 'Día' }));
    const dia = screen.getByRole('region', { name: 'Miércoles 18 de noviembre de 2026' });
    await waitFor(() => expect(within(dia).getAllByText('Libre')).toHaveLength(1));
    const filas = within(dia).getAllByRole('listitem').map((li) => li.textContent);
    expect(filas).toEqual([
      expect.stringContaining('8:00Rosa Aguilar'),
      expect.stringContaining('9:00Ernesto Vega'),
      '10:00Libre',
      '11:00Descanso',
      expect.stringContaining('12:00 a 14:00Mario Delgado'),
      '14:00Descanso',
    ]);
    expect(filas[4]).toContain('2 horarios');
  });

  it('sin horario de la clínica (503): aviso y solo las horas con citas', async () => {
    servicio.horarioFalla = true;
    montar();
    expect(await screen.findByText(/No se pudo leer el horario de la clínica/)).toBeInTheDocument();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    expect(within(semana()).queryByText('Descanso')).toBeNull();
    expect(within(semana()).getByRole('button', { name: /Rosa Aguilar/ })).toBeInTheDocument();
  });

  it('error al cargar las citas: mensaje de Estados con Reintentar', async () => {
    servicio.authFetch.mockImplementation((url: string, op?: RequestInit) =>
      url.startsWith('/api/citas?') ? Promise.resolve(respuesta({}, 500)) : apiSimulada(url, op),
    );
    montar();
    expect(await screen.findByRole('heading', { name: 'No pudimos cargar las citas' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Reintentar' })).toBeInTheDocument();
  });

  it('el dentista no entra a Agenda por ahora (solo Inicio)', async () => {
    servicio.usuario = { ...servicio.usuario!, rol: 'dentista' };
    montar();
    expect(await screen.findByRole('heading', { name: 'No tienes acceso a esta sección' })).toBeInTheDocument();
  });

  it('sin violaciones de accesibilidad', async () => {
    const { container } = montar();
    await screen.findByRole('region', { name: 'Citas de la semana' });
    await sinViolacionesAxe(container);
  });
});
