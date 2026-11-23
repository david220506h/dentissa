import { describe, expect, it } from 'vitest';

// Prueba mínima: confirma que Vitest, jsdom y jest-dom están configurados.
describe('entorno de pruebas', () => {
  it('tiene DOM y los comparadores de jest-dom', () => {
    document.body.innerHTML = '<button type="button">Confirmar cita</button>';
    expect(document.querySelector('button')).toHaveTextContent('Confirmar cita');
  });
});
