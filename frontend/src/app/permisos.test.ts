import { describe, expect, it } from 'vitest';
import { SECCIONES, esRolPanel, puedeVer, seccionesDe } from './permisos';
import { seccionActiva } from './Disposicion';

const ids = (rol: Parameters<typeof seccionesDe>[0]) => seccionesDe(rol).map((s) => s.id);

describe('matriz de permisos (Main.dc.html)', () => {
  it('administración ve las 13 secciones', () => {
    expect(ids('admin')).toEqual(SECCIONES.map((s) => s.id));
    expect(SECCIONES).toHaveLength(13);
  });

  it('asistente: sin expediente, servicios, ofertas, reportes ni administración', () => {
    expect(ids('asistente')).toEqual(['inicio', 'agenda', 'solicitudes', 'pacientes', 'inventario', 'pagos']);
  });

  it('dentista: Inicio y Pacientes por ahora (decisión 14.2), aunque la matriz le dé más', () => {
    expect(ids('dentista')).toEqual(['inicio', 'pacientes']);
    expect(puedeVer('dentista', 'agenda')).toBe(false);
    expect(puedeVer('dentista', 'expediente')).toBe(false);
  });

  it('solo el personal entra al panel', () => {
    expect(esRolPanel('admin')).toBe(true);
    expect(esRolPanel('asistente')).toBe(true);
    expect(esRolPanel('dentista')).toBe(true);
    expect(esRolPanel('paciente')).toBe(false);
    expect(esRolPanel(undefined)).toBe(false);
    expect(esRolPanel('ADMIN')).toBe(false);
  });
});

describe('sección activa', () => {
  it('"/" solo marca Inicio en la raíz', () => {
    expect(seccionActiva('/')).toBe('inicio');
    expect(seccionActiva('/agenda')).toBe('agenda');
    expect(seccionActiva('/pacientes/42')).toBe('pacientes');
    expect(seccionActiva('/agendas')).toBeUndefined();
  });
});
