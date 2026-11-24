import { expect, test, type Locator, type Page } from '@playwright/test';
import { abrirDiseno } from './utilidades';

// Componentes de Estados (catálogo) frente a Estados.dc.html, con estilos calculados en el mismo navegador.

const PROPIEDADES = [
  'font-size',
  'font-weight',
  'color',
  'background-color',
  'border-top-width',
  'border-top-color',
  'border-radius',
  'box-shadow',
  'padding',
  'margin',
  'width',
  'height',
  'max-width',
  'gap',
  'stroke',
  'stroke-width',
] as const;
type Propiedad = (typeof PROPIEDADES)[number];

async function estilos(objetivo: Locator, ignorar: Propiedad[]) {
  await objetivo.waitFor();
  const props = await objetivo.evaluate((el, lista) => {
    const cs = getComputedStyle(el);
    const caja = el.getBoundingClientRect();
    return Object.fromEntries(
      lista.map((p) => [
        p,
        p === 'width' ? `${Math.round(caja.width * 100) / 100}px` : p === 'height' ? `${Math.round(caja.height * 100) / 100}px` : cs.getPropertyValue(p),
      ]),
    );
  }, [...PROPIEDADES]);
  for (const p of ignorar) delete props[p];
  return props;
}

type Caso = {
  nombre: string;
  diseno: (p: Page) => Locator;
  catalogo: (p: Page) => Locator;
  /** Propiedades que dependen del contenedor de muestra, no del componente (razón en cada caso). */
  ignorar?: Propiedad[];
  /** Abrir el diálogo de muestra antes de medir. */
  dialogo?: boolean;
};

// El ancho de los bloques de texto y del diálogo depende de la tarjeta que los contiene
const ANCHO: Propiedad[] = ['width'];
const TEXTO: Propiedad[] = ['width', 'height'];
// Caja sin texto propio: la letra y el color heredados no se ven (el diseño parte de 16 px y el panel de 15).
// El color del ícono se compara aparte, en su trazo.
const CAJA: Propiedad[] = ['font-size', 'font-weight', 'color', 'border-top-color'];
// Radio de al menos la mitad del alto = píldora o círculo (regla aprobada): 18 y 13 px en el diseño, --radio-pildora en el panel
const PILDORA: Propiedad[] = ['border-radius'];

const enEstados = (p: Page) => p.locator('section[aria-labelledby="sec-Estados"]');

