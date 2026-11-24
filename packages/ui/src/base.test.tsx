import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import {
  Aviso,
  Avatar,
  Boton,
  Busqueda,
  CampoAreaTexto,
  CampoContrasena,
  CampoSeleccion,
  CampoTexto,
  Contador,
  Icono,
  Insignia,
  InsigniaEstadoCita,
  NOMBRES_ICONOS,
  Tarjeta,
  TarjetaIndicador,
  iniciales,
  type EstadoCita,
} from './index';
import { sinViolacionesAxe } from './prueba/axe';

describe('Icono', () => {
  it('es decorativo sin etiqueta y con nombre cuando la tiene', () => {
    const { container } = render(
      <>
        <Icono nombre="agenda" />
        <Icono nombre="alerta" etiqueta="Alergia" />
      </>,
    );
    const [decorativo, conNombre] = container.querySelectorAll('svg');
    expect(decorativo).toHaveAttribute('aria-hidden', 'true');
    expect(conNombre).toHaveAttribute('role', 'img');
    expect(screen.getByRole('img', { name: 'Alergia' })).toBeInTheDocument();
  });

  it('todos los íconos del diseño se dibujan', () => {
    for (const nombre of NOMBRES_ICONOS) {
      const { container, unmount } = render(<Icono nombre={nombre} />);
      expect(container.querySelector('svg')?.childElementCount, nombre).toBeGreaterThan(0);
      unmount();
    }
  });
});

