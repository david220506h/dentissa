import { expect, test, type Locator, type Page } from '@playwright/test';
import { abrirDiseno, sesionDeEjemplo } from './utilidades';

// Hoja "Más" y pacientes en tarjetas (catálogo y panel) frente a Movil2.dc.html.

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
  'width',
  'height',
  'min-height',
  'stroke',
  'stroke-width',
] as const;
type Propiedad = (typeof PROPIEDADES)[number];

async function estilos(objetivo: Locator, ignorar: readonly Propiedad[]) {
  await objetivo.waitFor();
  const props = await objetivo.evaluate((el, lista) => {
    const cs = getComputedStyle(el);
    const caja = el.getBoundingClientRect();
    const redondeo = (n: number) => `${Math.round(n * 100) / 100}px`;
    return Object.fromEntries(
      lista.map((p) => [p, p === 'width' ? redondeo(caja.width) : p === 'height' ? redondeo(caja.height) : cs.getPropertyValue(p)]),
    );
  }, [...PROPIEDADES]);
  for (const p of ignorar) delete props[p];
  return props;
}

// Lo que depende del contenedor o del texto (anchos), y la letra heredada en cajas sin texto propio
const ANCHO: Propiedad[] = ['width'];
const CAJA: Propiedad[] = ['font-size', 'font-weight', 'color', 'border-top-color'];
// Radio de al menos la mitad del alto = píldora (regla aprobada): 3 px en el diseño, --radio-pildora en el panel
const PILDORA: Propiedad[] = ['border-radius'];
const ICONO: Propiedad[] = ['font-size', 'color', 'border-top-color'];
// "auto" y "0px" son el mismo mínimo según el elemento sea flexible o de bloque; el alto real sí se compara
const MINIMO: Propiedad[] = ['min-height'];

const hojaDiseno = (p: Page) => p.locator('section[aria-label="Más opciones"]').first();
const hojaCatalogo = (p: Page) => p.getByRole('dialog', { name: 'Más opciones' });
const tarjetaDiseno = (p: Page, nombre: string) => p.getByText(nombre, { exact: true }).locator('xpath=ancestor::a[1]');
const tarjetaCatalogo = (p: Page, nombre: string) =>
  p.locator('section[aria-labelledby="sec-Pacientes en tarjetas"]').getByText(nombre, { exact: true }).locator('xpath=ancestor::a[1]');

type Caso = {
  nombre: string;
  diseno: (p: Page) => Locator;
  catalogo: (p: Page) => Locator;
  ignorar?: readonly Propiedad[];
  hoja?: boolean;
};

