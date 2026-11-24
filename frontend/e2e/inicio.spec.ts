import { expect, test, type Locator, type Page } from '@playwright/test';
import { ANCHOS, abrirDiseno, sesionDeEjemplo, sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

// Inicio del panel con la API simulada (datos de ejemplo del propio diseño, no reales), frente a
// Inicio.dc.html (escritorio) y Movil.dc.html (celular). "Ahora": miércoles 18 nov 2026, 9:30 en la clínica.
const AHORA = new Date('2026-11-18T15:30:00Z');

const cita = (id: number, paciente: string, servicio: string, hora: string, inicio: string, fin: string, estado: string) => ({
  id,
  paciente,
  servicio,
  dentista: 'Dra. Ejemplo',
  fecha: '18/11/2026',
  hora,
  inicio,
  fin,
  estado,
});

const CITAS = [
  cita(1, 'Rosa Aguilar', 'Resina dental', '08:00', '2026-11-18T14:00:00Z', '2026-11-18T15:00:00Z', 'confirmada'),
  cita(2, 'Ernesto Vega', 'Extracción dental', '09:00', '2026-11-18T15:00:00Z', '2026-11-18T16:00:00Z', 'pendiente'),
  cita(3, 'Mario Delgado', 'Tratamiento de conductos', '12:00', '2026-11-18T18:00:00Z', '2026-11-18T20:00:00Z', 'confirmada'),
  cita(4, 'Sofía Rangel', 'Limpieza dental', '15:00', '2026-11-18T21:00:00Z', '2026-11-18T22:00:00Z', 'confirmada'),
  { ...cita(5, 'Roberto Salas', 'Limpieza dental', '15:00', '2026-11-19T21:00:00Z', '2026-11-19T22:00:00Z', 'pendiente'), fecha: '19/11/2026' },
];

const PROPIEDADES = [
  'font-size',
  'font-weight',
  'line-height',
  'color',
  'background-color',
  'border-top-width',
  'border-top-color',
  'border-radius',
  'box-shadow',
  'padding',
  'gap',
  'height',
] as const;
type Propiedad = (typeof PROPIEDADES)[number];

async function estilos(objetivo: Locator, ignorar: readonly Propiedad[] = []) {
  await objetivo.waitFor();
  const props = await objetivo.evaluate((el, lista) => {
    const cs = getComputedStyle(el);
    return Object.fromEntries(
      lista.map((p) => [p, p === 'height' ? `${Math.round(el.getBoundingClientRect().height * 100) / 100}px` : cs.getPropertyValue(p)]),
    );
  }, [...PROPIEDADES]);
  for (const p of ignorar) delete props[p];
  return props;
}

async function abrirInicio(page: Page, ancho: number, opciones: { rol?: string; citas?: unknown[] } = {}) {
  await page.clock.install({ time: AHORA });
  await page.setViewportSize({ width: ancho, height: 900 });
  await sesionDeEjemplo(page, { citas: CITAS, ...opciones });
  await page.goto('/panel/');
  await page.evaluate(() => document.fonts.ready);
}

// Cajas: el contenido difiere (más o menos filas), así que no se compara su alto
const CAJA: Propiedad[] = ['height', 'font-size', 'font-weight', 'line-height', 'color'];
// Texto: el alto depende de cuántas líneas ocupa en cada ancho
const TEXTO: Propiedad[] = ['height'];

type Caso = { nombre: string; diseno: (p: Page) => Locator; panel: (p: Page) => Locator; ignorar?: readonly Propiedad[] };

const filaDiseno = (p: Page, nombre: string) =>
  p.getByText(nombre, { exact: true }).first().locator('xpath=ancestor::div[contains(@style,"padding:12px 0")][1]');
const agendaPanel = (p: Page) => p.getByRole('region', { name: /Agenda de hoy|Hoy/ });
const filaPanel = (p: Page, nombre: string) => agendaPanel(p).getByRole('listitem').filter({ hasText: nombre });

const ESCRITORIO: Caso[] = [
  { nombre: 'título', diseno: (p) => p.locator('main h1'), panel: (p) => p.locator('main h1'), ignorar: TEXTO },
  { nombre: 'fecha', diseno: (p) => p.locator('main h1 + p'), panel: (p) => p.locator('main h1 + p'), ignorar: TEXTO },
  {
    nombre: 'indicador Citas de hoy',
    diseno: (p) => p.getByText('Citas de hoy', { exact: true }).locator('xpath=ancestor::section[1]'),
    panel: (p) => p.getByRole('region', { name: 'Citas de hoy' }),
    ignorar: CAJA,
  },
  {
    // El panel lo llama "Sin confirmar" y sin detalle (decisión); se compara la caja con la del diseño
    nombre: 'indicador Sin confirmar',
    diseno: (p) => p.getByText('Citas sin confirmar', { exact: true }).locator('xpath=ancestor::section[1]'),
    panel: (p) => p.getByRole('region', { name: 'Sin confirmar' }).first(),
    ignorar: CAJA,
  },
  {
    nombre: 'tarjeta Agenda de hoy',
    diseno: (p) => p.getByRole('heading', { name: 'Agenda de hoy' }).locator('xpath=ancestor::section[1]'),
    panel: (p) => p.getByRole('region', { name: 'Agenda de hoy' }),
    ignorar: CAJA,
  },
  {
    nombre: 'título Agenda de hoy',
    diseno: (p) => p.getByRole('heading', { name: 'Agenda de hoy' }),
    panel: (p) => p.getByRole('heading', { name: 'Agenda de hoy' }),
    ignorar: TEXTO,
  },
  { nombre: 'fila con separador', diseno: (p) => filaDiseno(p, 'Ernesto Vega'), panel: (p) => filaPanel(p, 'Ernesto Vega'), ignorar: CAJA },
  { nombre: 'primera fila (sin separador)', diseno: (p) => filaDiseno(p, 'Rosa Aguilar'), panel: (p) => filaPanel(p, 'Rosa Aguilar'), ignorar: CAJA },
  {
    nombre: 'hora',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').locator('> div').first(),
    panel: (p) => filaPanel(p, 'Ernesto Vega').locator('> span').first(),
    ignorar: TEXTO,
  },
  {
    nombre: 'hora de varios horarios',
    diseno: (p) => p.getByText('12:00 a 14:00', { exact: true }),
    panel: (p) => filaPanel(p, 'Mario Delgado').locator('> span').first(),
    ignorar: TEXTO,
  },
  {
    nombre: 'paciente',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').getByText('Ernesto Vega'),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByText('Ernesto Vega'),
    ignorar: TEXTO,
  },
  {
    nombre: 'servicio',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').getByText('Extracción dental'),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByText('Extracción dental'),
    ignorar: TEXTO,
  },
];

const filaMovil = (p: Page, nombre: string) =>
  p.getByText(nombre, { exact: true }).first().locator('xpath=ancestor::div[contains(@style,"padding:10px 0")][1]');

const CELULAR: Caso[] = [
  {
    nombre: 'saludo',
    diseno: (p) => p.getByRole('heading', { name: 'Buenos días, Laura' }),
    panel: (p) => p.locator('main h1'),
    ignorar: TEXTO,
  },
  {
    nombre: 'fecha',
    diseno: (p) => p.getByText('Miércoles 18 de noviembre', { exact: true }).first(),
    panel: (p) => p.locator('main h1 + p'),
    ignorar: TEXTO,
  },
  {
    nombre: 'indicador compacto (Sin confirmar; el diseño dice "Por llamar")',
    diseno: (p) => p.getByText('Por llamar', { exact: true }).first().locator('xpath=..'),
    panel: (p) => p.getByRole('region', { name: 'Sin confirmar' }).last(),
    ignorar: [...CAJA],
  },
  {
    nombre: 'número del indicador compacto',
    diseno: (p) => p.getByText('Por llamar', { exact: true }).first().locator('xpath=preceding-sibling::span[1]'),
    panel: (p) => p.getByRole('region', { name: 'Sin confirmar' }).last().locator('span').nth(1),
    ignorar: TEXTO,
  },
  {
    nombre: 'etiqueta del indicador compacto',
    diseno: (p) => p.getByText('Por llamar', { exact: true }).first(),
    panel: (p) => p.getByRole('region', { name: 'Sin confirmar' }).last().getByText('Sin confirmar'),
    ignorar: TEXTO,
  },
  {
    nombre: 'tarjeta Hoy',
    diseno: (p) => p.getByText('Hoy', { exact: true }).first().locator('xpath=ancestor::section[1]'),
    panel: (p) => p.getByRole('region', { name: 'Hoy' }),
    ignorar: CAJA,
  },
  { nombre: 'título Hoy', diseno: (p) => p.getByText('Hoy', { exact: true }).first(), panel: (p) => p.getByRole('heading', { name: 'Hoy' }), ignorar: TEXTO },
  // El diseño separa columnas con flex (gap 12); el panel usa una rejilla con la hora sobre el nombre sin espacio
  // entre filas y 12 px entre columnas: mismo resultado, distinto valor de "gap"
  { nombre: 'fila', diseno: (p) => filaMovil(p, 'Ernesto Vega'), panel: (p) => filaPanel(p, 'Ernesto Vega'), ignorar: [...CAJA, 'gap'] },
  {
    nombre: 'hora',
    diseno: (p) => filaMovil(p, 'Ernesto Vega').getByText('9:00', { exact: true }),
    panel: (p) => filaPanel(p, 'Ernesto Vega').locator('> span').first(),
    ignorar: TEXTO,
  },
  {
    nombre: 'paciente',
    diseno: (p) => filaMovil(p, 'Ernesto Vega').getByText('Ernesto Vega'),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByText('Ernesto Vega'),
    ignorar: TEXTO,
  },
];

test.describe('Inicio en escritorio frente a Inicio.dc.html', () => {
  for (const caso of ESCRITORIO) {
    test(caso.nombre, async ({ page, context }) => {
      const diseno = await context.newPage();
      await diseno.setViewportSize({ width: 1440, height: 900 });
      await abrirDiseno(diseno, 'Inicio.dc.html');
      const esperado = await estilos(caso.diseno(diseno), caso.ignorar);
      await abrirInicio(page, 1440);
      expect(await estilos(caso.panel(page), caso.ignorar)).toEqual(esperado);
    });
  }
});

test.describe('Inicio en celular frente a Movil.dc.html', () => {
  for (const caso of CELULAR) {
    test(caso.nombre, async ({ page, context }) => {
      const diseno = await context.newPage();
      await diseno.setViewportSize({ width: 1800, height: 960 });
      await abrirDiseno(diseno, 'Movil.dc.html');
      const esperado = await estilos(caso.diseno(diseno), caso.ignorar);
      await abrirInicio(page, 390);
      expect(await estilos(caso.panel(page), caso.ignorar)).toEqual(esperado);
    });
  }
});

for (const ancho of ANCHOS) {
  test.describe(`Inicio a ${ancho} px`, () => {
    test('recepción: accesible, sin desborde, texto ≥ 12.5 px y captura', async ({ page }) => {
      await abrirInicio(page, ancho);
      await expect(agendaPanel(page).getByRole('listitem')).toHaveCount(4);
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
      await expect(page).toHaveScreenshot(`inicio-recepcion-${ancho}.png`, { fullPage: true });
    });

    test('dentista: captura', async ({ page }) => {
      await abrirInicio(page, ancho, { rol: 'dentista', citas: CITAS.slice(0, 2) });
      await expect(agendaPanel(page).getByRole('listitem')).toHaveCount(2);
      await sinViolacionesAxe(page);
      await expect(page).toHaveScreenshot(`inicio-dentista-${ancho}.png`, { fullPage: true });
    });

    test('día sin citas: captura', async ({ page }) => {
      await abrirInicio(page, ancho, { citas: [] });
      await expect(page.getByRole('heading', { name: 'No hay citas este día' })).toBeVisible();
      await expect(page).toHaveScreenshot(`inicio-vacio-${ancho}.png`, { fullPage: true });
    });
  });
}

test('error de la API: mensaje de Estados y Reintentar', async ({ page }) => {
  await abrirInicio(page, 1440);
  await page.unrouteAll();
  await page.route('**/api/**', (ruta) => ruta.fulfill({ status: 500, json: {} }));
  await page.reload();
  await expect(page.getByRole('heading', { name: 'No pudimos cargar las citas' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Citas de hoy' })).toHaveCount(0);
  await sinViolacionesAxe(page);
  await expect(page).toHaveScreenshot('inicio-error-1440.png', { fullPage: true });
});
