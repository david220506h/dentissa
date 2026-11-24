/**
 * Adónde volver tras iniciar sesión. Solo rutas internas del panel: cualquier otra cosa (otra dirección,
 * "//sitio", "/\sitio", "javascript:", texto raro o vacío) vuelve a Inicio. El valor llega de
 * location.state, que escribe el propio panel, pero es un dato del navegador y se valida igual.
 */
export function destinoSeguro(valor: unknown): string {
  if (typeof valor !== 'string' || valor.length === 0 || valor.length > 512) return '/';
  // Debe empezar con una sola "/" y no llevar barras invertidas ni caracteres de control
  if (!valor.startsWith('/') || valor.startsWith('//') || /[\\\u0000-\u001f\u007f]/.test(valor)) return '/';
  // Comprobación final: resuelta contra una base cualquiera, debe seguir en el mismo origen
  const base = 'https://panel.invalid';
  let url: URL;
  try {
    url = new URL(valor, base);
  } catch {
    return '/';
  }
  if (url.origin !== base) return '/';
  // Volver al inicio de sesión no tiene sentido
  if (url.pathname === '/iniciar-sesion') return '/';
  return `${url.pathname}${url.search}${url.hash}`;
}