const CASOS: Caso[] = [
  { nombre: 'hoja: caja', diseno: hojaDiseno, catalogo: hojaCatalogo, hoja: true, ignorar: [...ANCHO, 'height', ...CAJA, ...MINIMO] },
  {
    nombre: 'hoja: asa',
    diseno: (p) => hojaDiseno(p).locator('> span[aria-hidden="true"]'),
    catalogo: (p) => hojaCatalogo(p).locator('> span[aria-hidden="true"]'),
    hoja: true,
    ignorar: [...CAJA, ...PILDORA],
  },
  {
    nombre: 'hoja: fila del perfil',
    diseno: (p) => hojaDiseno(p).getByText('Laura Mendoza').locator('xpath=../..'),
    catalogo: (p) => hojaCatalogo(p).getByText('Laura Mendoza').locator('xpath=../..'),
    hoja: true,
    ignorar: [...ANCHO, ...CAJA],
  },
  {
    nombre: 'hoja: avatar',
    diseno: (p) => hojaDiseno(p).getByText('LM', { exact: true }),
    catalogo: (p) => hojaCatalogo(p).getByText('LM', { exact: true }),
    hoja: true,
  },
  {
    nombre: 'hoja: nombre',
    diseno: (p) => hojaDiseno(p).getByText('Laura Mendoza'),
    catalogo: (p) => hojaCatalogo(p).getByText('Laura Mendoza'),
    hoja: true,
    ignorar: ANCHO,
  },
  {
    nombre: 'hoja: rol (el diseño dice "Administradora")',
    diseno: (p) => hojaDiseno(p).getByText('Administradora'),
    // El panel usa el nombre neutro del rol ("Administración"); se comparan los estilos del texto
    catalogo: (p) => hojaCatalogo(p).getByText('Administración'),
    hoja: true,
    ignorar: ANCHO,
  },
  {
    nombre: 'hoja: enlace de sección',
    diseno: (p) => hojaDiseno(p).getByRole('link', { name: 'Servicios' }),
    catalogo: (p) => hojaCatalogo(p).getByRole('link', { name: 'Servicios' }),
    hoja: true,
    ignorar: ANCHO,
  },
  {
    nombre: 'hoja: ícono de la sección',
    diseno: (p) => hojaDiseno(p).getByRole('link', { name: 'Servicios' }).locator('svg'),
    catalogo: (p) => hojaCatalogo(p).getByRole('link', { name: 'Servicios' }).locator('svg'),
    hoja: true,
    ignorar: ICONO,
  },
  {
    nombre: 'hoja: Cerrar sesión',
    diseno: (p) => hojaDiseno(p).getByRole('link', { name: 'Cerrar sesión' }),
    catalogo: (p) => hojaCatalogo(p).getByRole('button', { name: 'Cerrar sesión' }),
    hoja: true,
    ignorar: [...ANCHO, 'height'],
  },
  {
    nombre: 'tarjeta de paciente: caja',
    diseno: (p) => tarjetaDiseno(p, 'Ernesto Vega'),
    catalogo: (p) => tarjetaCatalogo(p, 'Ernesto Vega'),
    ignorar: [...ANCHO, 'font-size'],
  },
  {
    nombre: 'tarjeta de paciente: nombre',
    diseno: (p) => tarjetaDiseno(p, 'Ernesto Vega').getByText('Ernesto Vega'),
    catalogo: (p) => tarjetaCatalogo(p, 'Ernesto Vega').getByText('Ernesto Vega'),
    ignorar: [...ANCHO, ...MINIMO],
  },
  {
    nombre: 'tarjeta de paciente: teléfono',
    diseno: (p) => tarjetaDiseno(p, 'Ernesto Vega').getByText('55 5555 0148'),
    catalogo: (p) => tarjetaCatalogo(p, 'Ernesto Vega').getByText('55 5555 0148'),
    ignorar: [...ANCHO, ...MINIMO],
  },
  {
    nombre: 'tarjeta de paciente: insignia Con cuenta',
    diseno: (p) => tarjetaDiseno(p, 'Ernesto Vega').getByText('Con cuenta'),
    catalogo: (p) => tarjetaCatalogo(p, 'Ernesto Vega').getByText('Con cuenta'),
  },
  {
    nombre: 'tarjeta de paciente: insignia Invitado',
    diseno: (p) => tarjetaDiseno(p, 'Valeria Núñez').getByText('Invitado'),
    catalogo: (p) => tarjetaCatalogo(p, 'Valeria Núñez').getByText('Invitado'),
  },
  {
    nombre: 'alergia: chip sólido',
    diseno: (p) => tarjetaDiseno(p, 'Ernesto Vega').getByText('Alergia: Penicilina'),
    catalogo: (p) => tarjetaCatalogo(p, 'Ernesto Vega').getByText('Alergia: Penicilina'),
  },
  {
    nombre: 'alergia: ícono',
    diseno: (p) => tarjetaDiseno(p, 'Ernesto Vega').getByText('Alergia: Penicilina').locator('svg'),
    catalogo: (p) => tarjetaCatalogo(p, 'Ernesto Vega').getByText('Alergia: Penicilina').locator('svg'),
    ignorar: ICONO,
  },
  {
    nombre: 'tarjeta de paciente: próxima cita',
    diseno: (p) => tarjetaDiseno(p, 'Ernesto Vega').getByText('Próxima: 18 nov'),
    catalogo: (p) => tarjetaCatalogo(p, 'Ernesto Vega').getByText('Próxima: 18 nov'),
    ignorar: ANCHO,
  },
];

