import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { Boton, CargandoLista, Dialogo, MensajeEstado } from './index';
import { sinViolacionesAxe } from './prueba/axe';

function Prueba({ alCerrar = () => {} }: { alCerrar?: () => void }) {
  const [abierto, setAbierto] = useState(false);
  const cerrar = () => {
    alCerrar();
    setAbierto(false);
  };
  return (
    <>
      <button type="button">Antes</button>
      <Boton onClick={() => setAbierto(true)}>Abrir</Boton>
      <Dialogo
        abierto={abierto}
        alCerrar={cerrar}
        titulo="¿Sigues ahí?"
        icono="reloj"
        tamano="chico"
        acciones={
          <>
            <Boton variante="secundario">Cerrar sesión</Boton>
            <Boton onClick={cerrar}>Mantener sesión</Boton>
          </>
        }
      >
        <p>Por seguridad, cerraremos tu sesión en 60 segundos.</p>
      </Dialogo>
    </>
  );
}

describe('Dialogo', () => {
  it('modal con nombre y descripción; al abrir, el foco va al diálogo', async () => {
    render(<Prueba />);
    await userEvent.click(screen.getByRole('button', { name: 'Abrir' }));
    const dialogo = screen.getByRole('dialog', { name: '¿Sigues ahí?' });
    expect(dialogo).toHaveAccessibleDescription('Por seguridad, cerraremos tu sesión en 60 segundos.');
    await waitFor(() => expect(dialogo).toHaveFocus());
  });

  it('el foco queda atrapado: Tab recorre solo los controles del diálogo', async () => {
    render(<Prueba />);
    await userEvent.click(screen.getByRole('button', { name: 'Abrir' }));
    await waitFor(() => expect(screen.getByRole('dialog')).toHaveFocus());
    await userEvent.tab();
    expect(screen.getByRole('button', { name: 'Cerrar' })).toHaveFocus();
    await userEvent.tab();
    expect(screen.getByRole('button', { name: 'Cerrar sesión' })).toHaveFocus();
    await userEvent.tab();
    expect(screen.getByRole('button', { name: 'Mantener sesión' })).toHaveFocus();
    await userEvent.tab();
    expect(screen.getByRole('button', { name: 'Cerrar' })).toHaveFocus();
    await userEvent.tab({ shift: true });
    expect(screen.getByRole('button', { name: 'Mantener sesión' })).toHaveFocus();
  });

  it('Esc cierra y el foco vuelve al botón que lo abrió', async () => {
    const alCerrar = vi.fn();
    render(<Prueba alCerrar={alCerrar} />);
    const abrir = screen.getByRole('button', { name: 'Abrir' });
    await userEvent.click(abrir);
    await userEvent.keyboard('{Escape}');
    expect(alCerrar).toHaveBeenCalledOnce();
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
    // React Aria devuelve el foco un cuadro después de desmontar el diálogo
    await waitFor(() => expect(abrir).toHaveFocus());
  });

  it('"Cerrar" también cierra y devuelve el foco', async () => {
    render(<Prueba />);
    const abrir = screen.getByRole('button', { name: 'Abrir' });
    await userEvent.click(abrir);
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
    // React Aria devuelve el foco un cuadro después de desmontar el diálogo
    await waitFor(() => expect(abrir).toHaveFocus());
  });

  it('sin violaciones de accesibilidad', async () => {
    render(<Prueba />);
    await userEvent.click(screen.getByRole('button', { name: 'Abrir' }));
    await sinViolacionesAxe(document.body);
  });
});

describe('MensajeEstado y CargandoLista', () => {
  it('título con el nivel pedido, texto y acción', () => {
    render(
      <MensajeEstado
        nivel={1}
        icono="auditoria"
        titulo="No tienes acceso a esta sección"
        texto="Tu rol no incluye Equipo y usuarios."
        accion={<Boton variante="secundario">Volver al inicio</Boton>}
      />,
    );
    expect(screen.getByRole('heading', { level: 1, name: 'No tienes acceso a esta sección' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Volver al inicio' })).toBeInTheDocument();
  });

  it('el esqueleto se anuncia una vez con su etiqueta y sus bloques quedan ocultos', async () => {
    const { container } = render(<CargandoLista etiqueta="Cargando citas" filas={3} />);
    expect(screen.getByRole('status', { name: 'Cargando citas' })).toBeInTheDocument();
    expect(container.querySelectorAll('[aria-hidden="true"]')).toHaveLength(12);
    await sinViolacionesAxe(container);
  });
});
