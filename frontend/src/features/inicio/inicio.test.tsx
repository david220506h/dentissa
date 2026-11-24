import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { Rutas } from '../../app/Rutas';
import { ProveedorSesion } from '../../app/sesion';
import { citasDeHoy, citasSinConfirmar, fechaLarga, hoyEnClinica, rangoHorario, saludo, type CitaApi } from './datos';

// Datos de ejemplo del diseño (Inicio.dc.html), no reales. "Ahora": miércoles 18 nov 2026, 9:30 en la clínica.
const AHORA = new Date('2026-11-18T15:30:00Z');

const cita = (id: number, paciente: string, servicio: string, fecha: string, hora: string, inicio: string, fin: string, estado: CitaApi['estado']): CitaApi => ({
  id,
  paciente,
  servicio,
  dentista: 'Dra. Ejemplo',
  fecha,
  hora,
  inicio,
  fin,
  estado,
});

const CITAS: CitaApi[] = [
  cita(3, 'Mario Delgado', 'Tratamiento de conductos', '18/11/2026', '12:00', '2026-11-18T18:00:00Z', '2026-11-18T20:00:00Z', 'confirmada'),
  cita(1, 'Rosa Aguilar', 'Resina dental', '18/11/2026', '08:00', '2026-11-18T14:00:00Z', '2026-11-18T15:00:00Z', 'confirmada'),
  cita(2, 'Ernesto Vega', 'Extracción dental', '18/11/2026', '09:00', '2026-11-18T15:00:00Z', '2026-11-18T16:00:00Z', 'pendiente'),
  cita(4, 'Sofía Rangel', 'Limpieza dental', '18/11/2026', '15:00', '2026-11-18T21:00:00Z', '2026-11-18T22:00:00Z', 'cancelada'),
  cita(5, 'Roberto Salas', 'Limpieza dental', '19/11/2026', '15:00', '2026-11-19T21:00:00Z', '2026-11-19T22:00:00Z', 'pendiente'),
  cita(6, 'Camila Ortega', 'Resina dental', '17/11/2026', '08:00', '2026-11-17T14:00:00Z', '2026-11-17T15:00:00Z', 'pendiente'),
];

describe('datos de Inicio', () => {
  it('"hoy" y la fecha se calculan en la zona de la clínica', () => {
    // 23:30 del 18 en Ciudad de México ya es 19 en UTC: sigue siendo 18 en la clínica
    expect(hoyEnClinica(new Date('2026-11-19T05:30:00Z'))).toBe('18/11/2026');
    expect(fechaLarga(AHORA, true)).toBe('Miércoles 18 de noviembre de 2026');
    expect(fechaLarga(AHORA, false)).toBe('Miércoles 18 de noviembre');
  });

  it('saludo según la hora de la clínica', () => {
    expect(saludo(AHORA)).toBe('Buenos días');
    expect(saludo(new Date('2026-11-18T20:00:00Z'))).toBe('Buenas tardes');
    expect(saludo(new Date('2026-11-19T02:00:00Z'))).toBe('Buenas noches');
  });

  it('citas de hoy en orden, sin canceladas; varias horas como rango', () => {
    const hoy = citasDeHoy(CITAS, AHORA);
    expect(hoy.map((c) => c.paciente)).toEqual(['Rosa Aguilar', 'Ernesto Vega', 'Mario Delgado']);
    expect(hoy.map(rangoHorario)).toEqual(['8:00', '9:00', '12:00 a 14:00']);
  });

  it('sin confirmar: pendientes que aún no ocurren (la de las 9:00 ya empezó; la de ayer no cuenta)', () => {
    expect(citasSinConfirmar(CITAS, AHORA)).toBe(1);
    expect(citasSinConfirmar(CITAS, new Date('2026-11-18T14:30:00Z'))).toBe(2);
  });
});

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
      <MemoryRouter initialEntries={['/']}>
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
  servicio.authFetch.mockReset();
  servicio.usuario = { id: 1, nombre: 'Laura', apellido: 'Ejemplo', email: 'laura@ejemplo.test', rol: 'asistente' };
});

afterEach(() => {
  vi.useRealTimers();
});

