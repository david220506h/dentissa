import { expect, test, type Locator, type Page } from '@playwright/test';
import { ANCHOS, abrirDiseno, sesionDeEjemplo, sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

// Panel nuevo: inicio de sesión contra Login.dc.html. Sin API: no se envía el formulario con datos reales.

// Reloj fijo para todo el archivo: Inicio muestra la fecha y el saludo, y las capturas no deben depender del día.
// Miércoles 18 nov 2026, 9:30 en la clínica.
test.beforeEach(async ({ page }) => {
  await page.clock.install({ time: new Date('2026-11-18T15:30:00Z') });
});

const PROPIEDADES = ['font-size', 'font-weight', 'color', 'background-color', 'border-radius', 'padding', 'line-height'] as const;

async function estilos(objetivo: Locator) {
  const props = await objetivo.evaluate((el, lista) => {
    const cs = getComputedStyle(el);
    return Object.fromEntries(lista.map((p) => [p, cs.getPropertyValue(p)]));
  }, [...PROPIEDADES]);
  const caja = await objetivo.boundingBox();
  return { ...props, alto: Math.round(caja?.height ?? 0) };
}

type Caso = {
  nombre: string;
  diseno: (p: Page) => Locator;
  panel: (p: Page) => Locator;
  /** Propiedades que no aplican al caso (con su razón en el caso). */
  ignorar?: string[];
};

const sin = (o: Record<string, unknown>, claves: string[] = []) =>
  Object.fromEntries(Object.entries(o).filter(([k]) => !claves.includes(k)));

const CASOS: Caso[] = [
  { nombre: 'título', diseno: (p) => p.locator('h1'), panel: (p) => p.locator('h1') },
  { nombre: 'lema', diseno: (p) => p.locator('h2'), panel: (p) => p.locator('h2') },
  { nombre: 'subtítulo', diseno: (p) => p.getByText('Entra con tu correo y contraseña.'), panel: (p) => p.getByText('Entra con tu correo y contraseña.') },
  { nombre: 'texto de acceso', diseno: (p) => p.getByText('Acceso exclusivo para el personal de la clínica.'), panel: (p) => p.getByText('Acceso exclusivo para el personal de la clínica.') },
  { nombre: 'nombre de la marca', diseno: (p) => p.getByText('Dentissa', { exact: true }), panel: (p) => p.getByText('Dentissa', { exact: true }) },
  { nombre: 'etiqueta', diseno: (p) => p.locator('label[for="correo"]'), panel: (p) => p.locator('label[for="correo"]') },
  { nombre: 'campo de correo', diseno: (p) => p.locator('#correo'), panel: (p) => p.locator('#correo') },
  { nombre: 'botón Entrar', diseno: (p) => p.getByRole('link', { name: 'Entrar' }), panel: (p) => p.getByRole('button', { name: 'Entrar' }),
    // Una sola línea centrada en 48 px: el interlineado interno no cambia nada visible (Boton ya se verificó contra Main)
    ignorar: ['line-height'] },
  { nombre: 'nota para pacientes', diseno: (p) => p.getByText('¿Eres paciente?'), panel: (p) => p.getByText('¿Eres paciente?') },
  {
    nombre: 'fondo de la presentación',
    diseno: (p) => p.locator('h2').locator('xpath=../..'),
    panel: (p) => p.locator('h2').locator('xpath=../..'),
    // Contenedor sin texto propio: solo cuentan el fondo, el relleno y el alto
    ignorar: ['font-size', 'font-weight', 'line-height'],
  },
];

test.describe('inicio de sesión contra Login.dc.html', () => {
  for (const caso of CASOS) {
    test(caso.nombre, async ({ page, browser }) => {
      await page.setViewportSize({ width: 1440, height: 900 });
      await abrirDiseno(page, 'Login.dc.html');
      const esperado = await estilos(caso.diseno(page).first());

      const panel = await browser.newPage({ viewport: { width: 1440, height: 900 } });
      await panel.goto('/panel/iniciar-sesion');
      await panel.evaluate(() => document.fonts.ready);
      const real = await estilos(caso.panel(panel).first());
      await panel.close();
      expect(sin(real, caso.ignorar)).toEqual(sin(esperado, caso.ignorar));
    });
  }

  test('sin "¿Olvidaste tu contraseña?" (no tiene backend todavía)', async ({ page }) => {
    await page.goto('/panel/iniciar-sesion');
    await expect(page.getByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.getByText('¿Olvidaste tu contraseña?')).toHaveCount(0);
  });
});

for (const ancho of ANCHOS) {
  test.describe(`inicio de sesión a ${ancho} px`, () => {
    test.use({ viewport: { width: ancho, height: 900 } });

    test('accesible, sin desborde, texto ≥ 12.5 px y captura', async ({ page }) => {
      await page.goto('/panel/iniciar-sesion');
      await page.evaluate(() => document.fonts.ready);
      await sinViolacionesAxe(page);
      await sinDesbordeHorizontal(page);
      const menor = await page.evaluate(() => {
        let min = Infinity;
        for (const el of document.querySelectorAll<HTMLElement>('body *')) {
          const tieneTexto = [...el.childNodes].some((n) => n.nodeType === 3 && n.textContent!.trim());
          if (tieneTexto && el.getClientRects().length) min = Math.min(min, parseFloat(getComputedStyle(el).fontSize));
        }
        return min;
      });
      expect(menor).toBeGreaterThanOrEqual(12.5);
      await expect(page).toHaveScreenshot(`login-${ancho}.png`, { fullPage: true });
    });

    test('errores de validación visibles y accesibles', async ({ page }) => {
      await page.goto('/panel/iniciar-sesion');
      await page.getByRole('button', { name: 'Entrar' }).click();
      await expect(page.getByLabel('Correo electrónico')).toBeFocused();
      await sinViolacionesAxe(page);
      await expect(page).toHaveScreenshot(`login-errores-${ancho}.png`, { fullPage: true });
    });
  });
}

test('el panel (/panel) sin sesión abre el inicio de sesión nuevo', async ({ page }) => {
  await page.goto('/panel/');
  await expect(page).toHaveURL(/\/iniciar-sesion$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Iniciar sesión' })).toBeVisible();
});

test('sin sesión, una ruta del panel lleva al inicio de sesión', async ({ page }) => {
  await page.goto('/panel/agenda');
  await expect(page).toHaveURL(/\/iniciar-sesion$/);
});


for (const ancho of ANCHOS) {
  test.describe(`panel con sesión a ${ancho} px`, () => {
    test.use({ viewport: { width: ancho, height: 900 } });

    test.beforeEach(async ({ page }) => {
      await sesionDeEjemplo(page);
    });

    for (const ruta of ['/', '/no-existe']) {
      test(`${ruta}: accesible, sin desborde y captura`, async ({ page }) => {
        await page.goto(`/panel${ruta}`);
        await expect(page.locator('h1')).toBeVisible();
        await page.evaluate(() => document.fonts.ready);
        await sinViolacionesAxe(page);
        await sinDesbordeHorizontal(page);
        const nombre = ruta === '/' ? 'inicio' : ruta.slice(1);
        await expect(page).toHaveScreenshot(`panel-${nombre}-${ancho}.png`, { fullPage: true });
      });
    }
  });
}

// Estados en el panel: "Sin permiso" (asistente en Equipo), "Próximamente" y el aviso de inactividad
for (const ancho of ANCHOS) {
  test.describe(`estados del panel a ${ancho} px`, () => {
    test.use({ viewport: { width: ancho, height: 900 } });

    test('sin permiso: accesible, sin desborde y captura', async ({ page }) => {
      await sesionDeEjemplo(page, { rol: 'asistente' });
      await page.goto('/panel/equipo');
      await expect(page.getByRole('heading', { level: 1, name: 'No tienes acceso a esta sección' })).toBeVisible();
      await expect(page.getByText('Tu rol no incluye Equipo y usuarios.', { exact: false })).toBeVisible();
      await sinViolacionesAxe(page);
      await sinDesbordeHorizontal(page);
      await expect(page).toHaveScreenshot(`panel-sin-permiso-${ancho}.png`, { fullPage: true });
    });

    test('próximamente: captura', async ({ page }) => {
      await sesionDeEjemplo(page);
      await page.goto('/panel/ofertas');
      await expect(page.getByRole('heading', { level: 1, name: 'Ofertas y contenido' })).toBeVisible();
      await sinViolacionesAxe(page);
      await expect(page).toHaveScreenshot(`panel-proximamente-${ancho}.png`, { fullPage: true });
    });

    test('inactividad: aviso a los 14 min, foco dentro y Esc lo cierra', async ({ page }) => {
      await sesionDeEjemplo(page);
      await page.goto('/panel/');
      await expect(page.locator('h1')).toBeVisible();
      await page.clock.fastForward('13:59');
      await expect(page.getByRole('dialog')).toHaveCount(0);
      await page.clock.fastForward('00:02');
      const dialogo = page.getByRole('dialog', { name: '¿Sigues ahí?' });
      await expect(dialogo).toBeVisible();
      await expect(dialogo).toContainText('cerraremos tu sesión en');
      await expect(dialogo).toBeFocused();
      await sinViolacionesAxe(page);
      await sinDesbordeHorizontal(page);
      await page.waitForTimeout(300);
      await expect(page).toHaveScreenshot(`panel-inactividad-${ancho}.png`, { mask: [dialogo.locator('b')] });
      await page.keyboard.press('Escape');
      await expect(page.getByRole('dialog')).toHaveCount(0);
    });
  });
}

// Hoja "Más" por rol (Movil2), solo con el menú inferior (menos de 1024 px)
for (const ancho of [360, 768] as const) {
  for (const rol of ['admin', 'asistente', 'dentista'] as const) {
    test(`hoja Más de ${rol} a ${ancho} px: accesible y captura`, async ({ page }) => {
      await page.setViewportSize({ width: ancho, height: 900 });
      await sesionDeEjemplo(page, { rol });
      await page.goto('/panel/');
      await page.getByRole('button', { name: 'Más' }).click();
      const hoja = page.getByRole('dialog', { name: 'Más opciones' });
      await expect(hoja).toBeVisible();
      await page.waitForTimeout(400);
      await sinViolacionesAxe(page);
      await sinDesbordeHorizontal(page);
      await expect(page).toHaveScreenshot(`panel-hoja-mas-${rol}-${ancho}.png`);
      await page.keyboard.press('Escape');
      await expect(hoja).toHaveCount(0);
      await expect(page.getByRole('button', { name: 'Más' })).toBeFocused();
    });
  }
}

test('a 1440 px no hay menú inferior ni botón Más: el menú lateral muestra todo', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await sesionDeEjemplo(page);
  await page.goto('/panel/');
  await expect(page.getByRole('button', { name: 'Más' })).toBeHidden();
  await expect(page.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Auditoría' })).toBeVisible();
});
