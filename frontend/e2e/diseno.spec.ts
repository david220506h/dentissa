// Capturas del diseño aprobado (sin red: Nunito local) para el informe visual lado a lado.
// Las pantallas del diseño son tableros de 1440 px; a 360 y 768 se usa el tablero de celular (Movil).
import { test } from '@playwright/test';
import { mkdirSync, readdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { ANCHOS, CARPETA_CAPTURAS, abrirDiseno } from './utilidades';

mkdirSync(CARPETA_CAPTURAS, { recursive: true });

const TABLEROS: Array<{ archivo: string; ancho: number }> = [
  { archivo: 'Main.dc.html', ancho: 1280 },
  { archivo: 'Login.dc.html', ancho: 1440 },
  { archivo: 'Inicio.dc.html', ancho: 1440 },
  { archivo: 'Movil.dc.html', ancho: 1800 },
];

for (const { archivo, ancho } of TABLEROS) {
  test(`captura del diseño: ${archivo}`, async ({ page }) => {
    await page.setViewportSize({ width: ancho, height: 900 });
    await abrirDiseno(page, archivo);
    await page.screenshot({ path: resolve(CARPETA_CAPTURAS, `diseno-${archivo.replace('.dc.html', '')}.png`), fullPage: true });
  });
}

test.afterAll(() => {
  // Informe local: diseño y catálogo lado a lado
  const capturas = readdirSync(CARPETA_CAPTURAS);
  const img = (n: string) => (capturas.includes(n) ? `<img src="capturas/${n}" alt="${n}" loading="lazy">` : '<p>(sin captura)</p>');
  const filas = ANCHOS.map(
    (a) => `<section><h2>${a} px</h2><div class="par"><figure><figcaption>Diseño (${a < 1024 ? 'Movil' : 'Main'})</figcaption>${img(
      a < 1024 ? 'diseno-Movil.png' : 'diseno-Main.png',
    )}</figure><figure><figcaption>Catálogo a ${a} px</figcaption>${img(`catalogo-${a}.png`)}</figure></div></section>`,
  ).join('\n');
  writeFileSync(
    resolve(CARPETA_CAPTURAS, '../index.html'),
    `<!doctype html><html lang="es-MX"><meta charset="utf-8"><title>Informe visual · Dentissa</title>
<style>body{font-family:system-ui;margin:24px;background:#f5f5f5;color:#333}.par{display:grid;grid-template-columns:1fr 1fr;gap:16px;align-items:start}
figure{margin:0;background:#fff;border:1px solid #eee;padding:8px;overflow:auto;max-height:80vh}img{max-width:100%;height:auto;display:block}</style>
<h1>Informe visual (local, solo desarrollo)</h1><p>Generado por Playwright. Los estilos de cada componente se comparan además en e2e/estilos-contra-diseno.spec.ts.</p>
${filas}</html>`,
  );
});
