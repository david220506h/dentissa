/** El panel del personal vive bajo /panel; el resto del sitio es público. */
export const BASE_PANEL = '/panel';

export const esRutaDelPanel = (ruta: string) => ruta === BASE_PANEL || ruta.startsWith(`${BASE_PANEL}/`);
