import { expect, test, type Locator, type Page } from '@playwright/test';
import { ANCHOS, abrirDiseno, sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

// Panel nuevo: inicio de sesión contra Login.dc.html. Sin API: no se envía el formulario con datos reales.

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

test('sin sesión, una ruta del panel lleva al inicio de sesión', async ({ page }) => {
  await page.goto('/panel/agenda');
  await expect(page).toHaveURL(/\/panel\/iniciar-sesion$/);
});

// Sesión de ejemplo solo en el navegador de prueba: sin tokens ni API (las secciones aún no piden datos)
const USUARIO_EJEMPLO = { id: 1, nombre: 'Ana', apellido: 'Ejemplo', email: 'ana@ejemplo.test', rol: 'admin' };

for (const ancho of ANCHOS) {
  test.describe(`panel con sesión a ${ancho} px`, () => {
    test.use({ viewport: { width: ancho, height: 900 } });

    test.beforeEach(async ({ page }) => {
      await page.addInitScript((u) => localStorage.setItem('usuario', JSON.stringify(u)), USUARIO_EJEMPLO);
    });

    for (const ruta of ['/', '/mas', '/no-existe']) {
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
      await page.addInitScript((u) => localStorage.setItem('usuario', JSON.stringify(u)), { ...USUARIO_EJEMPLO, rol: 'asistente' });
      await page.goto('/panel/equipo');
      await expect(page.getByRole('heading', { level: 1, name: 'No tienes acceso a esta sección' })).toBeVisible();
      await expect(page.getByText('Tu rol no incluye Equipo y usuarios.', { exact: false })).toBeVisible();
      await sinViolacionesAxe(page);
      await sinDesbordeHorizontal(page);
      await expect(page).toHaveScreenshot(`panel-sin-permiso-${ancho}.png`, { fullPage: true });
    });

    test('próximamente: captura', async ({ page }) => {
      await page.addInitScript((u) => localStorage.setItem('usuario', JSON.stringify(u)), USUARIO_EJEMPLO);
      await page.goto('/panel/ofertas');
      await expect(page.getByRole('heading', { level: 1, name: 'Ofertas y contenido' })).toBeVisible();
      await sinViolacionesAxe(page);
      await expect(page).toHaveScreenshot(`panel-proximamente-${ancho}.png`, { fullPage: true });
    });

    test('inactividad: aviso a los 14 min, foco dentro y Esc lo cierra', async ({ page }) => {
      await page.clock.install();
      await page.addInitScript((u) => localStorage.setItem('usuario', JSON.stringify(u)), USUARIO_EJEMPLO);
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
