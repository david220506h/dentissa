import AxeBuilder from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';
import { readdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const AQUI = dirname(fileURLToPath(import.meta.url));
export const RAIZ = resolve(AQUI, '../..');
export const CARPETA_DISENO = resolve(RAIZ, 'docs/design/panel');
export const CARPETA_CAPTURAS = resolve(AQUI, 'informe-visual/capturas');

/** Anchos de la verificación visual (plan de la Fase 3a, sección 9). */
export const ANCHOS = [360, 768, 1440] as const;

/** Archivos de Nunito autoalojada (los mismos que usa el panel). */
function fuenteLocal(): string {
  const dir = resolve(RAIZ, 'node_modules/@fontsource/nunito/files');
  const caras = [400, 600, 700, 800].map((peso) => {
    const archivo = readdirSync(dir).find((f) => f === `nunito-latin-${peso}-normal.woff2`)!;
    return `@font-face{font-family:'Nunito';font-weight:${peso};font-style:normal;src:url('${pathToFileURL(resolve(dir, archivo)).href}') format('woff2');}`;
  });
  return caras.join('\n');
}

/**
 * Abre una pantalla del diseño sin salir a internet: bloquea Google Fonts e inyecta la Nunito local,
 * para que la comparación use la misma fuente que el panel.
 */
export async function abrirDiseno(page: Page, archivo: string): Promise<void> {
  await page.route(/fonts\.(googleapis|gstatic)\.com/, (ruta) => ruta.abort());
  await page.route(/support\.js$/, (ruta) => ruta.fulfill({ status: 200, contentType: 'text/javascript', body: '' }));
  await page.goto(pathToFileURL(resolve(CARPETA_DISENO, archivo)).href);
  await page.addStyleTag({ content: fuenteLocal() });
  await page.evaluate(() => document.fonts.ready);
}

/** La página no se desborda a lo ancho (solo las regiones desplazables propias pueden hacerlo). */
export async function sinDesbordeHorizontal(page: Page): Promise<void> {
  const { ancho, contenido } = await page.evaluate(() => ({
    ancho: window.innerWidth,
    contenido: document.documentElement.scrollWidth,
  }));
  expect(contenido, 'la página no debe tener desplazamiento horizontal').toBeLessThanOrEqual(ancho);
}

/** axe con las reglas AA de WCAG 2.2, incluido el contraste real en el navegador. */
export async function sinViolacionesAxe(page: Page): Promise<void> {
  // @axe-core/playwright trae playwright-core 1.65.1 y @playwright/test usa 1.65.0: la API es la misma,
  // pero TypeScript ve dos tipos Page distintos.
  const resultado = await new AxeBuilder({ page: page as unknown as ConstructorParameters<typeof AxeBuilder>[0]['page'] })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  const resumen = resultado.violations.map(
    (v) => `${v.id}: ${v.help} → ${v.nodes.map((n) => n.target.join(' ')).slice(0, 3).join(' | ')}`,
  );
  expect(resumen).toEqual([]);
}
