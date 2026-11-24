import { expect, test, type Page } from '@playwright/test';
import { sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

// Sitio público con la API simulada (datos inventados). Reglas: AA, sin desborde a 360 px, texto ≥ 12.5 px y toques de 44 px.
async function simularApi(page: Page) {
  // axe mide el estado final: sin las animaciones de entrada (opacidad intermedia) ni el revelado al desplazarse
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.route('**/api/publico/**', (ruta) => {
    const url = new URL(ruta.request().url());
    if (url.pathname.endsWith('/servicios'))
      return ruta.fulfill({ json: [{ id: 1, nombre: 'Limpieza dental', precio: 600, duracionSlots: 1 }, { id: 6, nombre: 'Tratamiento de conductos', precio: 3500, duracionSlots: 2 }] });
    if (url.pathname.endsWith('/disponibilidad')) return ruta.fulfill({ json: [{ fecha: url.searchParams.get('desde'), horas: ['12:00', '13:00'] }] });
    return ruta.fulfill({ status: 201, json: { mensaje: 'Recibimos tu solicitud.' } });
  });
}

async function reglas(page: Page) {
  await page.evaluate(() => document.fonts.ready);
  await sinViolacionesAxe(page);
  await sinDesbordeHorizontal(page);
  const { menor, pequenos } = await page.evaluate(() => {
    let min = Infinity;
    for (const el of document.querySelectorAll<HTMLElement>('body *')) {
      const tieneTexto = [...el.childNodes].some((n) => n.nodeType === 3 && n.textContent!.trim());
      if (tieneTexto && el.getClientRects().length) min = Math.min(min, parseFloat(getComputedStyle(el).fontSize));
    }
    const chicos: string[] = [];
    for (const el of document.querySelectorAll<HTMLElement>('a, button, input:not([type=checkbox]), select, summary')) {
      const r = el.getBoundingClientRect();
      if (r.width && r.height && r.height < 44 && !el.closest('[aria-hidden=true]')) chicos.push(`${el.tagName} "${el.textContent?.trim()}" ${r.height}px`);
    }
    return { menor: min, pequenos: chicos };
  });
  expect(menor).toBeGreaterThanOrEqual(12.5);
  expect(pequenos).toEqual([]);
}

for (const ancho of [360, 1440]) {
  test(`sitio público a ${ancho} px: inicio y solicitud`, async ({ page }) => {
    await page.setViewportSize({ width: ancho, height: 900 });
    await simularApi(page);
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Limpieza dental' })).toBeVisible();
    await reglas(page);

    await page.getByRole('link', { name: 'Solicitar cita' }).first().click();
    await expect(page).toHaveURL(/\/solicitar-cita$/);
    await page.getByLabel('Servicio').selectOption({ label: 'Limpieza dental' });
    const fecha = await page.getByLabel('Fecha').getAttribute('min');
    await page.getByLabel('Fecha').fill(fecha!);
    await page.getByLabel('Horario libre').selectOption('12:00');
    await page.getByLabel('Nombre').fill('Ana');
    await page.getByLabel('Apellido').fill('Ejemplo');
    await page.getByLabel('Teléfono').fill('55 1234 5678');
    await page.getByLabel('Correo').fill('ana@ejemplo.test');
    await page.getByLabel('Leí y acepto el aviso de privacidad').check();
    await reglas(page);
    await page.getByRole('button', { name: 'Enviar solicitud' }).click();
    await expect(page.getByRole('heading', { name: 'Recibimos tu solicitud' })).toBeVisible();
    await reglas(page);
  });
}

test('el enlace del personal abre el panel en /panel', async ({ page }) => {
  await simularApi(page);
  await page.goto('/');
  await page.getByRole('link', { name: 'Acceso del personal' }).first().click();
  await expect(page).toHaveURL(/\/panel\/iniciar-sesion$/);
});
