import { expect, test, type Locator, type Page } from '@playwright/test';
import { ANCHOS, abrirDiseno, sesionDeEjemplo, sinDesbordeHorizontal, sinViolacionesAxe } from './utilidades';

// Equipo y usuarios con la API simulada y las personas de ejemplo del diseño (correos .test), no reales.
const USUARIOS = [
  { id: 11, nombre: 'Laura', apellido: 'Mendoza', email: 'laura.mendoza@example.com', rol: 'admin', activo: true, ultimoAccesoEn: '2026-11-18T14:02:00Z' },
  { id: 12, nombre: 'Mariana', apellido: 'Soto', email: 'mariana.soto@example.com', rol: 'dentista', activo: true, ultimoAccesoEn: '2026-11-18T13:55:00Z' },
  { id: 13, nombre: 'Karla', apellido: 'Reyes', email: 'karla.reyes@example.com', rol: 'asistente', activo: true, ultimoAccesoEn: '2026-11-18T00:40:00Z' },
  { id: 14, nombre: 'Jorge', apellido: 'Pineda', email: 'jorge.pineda@example.com', rol: 'asistente', activo: false, ultimoAccesoEn: '2026-10-02T17:00:00Z' },
];

async function abrirEquipo(page: Page, ancho: number) {
  // "Ahora": miércoles 18 nov 2026, 9:30 en la clínica ("Hoy, 8:02" como en el diseño)
  await page.clock.install({ time: new Date('2026-11-18T15:30:00Z') });
  await page.setViewportSize({ width: ancho, height: 900 });
  // La sesión es de otra cuenta de administración: así Laura es "la única administración activa", como en el diseño
  await sesionDeEjemplo(page, { rol: 'admin', usuarios: USUARIOS });
  await page.goto('/equipo');
  await page.getByRole('button', { name: 'Acceso de Laura Mendoza' }).waitFor();
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

type Caso = { nombre: string; diseno: (p: Page) => Locator; panel: (p: Page) => Locator; ignorar?: readonly Propiedad[]; elegir?: boolean };

const filaDiseno = (p: Page, nombre: string) => p.locator('tr', { hasText: nombre });
const filaPanel = (p: Page, nombre: string) => p.getByRole('region', { name: 'Usuarios de la clínica' }).locator('tr', { hasText: nombre });
const panelDiseno = (p: Page) => p.getByRole('heading', { name: 'Editar usuario' }).locator('xpath=ancestor::section[1]');
const panelPanel = (p: Page) => p.getByRole('region', { name: 'Acceso' });

const CASOS: Caso[] = [
  { nombre: 'título', diseno: (p) => p.locator('main h1'), panel: (p) => p.locator('main h1'), ignorar: TEXTO },
  { nombre: 'subtítulo', diseno: (p) => p.locator('main h1 + p'), panel: (p) => p.locator('main h1 + p'), ignorar: TEXTO },
  {
    nombre: 'aviso de auditoría',
    diseno: (p) => p.getByText('Los cambios de rol y las desactivaciones', { exact: false }).locator('xpath=..'),
    panel: (p) => p.getByText('Los cambios de rol y las desactivaciones', { exact: false }).locator('xpath=../..'),
    // El peso va en el texto (lo comparan los avisos de Estados); la caja se compara aquí
    ignorar: [...TEXTO, 'font-weight'],
  },
  {
    nombre: 'tarjeta de usuarios',
    diseno: (p) => p.getByRole('heading', { name: 'Usuarios de la clínica' }).locator('xpath=ancestor::section[1]'),
    panel: (p) => p.getByRole('heading', { name: 'Usuarios de la clínica' }).locator('xpath=ancestor::section[1]'),
    ignorar: CAJA,
  },
  {
    nombre: 'título de la tarjeta',
    diseno: (p) => p.getByRole('heading', { name: 'Usuarios de la clínica' }),
    panel: (p) => p.getByRole('heading', { name: 'Usuarios de la clínica' }),
    ignorar: TEXTO,
  },
  {
    nombre: 'descripción de la tarjeta',
    diseno: (p) => p.getByText('Cada persona entra con su propio correo.', { exact: false }),
    panel: (p) => p.getByText('Cada persona entra con su propio correo.', { exact: false }),
    ignorar: TEXTO,
  },
  {
    nombre: 'encabezado de columna',
    diseno: (p) => p.locator('th', { hasText: 'Persona' }),
    panel: (p) => p.locator('th', { hasText: 'Persona' }),
    ignorar: TEXTO,
  },
  { nombre: 'celda', diseno: (p) => filaDiseno(p, 'Karla Reyes').locator('td').first(), panel: (p) => filaPanel(p, 'Karla Reyes').locator('td').first(), ignorar: ANCHO },
  {
    nombre: 'avatar de 36 px',
    diseno: (p) => filaDiseno(p, 'Karla Reyes').getByText('KR', { exact: true }),
    panel: (p) => filaPanel(p, 'Karla Reyes').getByText('KR', { exact: true }),
  },
  {
    nombre: 'nombre',
    diseno: (p) => filaDiseno(p, 'Karla Reyes').getByText('Karla Reyes', { exact: true }),
    panel: (p) => filaPanel(p, 'Karla Reyes').getByText('Karla Reyes', { exact: true }),
    ignorar: TEXTO,
  },
  {
    nombre: 'correo',
    diseno: (p) => filaDiseno(p, 'Karla Reyes').getByText('karla.reyes@example.com'),
    panel: (p) => filaPanel(p, 'Karla Reyes').getByText('karla.reyes@example.com'),
    ignorar: TEXTO,
  },
  {
    nombre: 'insignia de administración (rosa, el diseño dice "Administrador")',
    diseno: (p) => filaDiseno(p, 'Laura Mendoza').getByText('Administrador', { exact: true }),
    panel: (p) => filaPanel(p, 'Laura Mendoza').getByText('Administración', { exact: true }),
    ignorar: ANCHO,
  },
  {
    nombre: 'insignia de dentista',
    diseno: (p) => filaDiseno(p, 'Mariana Soto').getByText('Dentista', { exact: true }),
    panel: (p) => filaPanel(p, 'Mariana Soto').getByText('Dentista', { exact: true }),
  },
  {
    nombre: 'insignia Inactivo',
    diseno: (p) => filaDiseno(p, 'Jorge Pineda').getByText('Inactivo', { exact: true }),
    panel: (p) => filaPanel(p, 'Jorge Pineda').getByText('Inactivo', { exact: true }),
  },
  {
    // El panel lo llama "Acceso" mientras no exista la edición; se comparan los estilos del botón
    nombre: 'botón Acceso (el diseño dice "Editar")',
    diseno: (p) => filaDiseno(p, 'Karla Reyes').getByRole('link', { name: 'Editar' }),
    panel: (p) => filaPanel(p, 'Karla Reyes').getByRole('button', { name: /Acceso/ }),
    ignorar: ANCHO,
  },
  {
    nombre: 'fila elegida',
    diseno: (p) => filaDiseno(p, 'Laura Mendoza'),
    panel: (p) => filaPanel(p, 'Laura Mendoza'),
    ignorar: CAJA,
    elegir: true,
  },
  {
    nombre: 'roles: fila',
    diseno: (p) => p.getByText('Agenda, solicitudes y pacientes.', { exact: false }).locator('xpath=..'),
    panel: (p) => p.getByText('Agenda, solicitudes y pacientes.', { exact: false }).locator('xpath=..'),
    ignorar: TEXTO,
  },
  { nombre: 'panel: caja', diseno: panelDiseno, panel: panelPanel, ignorar: CAJA, elegir: true },
  {
    nombre: 'panel: título (el diseño dice "Editar usuario")',
    diseno: (p) => panelDiseno(p).getByRole('heading', { name: 'Editar usuario' }),
    panel: (p) => panelPanel(p).getByRole('heading', { name: 'Acceso' }),
    ignorar: TEXTO,
    elegir: true,
  },
  {
    nombre: 'panel: separador de la sección de acceso',
    diseno: (p) => panelDiseno(p).getByText('Acceso', { exact: true }).locator('xpath=..'),
    panel: (p) => panelPanel(p).getByRole('button', { name: 'Desactivar usuario' }).locator('xpath=..'),
    ignorar: CAJA,
    elegir: true,
  },
  {
    nombre: 'último acceso en la tabla',
    diseno: (p) => filaDiseno(p, 'Laura Mendoza').getByText('Hoy, 8:02', { exact: true }),
    panel: (p) => filaPanel(p, 'Laura Mendoza').getByText('Hoy, 8:02', { exact: true }),
    ignorar: ANCHO,
  },
  {
    nombre: 'panel: Desactivar (deshabilitado por última administración)',
    diseno: (p) => panelDiseno(p).getByRole('button', { name: 'Desactivar usuario' }),
    panel: (p) => panelPanel(p).getByRole('button', { name: 'Desactivar usuario' }),
    ignorar: ANCHO,
    elegir: true,
  },
  {
    nombre: 'panel: nota de desactivar',
    diseno: (p) => panelDiseno(p).getByText('Desactivar cierra sus sesiones al instante.', { exact: false }),
    panel: (p) => panelPanel(p).getByText('Desactivar cierra sus sesiones al instante.', { exact: false }),
    ignorar: TEXTO,
    elegir: true,
  },
];

test.describe('Equipo frente a Equipo.dc.html', () => {
  for (const caso of CASOS) {
    test(caso.nombre, async ({ page, context }) => {
      const diseno = await context.newPage();
      await diseno.setViewportSize({ width: 1440, height: 900 });
      await abrirDiseno(diseno, 'Equipo.dc.html');
      const esperado = await estilos(caso.diseno(diseno), caso.ignorar);
      await abrirEquipo(page, 1440);
      if (caso.elegir) await page.getByRole('button', { name: 'Acceso de Laura Mendoza' }).click();
      expect(await estilos(caso.panel(page), caso.ignorar)).toEqual(esperado);
    });
  }
});

for (const ancho of ANCHOS) {
  test(`Equipo a ${ancho} px: accesible, sin desborde, texto ≥ 12.5 px y captura`, async ({ page }) => {
    await abrirEquipo(page, ancho);
    await page.getByRole('button', { name: 'Acceso de Laura Mendoza' }).click();
    await expect(panelPanel(page).getByRole('heading', { name: 'Acceso' })).toBeFocused();
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
    await expect(page).toHaveScreenshot(`equipo-${ancho}.png`, { fullPage: true });
  });
}

test('desactivar a una persona: aviso de éxito', async ({ page }) => {
  await abrirEquipo(page, 1440);
  await page.getByRole('button', { name: 'Acceso de Karla Reyes' }).click();
  await panelPanel(page).getByRole('button', { name: 'Desactivar usuario' }).click();
  await expect(page.getByText('La cuenta de Karla Reyes quedó inactiva. Sus sesiones se cerraron.')).toBeVisible();
});