// Excepción (mismo criterio aprobado que la leyenda del odontograma): "Cerrar sesión" declara min-height 52 px
// más 1 px de borde sin border-box y se dibuja de 53; el panel mide 52 reales.
test('excepción: "Cerrar sesión" de la hoja mide 52 px reales (el diseño dibuja 53)', async ({ page, context }) => {
  const disenoPage = await context.newPage();
  await abrirDiseno(disenoPage, 'Movil2.dc.html');
  const alto = await hojaDiseno(disenoPage).getByRole('link', { name: 'Cerrar sesión' }).evaluate((el) => el.getBoundingClientRect().height);
  expect(alto, 'si el diseño ya mide 52, retirar esta excepción').toBe(53);
  await page.goto('/catalogo.html');
  await page.getByRole('button', { name: 'Ver hoja Más' }).click();
  await page.waitForTimeout(400);
  const altoPanel = await hojaCatalogo(page).getByRole('button', { name: 'Cerrar sesión' }).evaluate((el) => el.getBoundingClientRect().height);
  expect(altoPanel).toBe(52);
});

test.describe('Movil2: catálogo frente al diseño', () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const caso of CASOS) {
    test(caso.nombre, async ({ page, context }) => {
      const disenoPage = await context.newPage();
      await disenoPage.setViewportSize({ width: 1800, height: 960 });
      await abrirDiseno(disenoPage, 'Movil2.dc.html');
      const esperado = await estilos(caso.diseno(disenoPage).first(), caso.ignorar ?? []);

      await page.goto('/catalogo.html');
      await page.evaluate(() => document.fonts.ready);
      if (caso.hoja) {
        await page.getByRole('button', { name: 'Ver hoja Más' }).click();
        await hojaCatalogo(page).waitFor();
        await page.waitForTimeout(400); // fin de la subida (280 ms)
      }
      const obtenido = await estilos(caso.catalogo(page).first(), caso.ignorar ?? []);
      expect(obtenido).toEqual(esperado);
    });
  }

  test('el fondo detrás de la hoja es el velo del diseño', async ({ page, context }) => {
    const disenoPage = await context.newPage();
    await abrirDiseno(disenoPage, 'Movil2.dc.html');
    const esperado = await hojaDiseno(disenoPage).locator('xpath=..').evaluate((el) => getComputedStyle(el).backgroundColor);
    await page.goto('/catalogo.html');
    await page.getByRole('button', { name: 'Ver hoja Más' }).click();
    await page.waitForTimeout(400);
    const velo = await hojaCatalogo(page).locator('xpath=../..').evaluate((el) => getComputedStyle(el).backgroundColor);
    expect(velo).toBe(esperado);
  });
});

test('panel: "Más" activo con la hoja abierta, como en el diseño', async ({ page, context }) => {
  const disenoPage = await context.newPage();
  await abrirDiseno(disenoPage, 'Movil2.dc.html');
  const esperado = await estilos(disenoPage.locator('nav a[aria-current="page"]', { hasText: 'Más' }).first(), ANCHO);

  await page.setViewportSize({ width: 390, height: 844 });
  await sesionDeEjemplo(page);
  await page.goto('/');
  await page.getByRole('button', { name: 'Más' }).click();
  await page.getByRole('dialog', { name: 'Más opciones' }).waitFor();
  await page.waitForTimeout(400);
  const obtenido = await estilos(page.getByRole('button', { name: 'Más', includeHidden: true }), ANCHO);
  expect(obtenido).toEqual(esperado);
});
