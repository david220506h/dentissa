import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sinViolacionesAxe } from '@dentissa/ui/prueba/axe';
import { cuerpo, errores, FORMULARIO_VACIO, sumarDias } from './logica';
import { SitioPublico } from './Sitio';

// Datos inventados; la API está simulada
const SERVICIOS = [
  { id: 1, nombre: 'Limpieza dental', precio: 600, duracionSlots: 1 },
  { id: 6, nombre: 'Tratamiento de conductos', precio: 3500, duracionSlots: 2 },
];

const respuesta = (datos: unknown, status = 200) => ({ ok: status < 400, status, json: async () => datos }) as Response;
const llamadas: { url: string; init?: RequestInit }[] = [];
let alEnviar: () => Response = () => respuesta({ mensaje: 'Recibimos tu solicitud.' }, 201);

beforeEach(() => {
  llamadas.length = 0;
  alEnviar = () => respuesta({ mensaje: 'Recibimos tu solicitud.' }, 201);
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string, init?: RequestInit) => {
      llamadas.push({ url, init });
      if (url.endsWith('/publico/servicios')) return respuesta(SERVICIOS);
      if (url.includes('/publico/disponibilidad')) return respuesta([{ fecha: '2026-12-01', horas: ['12:00', '13:00'] }]);
      if (url.endsWith('/publico/solicitudes')) return alEnviar();
      return respuesta({}, 404);
    }),
  );
});
afterEach(() => vi.unstubAllGlobals());

const montar = (ruta: string) =>
  render(
    <MemoryRouter initialEntries={[ruta]}>
      <SitioPublico />
    </MemoryRouter>,
  );

describe('Sitio público: lógica', () => {
  it('valida como el backend', () => {
    const e = errores(FORMULARIO_VACIO);
    expect(Object.keys(e).sort()).toEqual(['aceptaAviso', 'apellido', 'correo', 'fecha', 'hora', 'nombre', 'servicioId', 'telefono'].sort());
    expect(errores({ ...FORMULARIO_VACIO, nombre: 'Ana3' }).nombre).toBeDefined();
    expect(errores({ ...FORMULARIO_VACIO, telefono: '55-ABC-1234' }).telefono).toBeDefined();
    expect(errores({ ...FORMULARIO_VACIO, telefono: '(55) 1234-5678' }).telefono).toBeUndefined();
  });

  it('cuerpo limpio y suma de días', () => {
    expect(cuerpo({ ...FORMULARIO_VACIO, servicioId: '1', nombre: ' Ana ', motivo: '  ' })).toMatchObject({ servicioId: 1, nombre: 'Ana', motivo: null });
    expect(sumarDias('2026-11-30', 2)).toBe('2026-12-02');
  });
});

describe('Sitio público', () => {
  it('inicio: servicios, horario, contacto de ejemplo y botón Solicitar cita', async () => {
    const { container } = montar('/');
    expect(await screen.findByRole('heading', { name: 'Limpieza dental' })).toBeInTheDocument();
    expect(screen.getByText(/la clínica y sus datos son de ejemplo/)).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /Solicitar cita/ })[0]).toHaveAttribute('href', '/solicitar-cita');
    expect(screen.getAllByRole('link', { name: 'Acceso del personal' })[0]).toHaveAttribute('href', '/panel');
    await sinViolacionesAxe(container);
  });

  it('solicitar cita: valida, envía y confirma', async () => {
    const { container } = montar('/solicitar-cita');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar solicitud' }));
    expect(screen.getByText('Para enviar la solicitud, acepta el aviso de privacidad.')).toBeInTheDocument();
    expect(llamadas.some((l) => l.url.endsWith('/publico/solicitudes'))).toBe(false);

    await userEvent.selectOptions(await screen.findByLabelText('Servicio'), 'Limpieza dental');
    await userEvent.type(screen.getByLabelText('Fecha'), '2026-12-01');
    await userEvent.selectOptions(await screen.findByLabelText('Horario libre'), '12:00');
    await userEvent.type(screen.getByLabelText('Nombre'), 'Ana');
    await userEvent.type(screen.getByLabelText('Apellido'), 'Ejemplo');
    await userEvent.type(screen.getByLabelText('Teléfono'), '55 1234 5678');
    await userEvent.type(screen.getByLabelText('Correo'), 'ana@ejemplo.test');
    await userEvent.click(screen.getByLabelText('Leí y acepto el aviso de privacidad'));
    await sinViolacionesAxe(container);
    await userEvent.click(screen.getByRole('button', { name: 'Enviar solicitud' }));

    expect(await screen.findByRole('heading', { name: 'Recibimos tu solicitud' })).toBeInTheDocument();
    expect(screen.getByText('La clínica te contactará para confirmar.')).toBeInTheDocument();
    const envio = llamadas.find((l) => l.url.endsWith('/publico/solicitudes'))!;
    expect(JSON.parse(envio.init!.body as string)).toMatchObject({
      servicioId: 1, fecha: '2026-12-01', hora: '12:00', nombre: 'Ana', correo: 'ana@ejemplo.test', aceptaAviso: true, sitioWeb: '',
    });
  });

  it('horario ocupado (409): avisa y pide elegir otro', async () => {
    alEnviar = () => respuesta({ mensaje: 'Ese horario se acaba de ocupar. Elige otro, por favor.' }, 409);
    montar('/solicitar-cita');
    await userEvent.selectOptions(await screen.findByLabelText('Servicio'), 'Limpieza dental');
    await userEvent.type(screen.getByLabelText('Fecha'), '2026-12-01');
    await userEvent.selectOptions(await screen.findByLabelText('Horario libre'), '13:00');
    for (const [campo, valor] of [['Nombre', 'Ana'], ['Apellido', 'Ejemplo'], ['Teléfono', '5512345678'], ['Correo', 'ana@ejemplo.test']])
      await userEvent.type(screen.getByLabelText(campo!), valor!);
    await userEvent.click(screen.getByLabelText('Leí y acepto el aviso de privacidad'));
    await userEvent.click(screen.getByRole('button', { name: 'Enviar solicitud' }));
    const aviso = await screen.findByText('Ese horario se acaba de ocupar. Elige otro, por favor.');
    expect(aviso).toBeInTheDocument();
    expect(within(screen.getByLabelText('Horario libre').closest('div')!.parentElement!).getByLabelText('Horario libre')).toHaveValue('');
  });
});