describe('pantalla de Inicio', () => {
  it('recepción: citas de hoy, sin confirmar y la agenda del día desde GET /api/citas', async () => {
    servicio.authFetch.mockResolvedValue(respuesta(CITAS));
    montar();
    expect(await screen.findByRole('region', { name: 'Citas de hoy' })).toHaveTextContent('3');
    expect(screen.getAllByRole('region', { name: 'Sin confirmar' })[0]).toHaveTextContent('1');
    expect(screen.queryByText('Por llamar')).toBeNull();
    expect(screen.queryByText('Pendientes de llamada')).toBeNull();
    expect(servicio.authFetch).toHaveBeenCalledWith('/api/citas');
    const agenda = screen.getByRole('region', { name: /Agenda de hoy/ });
    const filas = within(agenda).getAllByRole('listitem');
    expect(filas).toHaveLength(3);
    expect(filas[2]).toHaveTextContent('12:00 a 14:00');
    expect(filas[2]).toHaveTextContent('Mario Delgado');
    expect(filas[1]).toHaveTextContent('Pendiente');
  });

  it('nada inventado: sin solicitudes, existencias, buscador, notificaciones ni "Nueva cita"', async () => {
    servicio.authFetch.mockResolvedValue(respuesta(CITAS));
    montar();
    await screen.findByRole('region', { name: 'Citas de hoy' });
    for (const texto of [/Solicitudes por revisar/, /Existencias bajas/, /Solicitudes nuevas/, /horarios del día/, /Horario libre/, /Descanso/]) {
      expect(screen.queryByText(texto)).toBeNull();
    }
    expect(screen.queryByRole('searchbox')).toBeNull();
    expect(screen.queryByRole('button', { name: /Notificaciones/ })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Nueva cita' })).toBeNull();
    expect(screen.queryByRole('link', { name: 'Nueva cita' })).toBeNull();
  });

  it('dentista: solo su agenda de hoy (mi-agenda) y sin "Sin confirmar"', async () => {
    servicio.usuario = { ...servicio.usuario!, rol: 'dentista' };
    servicio.authFetch.mockResolvedValue(respuesta(CITAS.filter((c) => c.id === 1)));
    montar();
    expect(await screen.findByRole('region', { name: 'Citas de hoy' })).toHaveTextContent('1');
    expect(servicio.authFetch).toHaveBeenCalledWith('/api/citas/mi-agenda?desde=2026-11-18&hasta=2026-11-18');
    expect(screen.queryByRole('region', { name: 'Sin confirmar' })).toBeNull();
  });

  it('día sin citas: estado vacío de Estados', async () => {
    servicio.authFetch.mockResolvedValue(respuesta([]));
    montar();
    expect(await screen.findByRole('heading', { name: 'No hay citas este día' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Citas de hoy' })).toHaveTextContent('0');
  });

  it('error: mensaje de Estados sin indicadores, y "Reintentar" vuelve a pedir', async () => {
    // La primera petición de citas falla; la bandeja de Solicitudes (contador del menú) responde aparte
    let pedidasCitas = 0;
    servicio.authFetch.mockImplementation((url: string) =>
      Promise.resolve(url.includes('/solicitudes') ? respuesta([]) : pedidasCitas++ === 0 ? respuesta({}, 500) : respuesta(CITAS)),
    );
    montar();
    expect(await screen.findByRole('heading', { name: 'No pudimos cargar las citas' })).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Citas de hoy' })).toBeNull();
    vi.useRealTimers();
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByRole('region', { name: 'Citas de hoy' })).toHaveTextContent('3');
  });

  it('mientras carga: esqueleto con su etiqueta', async () => {
    servicio.authFetch.mockReturnValue(new Promise(() => {}));
    montar();
    expect(await screen.findByRole('status', { name: 'Cargando citas' })).toBeInTheDocument();
  });

  it('sin violaciones de accesibilidad', async () => {
    servicio.authFetch.mockResolvedValue(respuesta(CITAS));
    const { container } = montar();
    await screen.findByRole('region', { name: 'Citas de hoy' });
    await sinViolacionesAxe(container);
  });
});
