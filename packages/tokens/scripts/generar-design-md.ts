/*
  Genera DESIGN.md (raíz del repositorio) a partir de packages/tokens/src/tokens.css.

  DESIGN.md es un archivo DERIVADO para herramientas de diseño (Impeccable): no es una fuente de
  verdad. La autoridad es docs/design/panel/*.dc.html y su implementación es tokens.css. Una prueba
  comprueba que DESIGN.md coincide con lo que genera este script; no se edita a mano.

  Uso: node packages/tokens/scripts/generar-design-md.ts
*/
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export type Tokens = Map<string, string>;

/** Lee las variables CSS de :root (nombre sin "--" → valor tal cual). */
export function leerTokens(css: string): Tokens {
  const sinComentarios = css.replace(/\/\*[\s\S]*?\*\//g, '');
  const tokens: Tokens = new Map();
  for (const m of sinComentarios.matchAll(/--([a-z0-9-]+)\s*:\s*([^;]+);/g)) {
    tokens.set(m[1]!, m[2]!.replace(/\s+/g, ' ').trim());
  }
  return tokens;
}

/** Resuelve var(--x) hasta un valor literal. */
export function resolver(tokens: Tokens, valor: string): string {
  return valor.replace(/var\(--([a-z0-9-]+)\)/g, (_, nombre: string) => {
    const v = tokens.get(nombre);
    if (v === undefined) throw new Error(`Token inexistente: --${nombre}`);
    return resolver(tokens, v);
  });
}

const esColor = (v: string) => /^#[0-9a-f]{6}$/i.test(v) || /^rgba?\(/i.test(v);

// Usos de la tabla "Tipografía" de Main.dc.html: [clave, token de tamaño, token de peso]
const USOS_TIPOGRAFIA: ReadonlyArray<readonly [string, string, string]> = [
  ['titulo-pagina', 'texto-titulo-pagina', 'peso-extranegrita'],
  ['titulo-tarjeta', 'texto-titulo-tarjeta', 'peso-negrita'],
  ['cuerpo', 'texto-cuerpo', 'peso-normal'],
  ['etiqueta', 'texto-etiqueta', 'peso-negrita'],
  ['tabla', 'texto-tabla', 'peso-normal'],
  ['detalle', 'texto-detalle', 'peso-seminegrita'],
  ['insignia', 'texto-insignia', 'peso-negrita'],
  ['boton', 'texto-boton', 'peso-negrita'],
];

export function generarDesignMd(tokensCss: string): string {
  const t = leerTokens(tokensCss);
  const r = (nombre: string) => resolver(t, `var(--${nombre})`);
  const lineas: string[] = ['---', 'name: "Dentissa — Panel de la clínica"',
    'description: "Panel administrativo de una clínica dental. DERIVADO de packages/tokens/src/tokens.css; la autoridad es docs/design/panel/. No editar a mano."'];

  lineas.push('colors:');
  for (const [nombre, valor] of t) {
    if (esColor(valor)) lineas.push(`  ${nombre}: "${valor.toLowerCase()}"`);
  }

  const familia = r('fuente');
  lineas.push('typography:');
  for (const [clave, tamano, peso] of USOS_TIPOGRAFIA) {
    lineas.push(`  ${clave}:`, `    fontFamily: "${familia.replace(/'/g, '')}"`,
      `    fontSize: "${r(tamano)}"`, `    fontWeight: ${r(peso)}`);
  }

  lineas.push('rounded:');
  for (const [nombre, valor] of t) if (nombre.startsWith('radio-')) lineas.push(`  ${nombre.slice(6)}: "${valor}"`);

  lineas.push('spacing:');
  for (const [nombre, valor] of t) if (nombre.startsWith('espacio-')) lineas.push(`  ${nombre.slice(8)}: "${valor}"`);

  lineas.push(
    'components:',
    '  button-primary:', '    backgroundColor: "{colors.rosa-700}"', '    textColor: "{colors.superficie}"',
    '    typography: "{typography.boton}"', '    rounded: "{rounded.10}"', `    height: "${r('alto-toque')}"`, '    padding: "0 18px"',
    '  button-secondary:', '    backgroundColor: "{colors.superficie}"', '    textColor: "{colors.texto}"',
    '    typography: "{typography.boton}"', '    rounded: "{rounded.10}"', `    height: "${r('alto-toque')}"`,
    '  button-danger:', '    backgroundColor: "{colors.superficie}"', '    textColor: "{colors.estado-cancelada-texto}"',
    '    typography: "{typography.boton}"', '    rounded: "{rounded.10}"', `    height: "${r('alto-toque')}"`,
    '  button-text:', '    textColor: "{colors.rosa-800}"', '    typography: "{typography.boton}"', `    height: "${r('alto-toque')}"`,
    '  input:', '    backgroundColor: "{colors.superficie}"', '    textColor: "{colors.texto}"', '    rounded: "{rounded.10}"',
    `    height: "${r('alto-toque')}"`, '    padding: "0 14px"',
    '  badge:', '    typography: "{typography.insignia}"', '    rounded: "{rounded.pildora}"', `    height: "${r('alto-insignia')}"`, '    padding: "0 10px"',
    '  nav-item-active:', '    backgroundColor: "{colors.rosa-50}"', '    textColor: "{colors.rosa-800}"', '    rounded: "{rounded.10}"',
    `    height: "${r('alto-toque')}"`,
    '  card:', '    backgroundColor: "{colors.superficie}"', '    rounded: "{rounded.14}"', '    padding: "20px"',
    '---',
    '',
    '# Design System: Dentissa — Panel de la clínica',
    '',
    '> **Archivo derivado.** Lo genera `packages/tokens/scripts/generar-design-md.ts` a partir de `packages/tokens/src/tokens.css`. La **autoridad visual** es el diseño aprobado en `docs/design/panel/*.dc.html`: `Main.dc.html` es el sistema visual y la matriz de permisos; `Movil.dc.html`, la versión celular. Si algo aquí difiere del diseño, gana el diseño.',
    '',
    '## Overview',
    '',
    'Panel de trabajo diario (modo *Operate*) para el personal de una clínica dental: administración, asistente y dentista. Claridad y rapidez antes que efecto. Modo claro; una sola acción principal por pantalla, en Rosa 700.',
    '',
    '## Colors',
    '',
    'Paleta de la clínica: un solo rosa de marca (Rosa 500 para acentos; Rosa 700 en botones para que el texto blanco se lea), neutros y cinco parejas de estado de la cita. Cada valor y su contraste están documentados en `Main.dc.html` (sección Color).',
    '',
    '### Named Rules',
    '',
    '- El estado nunca depende solo del color: toda insignia lleva texto y los descansos de la agenda van rayados.',
    '- Texto terciario (`texto-terciario`) solo sobre blanco.',
    '',
    '## Typography',
    '',
    'Nunito 400, 600, 700 y 800, autoalojada. Nada por debajo de 12.5 px. Escala y usos en `Main.dc.html` (sección Tipografía).',
    '',
    '## Layout',
    '',
    'Menú lateral desde 1024 px; menú inferior de cinco entradas por debajo (`Movil.dc.html`). Desde 360 px, sin desplazamiento horizontal de la página.',
    '',
    '## Elevation & Depth',
    '',
    'Una sola sombra suave en tarjetas (`0 1px 2px rgba(0,0,0,0.04)`) más bordes de 1 px.',
    '',
    '## Shapes',
    '',
    'Radios de 10 px (controles), 12 px y 14 px (tarjetas); insignias en píldora.',
    '',
    '## Components',
    '',
    'Botones, insignias de estado, campos, avisos y elementos del menú según `Main.dc.html` (sección Componentes). Alto mínimo de 44 px en todo lo que se toca.',
    '',
    "## Do's and Don'ts",
    '',
    '- Usar solo los colores y tamaños de este sistema; nunca valores sueltos.',
    '- No mostrar controles sin backend ni cifras inventadas.',
    '- Movimiento sobrio: lo frecuente es instantáneo (especificación en `docs/frontend/plan-fase-3a.md`, sección 4).',
    '',
  );
  return lineas.join('\n');
}

const RAIZ = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
export const RUTA_TOKENS = resolve(RAIZ, 'packages/tokens/src/tokens.css');
export const RUTA_DESIGN_MD = resolve(RAIZ, 'DESIGN.md');

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  writeFileSync(RUTA_DESIGN_MD, generarDesignMd(readFileSync(RUTA_TOKENS, 'utf8')));
  console.log(`DESIGN.md generado en ${RUTA_DESIGN_MD}`);
}
