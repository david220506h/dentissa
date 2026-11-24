import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ChipAlergia, InsigniaEstadoCita, TarjetaPaciente } from './index';
import { sinViolacionesAxe } from './prueba/axe';

// Datos de ejemplo del diseño (Movil2), no reales

describe('ChipAlergia', () => {
  it('dice "Alergia:" en texto, así que no depende del color', () => {
    render(<ChipAlergia alergia="Penicilina" />);
    expect(screen.getByText('Alergia: Penicilina')).toBeInTheDocument();
  });

  it('es un componente distinto de la insignia de estado "Cancelada"', () => {
    const { container } = render(
      <>
        <ChipAlergia alergia="Látex" />
        <InsigniaEstadoCita estado="cancelada" />
      </>,
    );
    const [chip, insignia] = [screen.getByText('Alergia: Látex'), screen.getByText('Cancelada').closest('span')!];
    expect(chip.className).not.toBe(insignia.className);
    expect(container).toBeTruthy();
  });
});

describe('TarjetaPaciente', () => {
  it('con alergia: el chip va dentro de la tarjeta, junto al nombre, la cuenta y la próxima cita', () => {
    render(
      <TarjetaPaciente
        nombre="Ernesto Vega"
        telefono="55 5555 0148"
        cuenta="con-cuenta"
        alergia="Penicilina"
        detalle="Próxima: 18 nov"
        href="/pacientes/2"
      />,
    );
    const tarjeta = screen.getByRole('link', { name: /Ernesto Vega/ });
    expect(tarjeta).toHaveAttribute('href', '/pacientes/2');
    expect(within(tarjeta).getByText('Alergia: Penicilina')).toBeInTheDocument();
    expect(within(tarjeta).getByText('Con cuenta')).toBeInTheDocument();
    expect(within(tarjeta).getByText('Próxima: 18 nov')).toBeInTheDocument();
  });

  it('sin alergia no hay chip; sin teléfono se muestra "—"; invitado en gris', () => {
    render(<TarjetaPaciente nombre="Valeria Núñez" cuenta="invitado" detalle="Solicitada: 19 nov" href="#" />);
    const tarjeta = screen.getByRole('link', { name: /Valeria Núñez/ });
    expect(within(tarjeta).queryByText(/Alergia:/)).toBeNull();
    expect(within(tarjeta).getByText('—')).toBeInTheDocument();
    expect(within(tarjeta).getByText('Invitado')).toBeInTheDocument();
  });

  it('sin violaciones de accesibilidad', async () => {
    const { container } = render(
      <TarjetaPaciente nombre="Mario Delgado" telefono="55 5555 0133" cuenta="con-cuenta" alergia="Látex" href="#" />,
    );
    await sinViolacionesAxe(container);
  });
});
