import { expect, test, type Locator, type Page } from '@playwright/test';
import { ANCHOS, abrirDiseno, sesionDeEjemplo, sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

// Agenda con la API simulada y las citas de ejemplo del propio diseño (Agenda.dc.html), no reales.
// "Ahora": miércoles 18 nov 2026, 9:30 en la clínica.
const AHORA = new Date('2026-11-18T15:30:00Z');
const TODOS = ['08:00', '09:00', '10:00', '12:00', '13:00', '15:00'];

let siguienteId = 1;
const cita = (paciente: string, servicio: string, dia: number, hora: string, estado: string, horas = 1) => {
  const inicio = new Date(`2026-11-${dia}T${hora}:00-06:00`);
  return {
    id: siguienteId++,
    paciente,
    servicio,
    dentista: 'Dra. Ana Torres',
    dentistaId: 1,
    pacienteId: siguienteId,
    fecha: `${dia}/11/2026`,
    hora,
    inicio: inicio.toISOString(),
    fin: new Date(inicio.getTime() + horas * 3_600_000).toISOString(),
    estado,
  };
};

const CITAS = [
  cita('Andrea Cruz', 'Limpieza dental', 16, '08:00', 'completada'),
  cita('Luis Ibarra', 'Resina dental', 16, '09:00', 'completada'),
  cita('Paola Rivas', 'Extracción dental', 16, '10:00', 'no_asistio'),
  cita('Jorge Salinas', 'Limpieza dental', 16, '12:00', 'completada'),
  cita('Elena Mora', 'Resina dental', 16, '15:00', 'completada'),
  cita('Hugo Paredes', 'Limpieza dental', 17, '08:00', 'completada'),
  cita('Daniela Soto', 'Resina dental', 17, '09:00', 'completada'),
  cita('Mario Delgado', 'Tratamiento de conductos', 17, '12:00', 'completada', 2),
  cita('Iván Lara', 'Limpieza dental', 17, '15:00', 'completada'),
  cita('Rosa Aguilar', 'Resina dental', 18, '08:00', 'confirmada'),
  cita('Ernesto Vega', 'Extracción dental', 18, '09:00', 'pendiente'),
  cita('Mario Delgado', 'Tratamiento de conductos', 18, '12:00', 'confirmada', 2),
  cita('Sofía Rangel', 'Limpieza dental', 18, '15:00', 'confirmada'),
  cita('Valeria Núñez', 'Limpieza dental', 19, '09:00', 'pendiente'),
  cita('Patricia Gil', 'Limpieza dental', 19, '10:00', 'confirmada'),
  cita('Roberto Salas', 'Resina dental', 19, '15:00', 'pendiente'),
  cita('Óscar Reyes', 'Resina dental', 20, '08:00', 'confirmada'),
  cita('Diego Herrera', 'Valoración ortodoncia', 20, '09:00', 'pendiente'),
  cita('Mariana Paz', 'Limpieza dental', 20, '12:00', 'confirmada'),
  cita('Camila Ortega', 'Resina dental', 21, '08:00', 'pendiente'),
];

// Horarios libres simulados: el horario de la clínica menos lo ocupado ese día (domingo cerrado)
const libres = (fecha: string) => {
  if (new Date(`${fecha}T12:00:00Z`).getUTCDay() === 0) return [];
  const [a, m, d] = fecha.split('-');
  const ocupados = new Set(
    CITAS.filter((c) => c.fecha === `${d}/${m}/${a}`).flatMap((c) =>
      Array.from({ length: Math.round((Date.parse(c.fin) - Date.parse(c.inicio)) / 3_600_000) }, (_, i) => `${String(Number(c.hora.slice(0, 2)) + i).padStart(2, '0')}:00`),
    ),
  );
  return TODOS.filter((h) => !ocupados.has(h));
};

async function abrirAgenda(page: Page, ancho: number) {
  await page.clock.install({ time: AHORA });
  await page.setViewportSize({ width: ancho, height: 900 });
  await sesionDeEjemplo(page, {
    rol: 'asistente',
    citas: CITAS,
    dentistas: [{ id: 1, nombre: 'Ana', apellido: 'Torres', titulo: 'Dra.' }],
    libres,
  });
  await page.goto('/agenda');
  await page.evaluate(() => document.fonts.ready);
}

const PROPIEDADES = [
  'font-size',
  'font-weight',
  'color',
  'background-color',
  'background-image',
  'border-top-width',
  'border-top-style',
  'border-top-color',
  'border-left-width',
  'border-radius',
  'padding',
  'margin',
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
// Letra heredada por contenedores sin texto propio: el diseño parte de 16 px y el panel de 15
const HEREDADA: Propiedad[] = ['font-size'];
// Valor declarado con border-box (criterio aprobado, como la leyenda del odontograma): el diseño declara
// 56 px (días) y 48 px (Libre) sin border-box y los dibuja con el borde, 58 y 50
const ALTO_DECLARADO: Propiedad[] = ['height'];

type Caso = { nombre: string; diseno: (p: Page) => Locator; panel: (p: Page) => Locator; ignorar?: readonly Propiedad[] };

const semana = (p: Page) => p.getByRole('region', { name: 'Citas de la semana' });
const bloqueDiseno = (p: Page, nombre: string, servicio: string) =>
  p.getByText(nombre, { exact: true }).filter({ has: p.locator('xpath=self::*') }).locator('xpath=..').filter({ hasText: servicio }).first();

const ESCRITORIO: Caso[] = [
  { nombre: 'título', diseno: (p) => p.locator('main h1'), panel: (p) => p.locator('main h1'), ignorar: TEXTO },
  { nombre: 'subtítulo de la semana', diseno: (p) => p.locator('main h1 + p'), panel: (p) => p.locator('main h1 + p'), ignorar: TEXTO },
  {
    nombre: 'barra de controles',
    diseno: (p) => p.getByRole('button', { name: 'Semana anterior' }).locator('xpath=ancestor::section[1]'),
    panel: (p) => p.getByRole('region', { name: 'Controles de la agenda' }),
    // En el diseño la separación vive en un div interior; en el panel, en la propia barra
    ignorar: [...CAJA, 'gap'],
  },
  { nombre: 'botón Semana anterior', diseno: (p) => p.getByRole('button', { name: 'Semana anterior' }), panel: (p) => p.getByRole('button', { name: 'Semana anterior' }) },
  { nombre: 'botón Hoy', diseno: (p) => p.getByRole('button', { name: 'Hoy' }), panel: (p) => p.getByRole('button', { name: 'Hoy' }) },
  {
    nombre: 'rango de la semana',
    diseno: (p) => p.getByText('16 – 21 nov 2026', { exact: true }),
    panel: (p) => p.getByRole('region', { name: 'Controles de la agenda' }).getByText('16 – 21 nov 2026', { exact: true }),
    ignorar: TEXTO,
  },
  {
    nombre: 'vista Semana elegida',
    diseno: (p) => p.getByRole('button', { name: 'Semana', exact: true }),
    panel: (p) => p.getByRole('button', { name: 'Semana', exact: true }),
    ignorar: ANCHO,
  },
  { nombre: 'etiqueta Dentista', diseno: (p) => p.locator('label', { hasText: 'Dentista' }), panel: (p) => p.locator('label', { hasText: 'Dentista' }), ignorar: TEXTO },
  { nombre: 'lista de dentistas', diseno: (p) => p.locator('label', { hasText: 'Dentista' }).locator('select'), panel: (p) => p.getByLabel('Dentista'), ignorar: ANCHO },
  {
    nombre: 'leyenda de estados',
    diseno: (p) => p.getByText('Una solicitud pendiente bloquea su horario.').locator('xpath=..'),
    panel: (p) => p.getByText('Una solicitud pendiente bloquea su horario.').locator('xpath=..'),
    // El diseño separa la cuadrícula con un div de 16 px; el panel, con 16 px de margen
    ignorar: [...TEXTO, 'margin'],
  },
  {
    nombre: 'cabecera de un día',
    diseno: (p) => p.getByText('Lun', { exact: true }).locator('xpath=..'),
    panel: (p) => semana(p).getByText('Lun', { exact: true }).locator('xpath=..'),
    ignorar: [...ANCHO, ...HEREDADA],
  },
  {
    nombre: 'cabecera de hoy',
    diseno: (p) => p.getByText('Mié · hoy', { exact: true }).locator('xpath=..'),
    panel: (p) => semana(p).getByText('Mié · hoy', { exact: true }).locator('xpath=..'),
    ignorar: [...ANCHO, ...HEREDADA],
  },
  {
    nombre: 'número del día de hoy',
    diseno: (p) => p.getByText('Mié · hoy', { exact: true }).locator('xpath=following-sibling::span[1]'),
    panel: (p) => semana(p).getByText('Mié · hoy', { exact: true }).locator('xpath=following-sibling::span[1]'),
  },
  {
    nombre: 'hora de una fila',
    diseno: (p) => p.getByText('9:00', { exact: true }).first(),
    panel: (p) => semana(p).getByText('9:00', { exact: true }),
  },
  {
    nombre: 'descanso',
    diseno: (p) => p.getByText('Descanso', { exact: true }).first(),
    panel: (p) => semana(p).getByText('Descanso', { exact: true }).first(),
    // Ocupa las 6 columnas: su ancho depende del ancho de la página
    ignorar: ANCHO,
  },
  {
    nombre: 'cita confirmada',
    diseno: (p) => bloqueDiseno(p, 'Rosa Aguilar', 'Resina dental'),
    panel: (p) => semana(p).getByRole('button', { name: /Rosa Aguilar/ }),
    ignorar: [...ANCHO, ...HEREDADA],
  },
  {
    nombre: 'cita pendiente',
    diseno: (p) => bloqueDiseno(p, 'Ernesto Vega', 'Extracción dental'),
    panel: (p) => semana(p).getByRole('button', { name: /Ernesto Vega/ }),
    ignorar: [...ANCHO, ...HEREDADA],
  },
  {
    nombre: 'cita de dos horarios',
    diseno: (p) => bloqueDiseno(p, 'Mario Delgado', 'sesión 2 de 3'),
    panel: (p) => semana(p).getByRole('button', { name: /Mario Delgado.*Confirmada/ }),
    ignorar: [...ANCHO, ...HEREDADA],
  },
  {
    nombre: 'nombre en la cita',
    diseno: (p) => p.getByText('Rosa Aguilar', { exact: true }),
    panel: (p) => semana(p).getByText('Rosa Aguilar', { exact: true }),
    ignorar: TEXTO,
  },
  {
    nombre: 'estado en la cita',
    diseno: (p) => bloqueDiseno(p, 'Ernesto Vega', 'Extracción dental').getByText('Pendiente', { exact: true }),
    panel: (p) => semana(p).getByRole('button', { name: /Ernesto Vega/ }).getByText('Pendiente', { exact: true }),
    ignorar: TEXTO,
  },
];

const filaMovil = (p: Page, hora: string) =>
  p.getByText('Agenda del día').locator('xpath=../..').locator('div[style*="flex:0 0 52px"]', { hasText: new RegExp(`^${hora}$`) }).locator('xpath=..');
const diaPanel = (p: Page) => p.getByRole('region', { name: 'Miércoles 18 de noviembre de 2026' });

const CELULAR: Caso[] = [
  {
    nombre: 'día elegido',
    diseno: (p) => p.locator('a[aria-current="date"]').first(),
    panel: (p) => diaPanel(p).getByRole('button', { pressed: true }),
    ignorar: [...ANCHO, ...HEREDADA, ...ALTO_DECLARADO],
  },
  {
    nombre: 'día sin elegir',
    diseno: (p) => p.getByText('Agenda del día').locator('xpath=../..').getByRole('link', { name: /Lun/ }),
    panel: (p) => diaPanel(p).getByRole('button', { name: /Lun/ }),
    ignorar: [...ANCHO, ...HEREDADA, ...ALTO_DECLARADO],
  },
  {
    nombre: 'hora',
    diseno: (p) => filaMovil(p, '8:00').locator('> div').first(),
    panel: (p) => diaPanel(p).getByRole('listitem').first().locator('> span').first(),
    ignorar: TEXTO,
  },
  {
    nombre: 'cita',
    diseno: (p) => filaMovil(p, '8:00').locator('div[style*="border-radius:12px"]').first(),
    panel: (p) => diaPanel(p).getByRole('button', { name: /Rosa Aguilar/ }),
    ignorar: [...ANCHO, ...HEREDADA],
  },
  {
    nombre: 'horario libre',
    diseno: (p) => filaMovil(p, '10:00').locator('div[style*="dashed"]'),
    panel: (p) => diaPanel(p).getByText('Libre', { exact: true }),
    ignorar: [...ANCHO, ...ALTO_DECLARADO],
  },
  {
    nombre: 'descanso',
    diseno: (p) => filaMovil(p, '11:00').getByText('Descanso', { exact: true }),
    panel: (p) => diaPanel(p).getByText('Descanso', { exact: true }).first(),
    ignorar: ANCHO,
  },
];

test.describe('Agenda en escritorio frente a Agenda.dc.html', () => {
  for (const caso of ESCRITORIO) {
    test(caso.nombre, async ({ page, context }) => {
      const diseno = await context.newPage();
      await diseno.setViewportSize({ width: 1440, height: 900 });
      await abrirDiseno(diseno, 'Agenda.dc.html');
      const esperado = await estilos(caso.diseno(diseno), caso.ignorar);
      await abrirAgenda(page, 1440);
      await expect(semana(page).getByText('Descanso').first()).toBeVisible();
      expect(await estilos(caso.panel(page), caso.ignorar)).toEqual(esperado);
    });
  }
});

test('excepción: días (56 px) y "Libre" (48 px) miden su valor declarado; el diseño dibuja 58 y 50', async ({ page, context }) => {
  const diseno = await context.newPage();
  await abrirDiseno(diseno, 'Movil.dc.html');
  const alto = (l: Locator) => l.evaluate((el) => Math.round(el.getBoundingClientRect().height));
  expect(await alto(diseno.locator('a[aria-current="date"]').first()), 'si el diseño ya mide 56, retirar la excepción').toBe(58);
  expect(await alto(filaMovil(diseno, '10:00').locator('div[style*="dashed"]'))).toBe(50);
  await abrirAgenda(page, 390);
  await expect(diaPanel(page).getByText('Libre')).toBeVisible();
  expect(await alto(diaPanel(page).getByRole('button', { pressed: true }))).toBe(56);
  expect(await alto(diaPanel(page).getByText('Libre', { exact: true }))).toBe(48);
});

test.describe('Agenda del día en celular frente a Movil.dc.html', () => {
  for (const caso of CELULAR) {
    test(caso.nombre, async ({ page, context }) => {
      const diseno = await context.newPage();
      await diseno.setViewportSize({ width: 1800, height: 960 });
      await abrirDiseno(diseno, 'Movil.dc.html');
      const esperado = await estilos(caso.diseno(diseno), caso.ignorar);
      await abrirAgenda(page, 390);
      await expect(diaPanel(page).getByText('Libre')).toBeVisible();
      expect(await estilos(caso.panel(page), caso.ignorar)).toEqual(esperado);
    });
  }
});

for (const ancho of ANCHOS) {
  test(`Agenda a ${ancho} px: accesible, sin desborde, texto ≥ 12.5 px y captura`, async ({ page }) => {
    await abrirAgenda(page, ancho);
    await expect(page.getByText('Descanso').filter({ visible: true }).first()).toBeVisible();
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
    await expect(page).toHaveScreenshot(`agenda-${ancho}.png`, { fullPage: true });
  });

  test(`diálogos de la cita a ${ancho} px: foco, Esc y captura`, async ({ page }) => {
    await abrirAgenda(page, ancho);
    const disparador = page.getByRole('button', { name: /Ernesto Vega/ }).first();
    await disparador.click();
    const dialogo = page.getByRole('dialog', { name: 'Cita' });
    await expect(dialogo).toBeVisible();
    await page.waitForTimeout(400);
    await sinViolacionesAxe(page);
    await expect(page).toHaveScreenshot(`agenda-cita-${ancho}.png`);
    await dialogo.getByRole('button', { name: 'Cancelar cita' }).click();
    const cancelar = page.getByRole('dialog', { name: 'Cancelar cita' });
    await expect(cancelar).toBeVisible();
    await page.waitForTimeout(400);
    await sinViolacionesAxe(page);
    await expect(page).toHaveScreenshot(`agenda-cancelar-${ancho}.png`);
    await page.keyboard.press('Escape');
    await expect(cancelar).toHaveCount(0);
    // El foco vuelve al botón que abrió "Cancelar cita" (dentro del diálogo de la cita)
    await expect(dialogo.getByRole('button', { name: 'Cancelar cita' })).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(dialogo).toHaveCount(0);
    await expect(disparador).toBeFocused();
  });
}

test('vista Día en escritorio: captura', async ({ page }) => {
  await abrirAgenda(page, 1440);
  await page.getByRole('button', { name: 'Día', exact: true }).click();
  await expect(diaPanel(page).getByText('Libre')).toBeVisible();
  await sinViolacionesAxe(page);
  await expect(page).toHaveScreenshot('agenda-dia-1440.png', { fullPage: true });
});
