import { describe, expect, it } from 'vitest';
import { cuerpoDe, erroresFormulario, fechaCorta, FORMULARIO_VACIO, rangoPagina, terminoValido } from './logica';

const HOY = '2026-11-24';

describe('Pacientes: lógica', () => {
  it('fecha corta en español', () => {
    expect(fechaCorta('2026-12-04')).toBe('4 dic 2026');
    expect(fechaCorta('2027-01-15')).toBe('15 ene 2027');
  });

  it('rango de la página', () => {
    expect(rangoPagina(1, 10, 31, 10)).toEqual({ desde: 1, hasta: 10, hayAnterior: false, haySiguiente: true });
    expect(rangoPagina(4, 10, 31, 1)).toEqual({ desde: 31, hasta: 31, hayAnterior: true, haySiguiente: false });
    expect(rangoPagina(1, 10, 0, 0)).toEqual({ desde: 0, hasta: 0, hayAnterior: false, haySiguiente: false });
  });

  it('la búsqueda se envía con 2 caracteres o más', () => {
    expect(terminoValido(' a ')).toBe('');
    expect(terminoValido(' Ro ')).toBe('Ro');
  });

  it('validación igual que el backend', () => {
    expect(erroresFormulario(FORMULARIO_VACIO, HOY)).toEqual({
      nombre: 'Escribe el nombre.',
      apellido: 'Escribe el apellido.',
      telefono: 'Indica al menos un teléfono o un correo.',
    });
    const base = { ...FORMULARIO_VACIO, nombre: 'Rosa', apellido: 'Aguilar' };
    expect(erroresFormulario({ ...base, telefono: '55 1234' }, HOY).telefono).toBe('El teléfono debe tener de 10 a 15 dígitos.');
    expect(erroresFormulario({ ...base, telefono: '(55) 1234-5678' }, HOY)).toEqual({});
    expect(erroresFormulario({ ...base, correo: 'rosa@' }, HOY).correo).toBe('Escribe un correo válido.');
    expect(erroresFormulario({ ...base, correo: 'rosa@ejemplo.test' }, HOY)).toEqual({});
    expect(erroresFormulario({ ...base, correo: 'r@e.test', fechaNacimiento: '2027-01-01' }, HOY).fechaNacimiento).toBeDefined();
    expect(erroresFormulario({ ...base, correo: 'r@e.test', alergias: 'x'.repeat(1001) }, HOY).alergias).toBeDefined();
  });

  it('cuerpo: recorta y manda vacíos como null', () => {
    expect(cuerpoDe({ ...FORMULARIO_VACIO, nombre: ' Rosa ', apellido: 'Aguilar', telefono: ' 5512345678 ', alergias: '  ' })).toEqual({
      nombre: 'Rosa',
      apellido: 'Aguilar',
      telefono: '5512345678',
      correo: null,
      fechaNacimiento: null,
      sexo: null,
      alergias: null,
    });
  });
});
