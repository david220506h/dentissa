// @vitest-environment node
// DESIGN.md es derivado: debe coincidir con lo que genera el script (no se edita a mano).
import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { RUTA_DESIGN_MD, RUTA_TOKENS, generarDesignMd } from '../scripts/generar-design-md.ts';

describe('DESIGN.md', () => {
  it('está generado desde tokens.css (si falla: node packages/tokens/scripts/generar-design-md.ts)', () => {
    const esperado = generarDesignMd(readFileSync(RUTA_TOKENS, 'utf8'));
    expect(readFileSync(RUTA_DESIGN_MD, 'utf8').replace(/\r\n/g, '\n')).toBe(esperado);
  });
});
