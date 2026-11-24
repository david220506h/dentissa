// Compara los estilos calculados de cada componente del catálogo con el mismo componente en
// docs/design/panel/Main.dc.html, en el mismo navegador y con la misma fuente.
import { expect, test, type Page } from '@playwright/test';
import { abrirDiseno } from './utilidades';

const PROPIEDADES = [
  'backgroundColor',
  'color',
  'borderTopColor',
  'borderTopWidth',
  'borderTopStyle',
  'height',
  'borderTopLeftRadius',
  'fontSize',
  'fontWeight',
  'paddingLeft',
  'paddingRight',
  'fontFamily',
] as const;
type Estilo = Record<(typeof PROPIEDADES)[number], string>;

/** Busca el primer elemento cuyo texto propio (sin hijos) es `texto` y sube hasta `ancestro`. */
async function estiloDe(page: Page, texto: string, ancestro: string, indice = 0): Promise<Estilo> {
  return page.evaluate(
    ({ texto, ancestro, indice, propiedades }) => {
      const candidatos = [...document.querySelectorAll('body *')].filter((el) => {
        const propio = [...el.childNodes]
          .filter((n) => n.nodeType === Node.TEXT_NODE)
          .map((n) => n.textContent ?? '')
          .join('')
          .trim();
        if (propio !== texto) return false;
        // Con selector, solo cuentan los que están dentro de ese tipo de elemento
        return ancestro.startsWith('^') || ancestro === '.' || el.closest(ancestro) !== null;
      });
      const base = candidatos[indice];
      if (!base) throw new Error(`No se encontró "${texto}"`);
      // '^n' sube n niveles; si no, el ancestro más cercano que cumpla el selector
      let el: Element | null = base;
      if (ancestro.startsWith('^')) for (let i = 0; i < Number(ancestro.slice(1)); i++) el = el?.parentElement ?? null;
      else if (ancestro !== '.') el = base.closest(ancestro);
      if (!el) throw new Error(`"${texto}" no está dentro de ${ancestro}`);
      const cs = getComputedStyle(el);
      return Object.fromEntries(
        propiedades.map((p) => {
          if (p === 'fontFamily') return [p, cs.fontFamily.split(',')[0]!.replace(/['"]/g, '').trim()];
          // Alto dibujado, no la propiedad CSS: el diseño no fija box-sizing y el panel usa border-box
          // Redondeo a centésimas: el navegador a veces mide 62.0001 px por subpíxeles
          if (p === 'height') return [p, `${Math.round(el.getBoundingClientRect().height * 100) / 100}px`];
          return [p, cs[p]];
        }),
      ) as Estilo;
    },
    { texto, ancestro, indice, propiedades: [...PROPIEDADES] },
  );
}

interface Caso {
  nombre: string;
  /** Pantalla del diseño (por defecto Main.dc.html). */
  archivo?: string;
  diseno: [texto: string, ancestro: string, indice?: number];
  catalogo: [texto: string, ancestro: string, indice?: number];
  /** Diferencias aprobadas (p. ej., el mínimo de 12.5 px de la decisión 3). */
  excepciones?: Partial<Estilo>;
  ignorar?: Array<(typeof PROPIEDADES)[number]>;
}

const CASOS: Caso[] = [
  { nombre: 'botón principal', diseno: ['Confirmar cita', 'button'], catalogo: ['Confirmar cita', 'button'] },
  { nombre: 'botón secundario', diseno: ['Proponer otro horario', 'button'], catalogo: ['Proponer otro horario', 'button'] },
  { nombre: 'botón de peligro', diseno: ['Rechazar solicitud', 'button'], catalogo: ['Rechazar solicitud', 'button'] },
  { nombre: 'botón de texto', diseno: ['Ver agenda', 'button'], catalogo: ['Ver agenda', 'button'] },
  { nombre: 'botón deshabilitado', diseno: ['Sin teléfono', 'button'], catalogo: ['Sin teléfono', 'button'] },
  ...['Confirmada', 'Pendiente', 'Completada', 'Cancelada', 'No asistió'].map<Caso>((t) => ({
    nombre: `insignia ${t}`,
    diseno: [t, 'span'],
    catalogo: [t, 'span'],
  })),
  { nombre: 'insignia Con cuenta', diseno: ['Con cuenta', 'span'], catalogo: ['Con cuenta', 'span'] },
  { nombre: 'insignia Invitado', diseno: ['Invitado', 'span'], catalogo: ['Invitado', 'span'] },
  { nombre: 'insignia de alergia', diseno: ['Penicilina', 'span'], catalogo: ['Penicilina', 'span'] },
  { nombre: 'etiqueta de campo', diseno: ['Correo electrónico', 'label'], catalogo: ['Correo electrónico', 'label'] },
  { nombre: 'mensaje de error de campo', diseno: ['Escribe un correo válido.', 'div'], catalogo: ['Escribe un correo válido.', 'div'] },
  ...[
    ['Información', 'info'],
    ['Cita confirmada', 'éxito'],
    ['Horario bloqueado', 'advertencia'],
    ['No se pudo guardar', 'error'],
  ].map<Caso>(([t, tono]) => ({
    nombre: `aviso de ${tono}`,
    // El título está dentro de un div; el aviso (caja con fondo) está dos niveles arriba
    diseno: [t!, '^2'],
    catalogo: [t!, '^2'],
  })),
  { nombre: 'elemento del menú activo', diseno: ['Activo', 'a'], catalogo: ['Inicio', 'a[aria-current="page"]'] },
  { nombre: 'elemento del menú normal', diseno: ['Normal', 'a'], catalogo: ['Agenda', 'nav[aria-label="Menú principal"] a'] },
  {
    nombre: 'contador del menú',
    diseno: ['5', 'span'],
    catalogo: ['5', 'span[class*="contador"]'],
    excepciones: { fontSize: '12.5px' },
  },
  // Odontograma frente a Expediente.dc.html (piezas no seleccionadas, para no medir el anillo)
  ...[
    ['17', 'sana'],
    ['35', 'con caries'],
    ['16', 'restaurada'],
    ['46', 'con endodoncia'],
    ['11', 'con corona'],
    ['18', 'ausente'],
  ].map<Caso>(([n, estado]) => ({
    nombre: `pieza ${estado} (${n})`,
    archivo: 'Expediente.dc.html',
    diseno: [n!, 'button'],
    catalogo: [n!, 'button'],
  })),
  ...['Sano', 'Caries', 'Restaurado', 'Endodoncia', 'Corona', 'Ausente'].map<Caso>((t) => ({
    nombre: `leyenda ${t}`,
    archivo: 'Expediente.dc.html',
    diseno: [t, 'li'],
    catalogo: [t, 'li'],
    // El diseño declara 30 px sin border-box y se dibuja de 32; el arquitecto aprobó 30 reales
    excepciones: { height: '30px' },
  })),
];

test.describe('estilos del catálogo frente al diseño', () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const caso of CASOS) {
    test(caso.nombre, async ({ page, context }) => {
      const disenoPage = await context.newPage();
      await abrirDiseno(disenoPage, caso.archivo ?? 'Main.dc.html');
      const esperado = await estiloDe(disenoPage, ...caso.diseno);

      await page.goto('/catalogo.html');
      await page.evaluate(() => document.fonts.ready);
      const obtenido = await estiloDe(page, ...caso.catalogo);

      const esperadoConExcepciones = { ...esperado, ...caso.excepciones };
      for (const p of caso.ignorar ?? []) {
        delete (esperadoConExcepciones as Partial<Estilo>)[p];
        delete (obtenido as Partial<Estilo>)[p];
      }
      expect(obtenido).toEqual(esperadoConExcepciones);
    });
  }
});
