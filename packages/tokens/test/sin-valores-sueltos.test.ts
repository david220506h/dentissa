// @vitest-environment node
// El código nuevo no usa colores, tamaños de letra ni radios literales: todo sale de tokens.css.
// Tampoco usa `transition: all` ni `ease-in` (especificación de movimiento aprobada).
import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const RAIZ = resolve(__dirname, '../../..');
// Carpetas del código nuevo. El código anterior (frontend/src/Layouts, components, Sesiones…)
// se retira o se migra al sitio y no se revisa aquí.
const CARPETAS = [
  'packages/ui/src',
  'frontend/src/app',
  'frontend/src/api',
  'frontend/src/features',
  'frontend/src/catalogo',
  'frontend/src/pwa',
];

function archivos(dir: string): string[] {
  if (!existsSync(dir)) return [];
  return readdirSync(dir).flatMap((n) => {
    const ruta = join(dir, n);
    return statSync(ruta).isDirectory() ? archivos(ruta) : [ruta];
  });
}

const todos = CARPETAS.flatMap((c) => archivos(resolve(RAIZ, c)));
const css = todos.filter((f) => f.endsWith('.css'));
const codigo = todos.filter((f) => /\.tsx?$/.test(f) && !f.includes('.test.'));

const buscar = (texto: string, re: RegExp) => texto.match(re) ?? [];

describe('sin valores sueltos en el código nuevo', () => {
  it('revisa al menos el paquete de componentes', () => {
    expect(existsSync(resolve(RAIZ, 'packages/ui/src'))).toBe(true);
  });

  for (const ruta of css) {
    it(`${relative(RAIZ, ruta)}`, () => {
      const contenido = readFileSync(ruta, 'utf8').replace(/\/\*[\s\S]*?\*\//g, '');
      expect(buscar(contenido, /#[0-9a-f]{3,8}\b|rgba?\(|hsla?\(/gi), 'colores literales').toEqual([]);
      expect(buscar(contenido, /font-size:\s*[\d.]+(px|rem|em)/g), 'tamaños de letra literales').toEqual([]);
      expect(buscar(contenido, /border-radius:\s*[\d.]+px/g), 'radios literales').toEqual([]);
      expect(buscar(contenido, /transition:\s*all\b/g), 'transition: all').toEqual([]);
      expect(buscar(contenido, /\bease-in\b(?!-out)/g), 'ease-in').toEqual([]);
    });
  }

  for (const ruta of codigo) {
    it(`${relative(RAIZ, ruta)} sin colores en línea`, () => {
      expect(buscar(readFileSync(ruta, 'utf8'), /['"`]#[0-9a-f]{3,8}['"`]/gi)).toEqual([]);
    });
  }
});
