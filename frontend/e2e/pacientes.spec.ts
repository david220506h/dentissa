import { expect, test, type Locator, type Page } from '@playwright/test';
import { ANCHOS, abrirDiseno, sesionDeEjemplo, sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

// Pacientes con la API simulada y las personas de ejemplo del diseño (Pacientes.dc.html); no son reales.
const cita = (fecha: string, estado = 'confirmada') => ({ id: 1, fecha, hora: '10:00', estado, servicio: 'Limpieza' });
const PACIENTES = [
  { id: 1, nombre: 'Rosa', apellido: 'Aguilar', telefono: '55 5555 0101', cuenta: 'con-cuenta', alergias: null, ultimaCita: cita('2026-11-18', 'completada'), proximaCita: cita('2026-12-14') },
  { id: 2, nombre: 'Ernesto', apellido: 'Vega', telefono: '55 5555 0148', cuenta: 'con-cuenta', alergias: 'Penicilina', ultimaCita: cita('2026-11-04', 'completada'), proximaCita: cita('2026-11-18') },
  { id: 3, nombre: 'Valeria', apellido: 'Núñez', telefono: null, cuenta: 'invitado', alergias: null, ultimaCita: null, proximaCita: cita('2026-11-19', 'pendiente') },
  { id: 4, nombre: 'Mario', apellido: 'Delgado', telefono: '55 5555 0133', cuenta: 'con-cuenta', alergias: 'Látex', ultimaCita: cita('2026-11-17', 'completada'), proximaCita: cita('2026-11-18') },
  { id: 5, nombre: 'Sofía', apellido: 'Rangel', telefono: '55 5555 0127', cuenta: 'con-cuenta', alergias: null, ultimaCita: cita('2026-10-21', 'completada'), proximaCita: cita('2026-11-18') },
  { id: 6, nombre: 'Camila', apellido: 'Ortega', telefono: '55 5555 0190', cuenta: 'invitado', alergias: null, ultimaCita: null, proximaCita: cita('2026-11-21', 'pendiente') },
  { id: 7, nombre: 'Diego', apellido: 'Herrera', telefono: '55 5555 0166', cuenta: 'con-cuenta', alergias: null, ultimaCita: null, proximaCita: cita('2026-11-20', 'pendiente') },
  { id: 8, nombre: 'Paola', apellido: 'Rivas', telefono: '55 5555 0119', cuenta: 'con-cuenta', alergias: null, ultimaCita: cita('2026-11-16', 'completada'), proximaCita: null },
];

async function abrirPacientes(page: Page, ancho: number) {
  await page.clock.install({ time: new Date('2026-11-18T15:30:00Z') });
  await page.setViewportSize({ width: ancho, height: 900 });
  await sesionDeEjemplo(page, { rol: 'asistente' });
  // Registrada después: Playwright usa primero la última ruta que coincide
  await page.route('**/api/pacientes?**', (ruta) =>
    ruta.fulfill({ status: 200, json: { elementos: PACIENTES, total: PACIENTES.length, pagina: 1, tamano: 10 } }),
  );
  await page.goto('/pacientes');
  await page.getByText('Ernesto Vega').filter({ visible: true }).first().waitFor();
  await page.evaluate(() => document.fonts.ready);
}

const PROPIEDADES = [
  'font-size',
  'font-weight',
  'color',
  'background-color',
  'border-top-width',
  'border-top-color',
  'border-bottom-width',
  'border-radius',
  'padding',
  'gap',
  'height',
  'width',
] as const;
type Propiedad = (typeof PROPIEDADES)[number];

async function estilos(objetivo: Locator, ignorar: readonly Propiedad[] = []) {
  await objetivo.waitFor();
  const props = await objetivo.evaluate((el, lista) => {
    const cs = getComputedStyle(el);
    const caja = el.getBoundingClientRect();
    const r = (n: number) => `${Math.round(n * 100) / 100}px`;
    return Object.fromEntries(lista.map((p) => [p, p === 'height' ? r(caja.height) : p === 'width' ? r(caja.width) : cs.getPropertyValue(p)]));
  }, [...PROPIEDADES]);
  for (const p of ignorar) delete props[p];
  return props;
}

const ANCHO: Propiedad[] = ['width'];
const TEXTO: Propiedad[] = ['width', 'height'];
const CAJA: Propiedad[] = ['width', 'height', 'font-size', 'font-weight', 'color'];

type Caso = { nombre: string; diseno: (p: Page) => Locator; panel: (p: Page) => Locator; ignorar?: readonly Propiedad[] };

const filaDiseno = (p: Page, nombre: string) => p.locator('tr', { hasText: nombre });
const filaPanel = (p: Page, nombre: string) => p.getByRole('region', { name: 'Pacientes' }).locator('tr', { hasText: nombre });

const CASOS: Caso[] = [
  { nombre: 'título', diseno: (p) => p.locator('main h1'), panel: (p) => p.locator('main h1'), ignorar: TEXTO },
  { nombre: 'subtítulo', diseno: (p) => p.locator('main h1 + p'), panel: (p) => p.locator('main h1 + p'), ignorar: TEXTO },
  {
    nombre: 'botón Nuevo paciente',
    diseno: (p) => p.getByText('Nuevo paciente', { exact: true }).locator('xpath=ancestor-or-self::*[self::a or self::button][1]'),
    panel: (p) => p.getByRole('button', { name: 'Nuevo paciente' }),
    ignorar: ANCHO,
  },
  {
    nombre: 'campo de búsqueda',
    diseno: (p) => p.getByRole('searchbox', { name: 'Buscar paciente' }),
    panel: (p) => p.getByRole('searchbox', { name: 'Buscar paciente' }),
    ignorar: ANCHO,
  },
  {
    nombre: 'encabezado de columna',
    diseno: (p) => p.locator('th', { hasText: 'Alergias' }),
    panel: (p) => p.locator('th', { hasText: 'Alergias' }),
    ignorar: TEXTO,
  },
  {
    // El diseño pone el correo bajo el nombre; la lista de la API no lo trae (propuesta, 1.1)
    nombre: 'celda',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').locator('td').nth(1),
    panel: (p) => filaPanel(p, 'Ernesto Vega').locator('td').nth(1),
    ignorar: TEXTO,
  },
  {
    nombre: 'avatar de 36 px',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').getByText('EV', { exact: true }),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByText('EV', { exact: true }),
  },
  {
    nombre: 'nombre',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').getByText('Ernesto Vega', { exact: true }),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByText('Ernesto Vega', { exact: true }),
    ignorar: TEXTO,
  },
  {
    nombre: 'chip de alergia (rojo sólido)',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').getByText('Alergia: Penicilina'),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByText('Alergia: Penicilina'),
    ignorar: ANCHO,
  },
  {
    nombre: 'insignia Con cuenta',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').getByText('Con cuenta', { exact: true }),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByText('Con cuenta', { exact: true }),
  },
  {
    nombre: 'insignia Invitado',
    diseno: (p) => filaDiseno(p, 'Valeria Núñez').getByText('Invitado', { exact: true }),
    panel: (p) => filaPanel(p, 'Valeria Núñez').getByText('Invitado', { exact: true }),
  },
  {
    nombre: 'Sin registro',
    diseno: (p) => filaDiseno(p, 'Rosa Aguilar').getByText('Sin registro', { exact: true }),
    panel: (p) => filaPanel(p, 'Rosa Aguilar').getByText('Sin registro', { exact: true }),
    ignorar: TEXTO,
  },
  {
    nombre: 'Ver ficha',
    diseno: (p) => filaDiseno(p, 'Ernesto Vega').getByRole('link', { name: 'Ver ficha' }),
    panel: (p) => filaPanel(p, 'Ernesto Vega').getByRole('link', { name: /Ver ficha/ }),
    ignorar: ANCHO,
  },
];

test.describe('Pacientes frente a Pacientes.dc.html', () => {
  for (const caso of CASOS) {
    test(caso.nombre, async ({ page, context }) => {
      const diseno = await context.newPage();
      await diseno.setViewportSize({ width: 1440, height: 900 });
      await abrirDiseno(diseno, 'Pacientes.dc.html');
      const esperado = await estilos(caso.diseno(diseno), caso.ignorar);
      await abrirPacientes(page, 1440);
      expect(await estilos(caso.panel(page), caso.ignorar)).toEqual(esperado);
    });
  }
});

test('la lista muestra las solicitudes pendientes como "(solicitada)"', async ({ page }) => {
  await abrirPacientes(page, 1440);
  await expect(filaPanel(page, 'Valeria Núñez')).toContainText('19 nov 2026 (solicitada)');
});

for (const ancho of ANCHOS) {
  test(`Pacientes a ${ancho} px: accesible, sin desborde, texto ≥ 12.5 px, toques de 44 px y captura`, async ({ page }) => {
    await abrirPacientes(page, ancho);
    await sinViolacionesAxe(page);
    await sinDesbordeHorizontal(page);
    const { menor, pequenos } = await page.evaluate(() => {
      let min = Infinity;
      for (const el of document.querySelectorAll<HTMLElement>('body *')) {
        const tieneTexto = [...el.childNodes].some((n) => n.nodeType === 3 && n.textContent!.trim());
        if (tieneTexto && el.getClientRects().length) min = Math.min(min, parseFloat(getComputedStyle(el).fontSize));
      }
      const chicos: string[] = [];
      for (const el of document.querySelectorAll<HTMLElement>('main a, main button, main input, main select')) {
        const r = el.getBoundingClientRect();
        if (r.width && r.height < 44) chicos.push(`${el.tagName} "${el.textContent?.trim() || el.getAttribute('aria-label')}" ${r.height}px`);
      }
      return { menor: min, pequenos: chicos };
    });
    expect(menor).toBeGreaterThanOrEqual(12.5);
    expect(pequenos).toEqual([]);
    await expect(page).toHaveScreenshot(`pacientes-${ancho}.png`, { fullPage: true });
  });
}
