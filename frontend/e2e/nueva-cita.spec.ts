import { expect, test, type Locator, type Page } from '@playwright/test';
import { abrirDiseno, sesionDeEjemplo } from './utilidades';

// Formulario «Nueva cita» frente a CitaFormularios.dc.html, con la API simulada y datos de ejemplo (no reales).
// "Ahora": miércoles 18 nov 2026, 9:30 en la clínica.
const AHORA = new Date('2026-11-18T15:30:00Z');
const SERVICIOS = [
  { id: 1, nombre: 'Limpieza dental', precio: 600, duracionSlots: 1 },
  { id: 2, nombre: 'Tratamiento de conductos', precio: 3500, duracionSlots: 2 },
];
const libre = (hora: string) => ({
  hora,
  inicio: `2026-11-18T${hora}:00-06:00`,
  fin: `2026-11-18T${hora}:00-06:00`,
  dentistasLibres: [{ id: 1, nombre: 'Dra. Mariana Soto' }],
});

async function abrirFormulario(page: Page) {
  await page.clock.install({ time: AHORA });
  await page.setViewportSize({ width: 1440, height: 1100 });
  await sesionDeEjemplo(page, { rol: 'asistente' });
  // Registradas después: Playwright usa primero la última ruta que coincide
  await page.route('**/api/citas/servicios', (r) => r.fulfill({ status: 200, json: SERVICIOS }));
  await page.route('**/api/agenda/disponibilidad?**', (r) => r.fulfill({ status: 200, json: [libre('10:00'), libre('12:00')] }));
  await page.goto('/panel/agenda');
  await page.getByRole('button', { name: 'Nueva cita' }).click();
  const dialogo = page.getByRole('dialog', { name: 'Nueva cita' });
  await dialogo.getByLabel('Horario libre').selectOption('10:00');
  await page.waitForTimeout(400);
  await page.evaluate(() => document.fonts.ready);
  return dialogo;
}

const PROPIEDADES = [
  'font-size',
  'font-weight',
  'color',
  'background-color',
  'border-top-width',
  'border-top-color',
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

type Caso = { nombre: string; diseno: (d: Locator) => Locator; panel: (d: Locator) => Locator; ignorar?: readonly Propiedad[] };

const CASOS: Caso[] = [
  // El panel centra el diálogo con su propio ancho; el diseño lo dibuja en una columna
  { nombre: 'caja del diálogo', diseno: (d) => d, panel: (d) => d, ignorar: CAJA },
  { nombre: 'título', diseno: (d) => d.getByRole('heading', { name: 'Nueva cita' }), panel: (d) => d.getByRole('heading', { name: 'Nueva cita' }), ignorar: TEXTO },
  {
    nombre: 'ayuda bajo el título',
    diseno: (d) => d.getByText('Los horarios libres ya excluyen descansos y cierres.'),
    panel: (d) => d.getByText('Los horarios libres ya excluyen descansos y cierres.'),
    ignorar: TEXTO,
  },
  { nombre: 'etiqueta de campo', diseno: (d) => d.locator('label', { hasText: /^Servicio$/ }), panel: (d) => d.locator('label', { hasText: /^Servicio$/ }), ignorar: TEXTO },
  { nombre: 'lista Servicio', diseno: (d) => d.getByLabel('Servicio'), panel: (d) => d.getByLabel('Servicio'), ignorar: ANCHO },
  { nombre: 'campo Fecha', diseno: (d) => d.getByLabel('Fecha'), panel: (d) => d.getByLabel('Fecha'), ignorar: ANCHO },
  { nombre: 'lista Horario libre', diseno: (d) => d.getByLabel('Horario libre'), panel: (d) => d.getByLabel('Horario libre'), ignorar: ANCHO },
  { nombre: 'lista Dentista', diseno: (d) => d.getByLabel('Dentista'), panel: (d) => d.getByLabel('Dentista'), ignorar: ANCHO },
  { nombre: 'Motivo', diseno: (d) => d.getByLabel('Motivo (opcional)'), panel: (d) => d.getByLabel('Motivo (opcional)'), ignorar: TEXTO },
  {
    nombre: 'ayuda del motivo',
    diseno: (d) => d.getByText('Solo lo ve el personal autorizado.', { exact: false }),
    panel: (d) => d.getByText('Solo lo ve el personal autorizado.', { exact: false }),
    ignorar: TEXTO,
  },
  {
    // El diseño escribe "la paciente"; el panel usa "la persona" (género neutro)
    nombre: 'casilla Crear ya confirmada',
    diseno: (d) => d.locator('label', { hasText: 'Crear ya confirmada' }),
    panel: (d) => d.locator('label', { hasText: 'Crear ya confirmada' }),
    ignorar: TEXTO,
  },
  { nombre: 'botón Cancelar', diseno: (d) => d.getByRole('button', { name: 'Cancelar' }), panel: (d) => d.getByRole('button', { name: 'Cancelar' }), ignorar: ANCHO },
  { nombre: 'botón Guardar cita', diseno: (d) => d.getByRole('button', { name: 'Guardar cita' }), panel: (d) => d.getByRole('button', { name: 'Guardar cita' }), ignorar: ANCHO },
  {
    nombre: 'botón Cerrar',
    diseno: (d) => d.getByRole('button', { name: 'Cerrar' }),
    panel: (d) => d.getByRole('button', { name: 'Cerrar' }),
  },
];

test.describe('Nueva cita frente a CitaFormularios.dc.html', () => {
  for (const caso of CASOS) {
    test(caso.nombre, async ({ page, context }) => {
      const diseno = await context.newPage();
      await diseno.setViewportSize({ width: 1440, height: 1100 });
      await abrirDiseno(diseno, 'CitaFormularios.dc.html');
      const esperado = await estilos(caso.diseno(diseno.getByRole('dialog', { name: 'Nueva cita' })), caso.ignorar);
      const dialogo = await abrirFormulario(page);
      expect(await estilos(caso.panel(dialogo), caso.ignorar)).toEqual(esperado);
    });
  }
});