describe('Boton', () => {
  it('es type="button" por defecto y ejecuta su acción', async () => {
    const alPulsar = vi.fn();
    render(
      <Boton variante="principal" icono="confirmar" onClick={alPulsar}>
        Confirmar cita
      </Boton>,
    );
    const boton = screen.getByRole('button', { name: 'Confirmar cita' });
    expect(boton).toHaveAttribute('type', 'button');
    await userEvent.click(boton);
    expect(alPulsar).toHaveBeenCalledOnce();
  });

  it('se activa con el teclado (Enter y Espacio)', async () => {
    const alPulsar = vi.fn();
    render(<Boton onClick={alPulsar}>Proponer otro horario</Boton>);
    await userEvent.tab();
    await userEvent.keyboard('{Enter}');
    await userEvent.keyboard(' ');
    expect(alPulsar).toHaveBeenCalledTimes(2);
  });

  it('al cargar queda deshabilitado, ocupado y dice "Guardando…"', () => {
    render(<Boton cargando>Guardar cambios</Boton>);
    const boton = screen.getByRole('button', { name: 'Guardando…' });
    expect(boton).toBeDisabled();
    expect(boton).toHaveAttribute('aria-busy', 'true');
  });

  it('al cargar conserva su variante (no se ve como deshabilitado)', () => {
    render(<Boton variante="principal" cargando>Guardar cambios</Boton>);
    const boton = screen.getByRole('button', { name: 'Guardando…' });
    expect(boton.className).toMatch(/principal/);
    expect(boton).toHaveAttribute('aria-busy', 'true');
  });

  it('deshabilitado no ejecuta la acción', async () => {
    const alPulsar = vi.fn();
    render(
      <Boton disabled onClick={alPulsar}>
        Sin teléfono
      </Boton>,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Sin teléfono' }));
    expect(alPulsar).not.toHaveBeenCalled();
  });
});

describe('Insignias', () => {
  const casos: Array<[EstadoCita, string]> = [
    ['confirmada', 'Confirmada'],
    ['pendiente', 'Pendiente'],
    ['completada', 'Completada'],
    ['cancelada', 'Cancelada'],
    ['no_asistio', 'No asistió'],
  ];

  it.each(casos)('el estado %s siempre lleva su texto ("%s")', (estado, texto) => {
    render(<InsigniaEstadoCita estado={estado} />);
    expect(screen.getByText(texto)).toBeInTheDocument();
  });

  it('una alergia lleva ícono y texto', () => {
    render(
      <Insignia tono="rojo" icono="alerta">
        Penicilina
      </Insignia>,
    );
    expect(screen.getByText('Penicilina')).toBeInTheDocument();
  });
});

describe('Contador y Avatar', () => {
  it('el contador se lee con su descripción completa', () => {
    render(<Contador valor={5} descripcion="5 solicitudes por revisar" />);
    expect(screen.getByText(', 5 solicitudes por revisar')).toBeInTheDocument();
  });

  it('las iniciales salen del primer nombre y el primer apellido', () => {
    expect(iniciales('Laura Mendoza')).toBe('LM');
    expect(iniciales('  valeria  ')).toBe('V');
    expect(iniciales('Rosa Aguilar Pérez')).toBe('RA');
    const { container } = render(<Avatar nombre="Laura Mendoza" />);
    expect(container.firstChild).toHaveAttribute('aria-hidden', 'true');
  });
});

describe('Tarjetas', () => {
  it('la tarjeta con título es una región con nombre', () => {
    render(
      <Tarjeta titulo="Agenda de hoy">
        <p>Contenido</p>
      </Tarjeta>,
    );
    expect(screen.getByRole('region', { name: 'Agenda de hoy' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: 'Agenda de hoy' })).toBeInTheDocument();
  });

  it('el indicador muestra etiqueta, valor y detalle', () => {
    render(<TarjetaIndicador etiqueta="Citas de hoy" valor={5} icono="agenda" detalle="de 6 horarios del día" />);
    const region = screen.getByRole('region', { name: 'Citas de hoy' });
    expect(region).toHaveTextContent('5');
    expect(region).toHaveTextContent('de 6 horarios del día');
  });
});

describe('Campos', () => {
  it('la etiqueta nombra el campo y el error se asocia y lo marca inválido', () => {
    render(<CampoTexto etiqueta="Correo electrónico" type="email" error="Escribe un correo válido." />);
    const campo = screen.getByLabelText('Correo electrónico');
    expect(campo).toHaveAttribute('aria-invalid', 'true');
    expect(campo).toHaveAccessibleDescription('Escribe un correo válido.');
  });

  it('la ayuda se asocia como descripción', () => {
    render(<CampoTexto etiqueta="Teléfono" ayuda="Diez dígitos." />);
    expect(screen.getByLabelText('Teléfono')).toHaveAccessibleDescription('Diez dígitos.');
  });

  it('la contraseña se puede mostrar y ocultar con el botón', async () => {
    render(<CampoContrasena etiqueta="Contraseña" />);
    const campo = screen.getByLabelText('Contraseña');
    expect(campo).toHaveAttribute('type', 'password');
    await userEvent.click(screen.getByRole('button', { name: 'Mostrar contraseña' }));
    expect(campo).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: 'Ocultar contraseña' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('selección, área de texto y búsqueda tienen nombre', () => {
    render(
      <>
        <CampoSeleccion etiqueta="Dentista">
          <option>Dra. Ana Torres</option>
        </CampoSeleccion>
        <CampoAreaTexto etiqueta="Nota" />
        <Busqueda etiqueta="Buscar" />
      </>,
    );
    expect(screen.getByLabelText('Dentista')).toBeInTheDocument();
    expect(screen.getByLabelText('Nota')).toBeInTheDocument();
    expect(screen.getByRole('searchbox', { name: 'Buscar' })).toBeInTheDocument();
  });
});

describe('Aviso', () => {
  it('fijo no se anuncia; tras una acción se anuncia (status), y los errores como alert', () => {
    const { rerender } = render(<Aviso tono="info" titulo="Información" />);
    expect(screen.queryByRole('status')).toBeNull();
    rerender(<Aviso tono="exito" titulo="Cita confirmada" anunciar />);
    expect(screen.getByRole('status')).toHaveTextContent('Cita confirmada');
    rerender(<Aviso tono="error" titulo="No se pudo guardar" anunciar />);
    expect(screen.getByRole('alert')).toHaveTextContent('No se pudo guardar');
  });

  it('informativos y de éxito se pueden cerrar', async () => {
    const alCerrar = vi.fn();
    const { rerender } = render(<Aviso tono="info" titulo="Información" alCerrar={alCerrar} />);
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar aviso' }));
    rerender(<Aviso tono="exito" alCerrar={alCerrar}>Cita confirmada.</Aviso>);
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar aviso' }));
    expect(alCerrar).toHaveBeenCalledTimes(2);
  });

  it('errores y advertencias se quedan hasta resolverse: sin "Cerrar aviso"', () => {
    // Los tipos ya lo impiden; la prueba cubre también el caso forzado desde JavaScript
    const forzado = { alCerrar: vi.fn() } as unknown as Record<string, never>;
    const { rerender } = render(<Aviso tono="advertencia" titulo="Horario bloqueado" {...forzado} />);
    expect(screen.queryByRole('button', { name: 'Cerrar aviso' })).toBeNull();
    rerender(<Aviso tono="error" titulo="No se pudo guardar" {...forzado} />);
    expect(screen.queryByRole('button', { name: 'Cerrar aviso' })).toBeNull();
  });

  it('las advertencias tras una acción se anuncian como alert; sin título es un solo mensaje', () => {
    render(
      <Aviso tono="advertencia" anunciar>
        Hay 2 solicitudes por vencer en menos de 2 horas.
      </Aviso>,
    );
    expect(screen.getByRole('alert')).toHaveTextContent('Hay 2 solicitudes por vencer en menos de 2 horas.');
  });
});

describe('accesibilidad (axe)', () => {
  it('los componentes base no tienen violaciones', async () => {
    const { container } = render(
      <main>
        <h1>Componentes</h1>
        <Boton variante="principal" icono="confirmar">
          Confirmar cita
        </Boton>
        <Boton variante="peligro" icono="cerrar">
          Rechazar solicitud
        </Boton>
        <Boton variante="texto" icono="siguiente">
          Ver agenda
        </Boton>
        <Boton disabled>Sin teléfono</Boton>
        <InsigniaEstadoCita estado="confirmada" />
        <Insignia tono="azul">Con cuenta</Insignia>
        <Contador valor={5} descripcion="5 solicitudes por revisar" />
        <Avatar nombre="Laura Mendoza" />
        <Tarjeta titulo="Agenda de hoy">
          <p>Sin citas.</p>
        </Tarjeta>
        <TarjetaIndicador etiqueta="Citas de hoy" valor={5} icono="agenda" />
        <CampoTexto etiqueta="Correo electrónico" type="email" error="Escribe un correo válido." />
        <CampoContrasena etiqueta="Contraseña" />
        <CampoSeleccion etiqueta="Dentista">
          <option>Dra. Ana Torres</option>
        </CampoSeleccion>
        <Aviso tono="error" titulo="No se pudo guardar" anunciar>
          Ese horario ya tiene una cita.
        </Aviso>
      </main>,
    );
    await sinViolacionesAxe(container);
  });
});
