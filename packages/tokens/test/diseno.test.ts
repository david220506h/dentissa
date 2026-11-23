// @vitest-environment node
// Los tokens y el diseño aprobado no se pueden separar: si el arquitecto cambia un color o un
// tamaño en docs/design/panel/, esta prueba falla hasta que tokens.css se actualice.
import { readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { leerTokens, resolver } from '../scripts/generar-design-md.ts';
import { TEXTO_MINIMO_PX } from '../src/index.ts';

const RAIZ = resolve(__dirname, '../../..');
const CARPETA_DISENO = resolve(RAIZ, 'docs/design/panel');
const tokens = leerTokens(readFileSync(resolve(__dirname, '../src/tokens.css'), 'utf8'));

const archivosDiseno = readdirSync(CARPETA_DISENO).filter((f) => f.endsWith('.dc.html'));
const html = archivosDiseno.map((f) => readFileSync(resolve(CARPETA_DISENO, f), 'utf8'));
// Solo los estilos: los colores mencionados en el texto (p. ej., los que "se retiran") no cuentan
const estilos = html.flatMap((h) => [...h.matchAll(/style="([^"]*)"/g)].map((m) => m[1]!)).join(';');

const RE_COLOR = /#[0-9a-f]{6}\b|rgba?\([^)]*\)/gi;
const normalizar = (c: string) => c.toLowerCase().replace(/\s+/g, '');
const coloresDiseno = new Set([...estilos.matchAll(RE_COLOR)].map((m) => normalizar(m[0])));
const valoresLiterales = [...tokens.values()].filter((v) => !v.includes('var('));
const coloresTokens = new Set(valoresLiterales.flatMap((v) => [...v.matchAll(RE_COLOR)].map((m) => normalizar(m[0]))));
const px = (lista: Iterable<string | number>) => new Set([...lista].map((v) => Number.parseFloat(String(v))));

describe('tokens.css frente al diseño aprobado', () => {
  it('lee los 13 archivos del diseño', () => {
    expect(archivosDiseno).toHaveLength(13);
  });

  it('no inventa colores: cada color de los tokens aparece en el diseño', () => {
    expect([...coloresTokens].filter((c) => !coloresDiseno.has(c))).toEqual([]);
  });

  it('cubre el diseño: cada color usado en el diseño tiene token', () => {
    expect([...coloresDiseno].filter((c) => !coloresTokens.has(c))).toEqual([]);
  });

  it('la paleta y los estados de Main tienen el valor exacto del diseño', () => {
    const main = readFileSync(resolve(CARPETA_DISENO, 'Main.dc.html'), 'utf8');
    const muestras = [...main.matchAll(/>(#[0-9a-f]{6})</gi)].map((m) => m[1]!.toLowerCase());
    expect(muestras.length).toBeGreaterThanOrEqual(13);
    for (const c of muestras) expect(coloresTokens).toContain(c);
    const pares: Array<[string, string, string]> = [
      ['confirmada', '#14532d', '#dcfce7'],
      ['pendiente', '#854d0e', '#fef9c3'],
      ['completada', '#1e40af', '#dbeafe'],
      ['cancelada', '#991b1b', '#fee2e2'],
      ['no-asistio', '#374151', '#f3f4f6'],
    ];
    for (const [estado, texto, fondo] of pares) {
      expect(main).toContain(`Texto ${texto} sobre ${fondo}`);
      expect(tokens.get(`estado-${estado}-texto`)).toBe(texto);
      expect(tokens.get(`estado-${estado}-fondo`)).toBe(fondo);
    }
  });

  it('tamaños de letra: todos del diseño y ninguno menor a 12.5 px', () => {
    const tamanosDiseno = px([...estilos.matchAll(/font-size:\s*([\d.]+)px/g)].map((m) => m[1]!));
    const tamanosTokens = px(
      [...tokens].filter(([n, v]) => /^texto-[\d-]+$/.test(n) && !v.includes('var(')).map(([, v]) => v),
    );
    for (const t of tamanosTokens) {
      expect(t).toBeGreaterThanOrEqual(TEXTO_MINIMO_PX);
      expect(tamanosDiseno.has(t), `${t}px no aparece en el diseño`).toBe(true);
    }
    // Lo dibujado en 12 px sube a 12.5 px (decisión 3); 34 px es solo el título del documento Main.
    const sinToken = [...tamanosDiseno].filter((t) => t >= TEXTO_MINIMO_PX && t !== 34 && !tamanosTokens.has(t));
    expect(sinToken).toEqual([]);
  });

  it('los usos de la tabla de tipografía de Main tienen su tamaño', () => {
    const usos: Array<[string, number]> = [
      ['texto-titulo-pagina', 26],
      ['texto-titulo-tarjeta', 18],
      ['texto-cuerpo', 15],
      ['texto-etiqueta', 14],
      ['texto-tabla', 14.5],
      ['texto-detalle', 13],
      ['texto-insignia', 12.5],
    ];
    for (const [token, tamano] of usos) {
      expect(Number.parseFloat(resolver(tokens, `var(--${token})`))).toBe(tamano);
    }
  });

  it('radios: todos aparecen en el diseño', () => {
    const radiosDiseno = new Set(
      [...estilos.matchAll(/border-radius:\s*([^;]+)/g)].flatMap((m) => m[1]!.trim().split(/\s+/)),
    );
    for (const [nombre, valor] of tokens) {
      if (nombre.startsWith('radio-')) expect(radiosDiseno.has(valor), `${nombre}: ${valor}`).toBe(true);
    }
  });
});
