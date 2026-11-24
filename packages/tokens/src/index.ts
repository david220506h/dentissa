// Valores del sistema visual que también se necesitan en TypeScript.
// Los colores y medidas viven solo en tokens.css; aquí solo lo que CSS no puede expresar
// (los puntos de quiebre no se pueden usar como variables dentro de @media).

/** Ancho desde el que se muestra el menú lateral (decisión 14.2 del plan de la Fase 3a). */
export const ANCHO_MENU_LATERAL = 1024;

/** Ancho mínimo soportado. */
export const ANCHO_MINIMO = 360;

/** Tamaño de letra mínimo permitido, en px (decisión 3 del plan de la Fase 3a). */
export const TEXTO_MINIMO_PX = 12.5;

/** Consulta de movimiento reducido. */
export const CONSULTA_MOVIMIENTO_REDUCIDO = '(prefers-reduced-motion: reduce)';
