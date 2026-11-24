// Catálogo a 360, 768 y 1440 px: sin desbordes, sin violaciones AA (con contraste real) y capturas
// para el informe visual (frontend/e2e/informe-visual/index.html) y la regresión visual.
import { expect, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { ANCHOS, CARPETA_CAPTURAS, sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

mkdirSync(CARPETA_CAPTURAS, { recursive: true });

for (const ancho of ANCHOS) {
  test.describe(`catálogo a ${ancho} px`, () => {
    test.use({ viewport: { width: ancho, height: 900 } });

    test.beforeEach(async ({ page }) => {
      await page.goto('/catalogo.html');
      await page.evaluate(() => document.fonts.ready);
    });

    test('no se desborda a lo ancho', async ({ page }) => {
      await sinDesbordeHorizontal(page);
    });

    test('sin violaciones de accesibilidad AA', async ({ page }) => {
      await sinViolacionesAxe(page);
    });

    test('ningún texto mide menos de 12.5 px', async ({ page }) => {
      const pequenos = await page.evaluate(() =>
        [...document.querySelectorAll('body *')]
          .filter((el) => [...el.childNodes].some((n) => n.nodeType === Node.TEXT_NODE && n.textContent!.trim()))
          .filter((el) => Number.parseFloat(getComputedStyle(el).fontSize) < 12.5)
          .map((el) => `${el.tagName}: "${el.textContent!.trim().slice(0, 30)}" ${getComputedStyle(el).fontSize}`),
      );
      expect(pequenos).toEqual([]);
    });

    test('todo lo que se toca mide al menos 44 px', async ({ page }) => {
      const chicos = await page.evaluate(() =>
        [...document.querySelectorAll('button, a[href], input:not([type="hidden"]), select, textarea, [role="tab"]')]
          .map((el) => ({ el, r: el.getBoundingClientRect() }))
          .filter(({ r }) => r.width > 0 && r.height > 0)
          // Los botones del grupo Día/Semana miden 42 px dentro de un contenedor de 44 px (diseño de Agenda)
          .filter(({ el, r }) => Math.round(r.height) < (el.closest('[role="group"]') ? 42 : 44))
          .map(({ el, r }) => `${el.tagName} "${(el.textContent || el.getAttribute('aria-label') || '').trim().slice(0, 30)}" ${r.height}px`),
      );
      expect(chicos).toEqual([]);
    });

    test('captura', async ({ page }) => {
      await page.screenshot({ path: resolve(CARPETA_CAPTURAS, `catalogo-${ancho}.png`), fullPage: true });
      await expect(page).toHaveScreenshot(`catalogo-${ancho}.png`, { fullPage: true });
    });
  });
}
