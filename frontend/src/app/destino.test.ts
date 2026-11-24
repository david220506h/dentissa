import { describe, expect, it } from 'vitest';
import { destinoSeguro } from './destino';

describe('destino tras iniciar sesión: solo rutas internas', () => {
  it.each([
    ['/agenda', '/agenda'],
    ['/pacientes/42?pestana=datos#alergias', '/pacientes/42?pestana=datos#alergias'],
    ['/', '/'],
  ])('acepta %s', (entrada, salida) => {
    expect(destinoSeguro(entrada)).toBe(salida);
  });

  it.each([
    'https://sitio-malicioso.example/robar',
    '//sitio-malicioso.example',
    '/\\sitio-malicioso.example',
    '\\\\sitio-malicioso.example',
    'javascript:alert(1)',
    'agenda',
    '/agenda\nhttps://otro',
    '/\tsitio',
    '',
    '/iniciar-sesion',
    `/${'a'.repeat(600)}`,
  ])('rechaza %j y vuelve a Inicio', (entrada) => {
    expect(destinoSeguro(entrada)).toBe('/');
  });

  it.each([undefined, null, 42, { pathname: '/agenda' }])('rechaza valores que no son texto (%j)', (entrada) => {
    expect(destinoSeguro(entrada)).toBe('/');
  });
});