const CASOS: Caso[] = [
  {
    nombre: 'vacío: círculo del ícono',
    diseno: (p) => p.getByText('No hay citas este día').locator('xpath=preceding-sibling::span[1]'),
    catalogo: (p) => enEstados(p).getByText('No hay citas este día').locator('xpath=preceding-sibling::span[1]'),
    ignorar: CAJA,
  },
  {
    nombre: 'vacío: ícono (trazo y tamaño)',
    diseno: (p) => p.getByText('No hay citas este día').locator('xpath=preceding-sibling::span[1]/*[local-name()="svg"]'),
    catalogo: (p) => enEstados(p).getByText('No hay citas este día').locator('xpath=preceding-sibling::span[1]//*[local-name()="svg"]'),
    ignorar: ['font-size', 'color', 'border-top-color'],
  },
  {
    nombre: 'vacío: contenedor',
    diseno: (p) => p.getByText('No hay citas este día').locator('xpath=..'),
    catalogo: (p) => enEstados(p).getByText('No hay citas este día').locator('xpath=..'),
    ignorar: [...TEXTO, ...CAJA],
  },
  {
    nombre: 'vacío: título',
    diseno: (p) => p.getByText('No hay citas este día'),
    catalogo: (p) => enEstados(p).getByText('No hay citas este día'),
    ignorar: TEXTO,
  },
  {
    nombre: 'vacío: texto',
    diseno: (p) => p.getByText('Es un día sin citas.'),
    catalogo: (p) => enEstados(p).getByText('Es un día sin citas.'),
    ignorar: TEXTO,
  },
  {
    nombre: 'vacío: botón Nueva cita',
    diseno: (p) => p.getByRole('button', { name: 'Nueva cita' }),
    catalogo: (p) => enEstados(p).getByRole('button', { name: 'Nueva cita' }),
  },
  {
    nombre: 'error: botón Reintentar (sin el ícono, ver la excepción)',
    diseno: (p) => p.getByRole('button', { name: 'Reintentar' }),
    catalogo: (p) => enEstados(p).getByRole('button', { name: 'Reintentar' }),
  },
  {
    nombre: 'sin permiso: botón Volver al inicio',
    diseno: (p) => p.getByRole('button', { name: 'Volver al inicio' }),
    catalogo: (p) => enEstados(p).getByRole('button', { name: 'Volver al inicio' }),
  },
  ...[
    ['avatar', 0],
    ['línea larga', 1],
    ['línea corta', 2],
    ['insignia', 3],
  ].map<Caso>(([n, i]) => ({
    nombre: `cargando: ${n}`,
    diseno: (p) => p.locator('[role="status"][aria-label="Cargando citas"] span[aria-hidden="true"]').nth(i as number),
    catalogo: (p) => p.locator('[role="status"][aria-label="Cargando citas"] span[aria-hidden="true"]').nth(i as number),
    // Las líneas se miden en % del contenedor
    ignorar: [...CAJA, ...(n === 'línea larga' || n === 'línea corta' ? ANCHO : PILDORA)],
  })),
  {
    nombre: 'aviso de éxito: caja',
    diseno: (p) => p.getByText('Cita confirmada. Se envió el correo a Rosa Aguilar.').locator('xpath=..'),
    catalogo: (p) => enEstados(p).getByText('Cita confirmada. Se envió el correo a Rosa Aguilar.').locator('xpath=../..'),
    // El peso va en el texto (se compara en su propio caso), no en la caja
    ignorar: [...TEXTO, 'font-weight'],
  },
  {
    nombre: 'aviso de éxito: texto',
    diseno: (p) => p.getByText('Cita confirmada. Se envió el correo a Rosa Aguilar.'),
    catalogo: (p) => enEstados(p).getByText('Cita confirmada. Se envió el correo a Rosa Aguilar.'),
    ignorar: TEXTO,
  },
  {
    nombre: 'aviso de advertencia: caja (sin cerrar)',
    diseno: (p) => p.getByText('Hay 2 solicitudes por vencer').locator('xpath=..'),
    catalogo: (p) => enEstados(p).getByText('Hay 2 solicitudes por vencer').locator('xpath=../..'),
    // El peso va en el texto (se compara en su propio caso), no en la caja
    ignorar: [...TEXTO, 'font-weight'],
  },
  {
    nombre: 'aviso: botón Cerrar aviso',
    diseno: (p) => p.getByRole('button', { name: 'Cerrar aviso' }).first(),
    catalogo: (p) => enEstados(p).getByRole('button', { name: 'Cerrar aviso' }).first(),
  },
  {
    nombre: 'diálogo: caja',
    diseno: (p) => p.locator('section[role="dialog"]'),
    catalogo: (p) => p.getByRole('dialog'),
    dialogo: true,
    // En el diseño mide lo que su tarjeta de muestra; en el panel, hasta 420 px
    ignorar: [...TEXTO, 'max-width', 'font-size'],
  },
  {
    nombre: 'diálogo: título',
    diseno: (p) => p.locator('section[role="dialog"] h2'),
    catalogo: (p) => p.getByRole('dialog').getByRole('heading'),
    dialogo: true,
    ignorar: TEXTO,
  },
  {
    nombre: 'diálogo: círculo del ícono',
    diseno: (p) => p.locator('section[role="dialog"] h2').locator('xpath=../preceding-sibling::span[1]'),
    catalogo: (p) => p.getByRole('dialog').getByRole('heading').locator('xpath=preceding-sibling::span[1]'),
    dialogo: true,
    ignorar: CAJA,
  },
  {
    nombre: 'diálogo: ícono (trazo y tamaño)',
    diseno: (p) => p.locator('section[role="dialog"] h2').locator('xpath=../preceding-sibling::span[1]/*[local-name()="svg"]'),
    catalogo: (p) => p.getByRole('dialog').getByRole('heading').locator('xpath=preceding-sibling::span[1]//*[local-name()="svg"]'),
    dialogo: true,
    ignorar: ['font-size', 'color', 'border-top-color'],
  },
  {
    nombre: 'diálogo: botón Cerrar',
    diseno: (p) => p.locator('section[role="dialog"] button[aria-label="Cerrar"]'),
    catalogo: (p) => p.getByRole('dialog').getByRole('button', { name: 'Cerrar', exact: true }),
    dialogo: true,
  },
  {
    nombre: 'diálogo: texto',
    diseno: (p) => p.locator('section[role="dialog"] p'),
    catalogo: (p) => p.getByRole('dialog').locator('p'),
    dialogo: true,
    ignorar: TEXTO,
  },
  {
    nombre: 'diálogo: Mantener sesión',
    diseno: (p) => p.locator('section[role="dialog"]').getByRole('button', { name: 'Mantener sesión' }),
    catalogo: (p) => p.getByRole('dialog').getByRole('button', { name: 'Mantener sesión' }),
    dialogo: true,
  },
  {
    nombre: 'diálogo: Cerrar sesión',
    diseno: (p) => p.locator('section[role="dialog"]').getByRole('button', { name: 'Cerrar sesión' }),
    catalogo: (p) => p.getByRole('dialog').getByRole('button', { name: 'Cerrar sesión' }),
    dialogo: true,
  },
];

// Excepción aprobada: "Reintentar" lleva la flecha circular; Estados.dc.html aún dibuja un reloj y el
// próximo zip lo corrige. Si el diseño ya trae la flecha, esta prueba falla para retirar la excepción.
test('excepción: "Reintentar" con flecha circular en lugar del reloj del diseño', async ({ page, context }) => {
  const disenoPage = await context.newPage();
  await abrirDiseno(disenoPage, 'Estados.dc.html');
  const trazoDiseno = await disenoPage.getByRole('button', { name: 'Reintentar' }).locator('svg path').first().getAttribute('d');
  expect(trazoDiseno, 'el diseño todavía dibuja el reloj').toBe('M12 6v6l4 2');
  await page.goto('/catalogo.html');
  const trazo = await page.locator('section[aria-labelledby="sec-Estados"]').getByRole('button', { name: 'Reintentar' }).locator('svg path').first().getAttribute('d');
  expect(trazo).toBe('M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8');
});

test.describe('Estados: catálogo frente al diseño', () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const caso of CASOS) {
    test(caso.nombre, async ({ page, context }) => {
      const disenoPage = await context.newPage();
      await abrirDiseno(disenoPage, 'Estados.dc.html');
      const esperado = await estilos(caso.diseno(disenoPage).first(), caso.ignorar ?? []);

      await page.goto('/catalogo.html');
      await page.evaluate(() => document.fonts.ready);
      if (caso.dialogo) {
        await page.getByRole('button', { name: 'Ver aviso de inactividad' }).click();
        await page.getByRole('dialog').waitFor();
        // Espera a que termine la entrada (200 ms) para medir el estado final
        await page.waitForTimeout(300);
      }
      const obtenido = await estilos(caso.catalogo(page).first(), caso.ignorar ?? []);
      expect(obtenido).toEqual(esperado);
    });
  }
});
