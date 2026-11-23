import axe from 'axe-core';
import { expect } from 'vitest';

/**
 * Revisa un fragmento con axe-core (reglas de WCAG 2.0/2.1/2.2 A y AA) y falla si hay violaciones.
 * En jsdom no hay diseño ni colores calculados: el contraste se verifica en Playwright con la página real.
 */
export async function sinViolacionesAxe(contenedor: Element): Promise<void> {
  const resultado = await axe.run(contenedor, {
    runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] },
    rules: { 'color-contrast': { enabled: false } },
  });
  const resumen = resultado.violations.map((v) => `${v.id}: ${v.help} (${v.nodes.length})`);
  expect(resumen).toEqual([]);
}
