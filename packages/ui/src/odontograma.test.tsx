import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Odontograma, type EstadoPieza } from './index';
import { sinViolacionesAxe } from './prueba/axe';

// Datos de ejemplo del diseño (Expediente.dc.html)
const PIEZAS: Partial<Record<number, EstadoPieza>> = {
  18: 'ausente', 16: 'restaurado', 11: 'corona', 21: 'corona', 24: 'restaurado', 26: 'restaurado', 28: 'ausente',
  48: 'ausente', 46: 'endodoncia', 35: 'caries', 36: 'caries', 38: 'ausente',
};

describe('Odontograma', () => {
  it('dibuja las 32 piezas con numeración internacional y su estado en el nombre', () => {
    render(<Odontograma piezas={PIEZAS} alSeleccionar={() => {}} />);
    expect(screen.getAllByRole('button')).toHaveLength(32);
    expect(screen.getByRole('button', { name: 'Diente 36, caries' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Diente 17, sano' })).toBeInTheDocument();
    const superiorDerecha = screen.getByRole('group', { name: 'Superior, derecha del paciente' });
    expect(within(superiorDerecha).getAllByRole('button').map((b) => b.textContent?.match(/\d+/)?.[0])).toEqual([
      '18', '17', '16', '15', '14', '13', '12', '11',
    ]);
  });

  it('cada pieza lleva su código además del color, y la leyenda los explica', () => {
    render(<Odontograma piezas={PIEZAS} alSeleccionar={() => {}} />);
    expect(screen.getByRole('button', { name: 'Diente 11, corona' })).toHaveTextContent('Co');
    const leyenda = screen.getByRole('list', { name: 'Códigos del odontograma' });
    expect(within(leyenda).getAllByRole('listitem').map((l) => l.textContent)).toEqual([
      '–Sano', 'CCaries', 'RRestaurado', 'EEndodoncia', 'CoCorona', 'AuAusente',
    ]);
  });

  it('la pieza seleccionada queda marcada y se puede elegir con teclado', async () => {
    const alSeleccionar = vi.fn();
    render(<Odontograma piezas={PIEZAS} seleccionada={36} alSeleccionar={alSeleccionar} />);
    expect(screen.getByRole('button', { name: 'Diente 36, caries' })).toHaveAttribute('aria-pressed', 'true');
    screen.getByRole('button', { name: 'Diente 46, endodoncia' }).focus();
    await userEvent.keyboard('{Enter}');
    expect(alSeleccionar).toHaveBeenCalledWith(46);
  });

  it('sin alSeleccionar es de solo lectura', () => {
    render(<Odontograma piezas={PIEZAS} />);
    expect(screen.queryAllByRole('button')).toHaveLength(0);
    expect(screen.getByText(', endodoncia')).toBeInTheDocument();
  });

  it('sin violaciones de accesibilidad', async () => {
    const { container } = render(<Odontograma piezas={PIEZAS} seleccionada={36} alSeleccionar={() => {}} />);
    await sinViolacionesAxe(container);
  });
});
