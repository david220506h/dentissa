import { describe, expect, it } from 'vitest';
import { accionesDe, alternativas, erroresAltaRapida, horaApi, motivoCancelacion, rangoHoras, type HorarioLibre } from './logica';

const INICIO = '2026-11-18T15:00:00.000Z'; // 9:00 en la clínica
const antes = new Date('2026-11-18T14:30:00Z');
const a10 = new Date('2026-11-18T15:10:00Z');
const a15 = new Date('2026-11-18T15:15:00Z');

describe('accionesDe', () => {
  it('recepción: confirmar una pendiente, cancelar, reagendar y cambiar de dentista antes del inicio', () => {
    expect(accionesDe({ estado: 'pendiente', inicio: INICIO }, 'asistente', antes)).toEqual({
      confirmar: true, cancelar: true, reagendar: true, cambiarDentista: true, completar: false, noAsistio: false,
    });
  });

  it('completar desde el inicio; no asistió desde inicio + 15 min; ya no se cambia de dentista', () => {
    const a = accionesDe({ estado: 'confirmada', inicio: INICIO }, 'admin', a10);
    expect([a.confirmar, a.completar, a.noAsistio, a.cambiarDentista]).toEqual([false, true, false, false]);
    expect(accionesDe({ estado: 'confirmada', inicio: INICIO }, 'admin', a15).noAsistio).toBe(true);
  });

  it('el dentista solo completa o marca inasistencia', () => {
    const a = accionesDe({ estado: 'confirmada', inicio: INICIO }, 'dentista', a15);
    expect(a).toEqual({ confirmar: false, cancelar: false, reagendar: false, cambiarDentista: false, completar: true, noAsistio: true });
  });

  it('finales (completada, cancelada, no asistió): sin acciones', () => {
    for (const estado of ['completada', 'cancelada', 'no_asistio'] as const)
      expect(Object.values(accionesDe({ estado, inicio: INICIO }, 'admin', a15)).some(Boolean)).toBe(false);
  });
});

describe('formularios', () => {
  const libre = (hora: string): HorarioLibre => ({ hora, inicio: '', fin: '', dentistasLibres: [] });

  it('alternativas tras un 409: sin el horario intentado y hasta 3', () => {
    expect(alternativas(['09:00', '10:00', '12:00', '13:00', '15:00'].map(libre), '09:00')).toEqual(['10:00', '12:00', '13:00']);
    expect(alternativas([libre('09:00')], '09:00')).toEqual([]);
  });

  it('motivo de cancelación con nota opcional', () => {
    expect(motivoCancelacion('Otro', '  ')).toBe('Otro');
    expect(motivoCancelacion('Otro', 'Viaje')).toBe('Otro. Viaje');
  });

  it('rango de horas de una cita de varios horarios', () => {
    expect(rangoHoras('12:00', 120)).toBe('12:00 a 14:00');
    expect(rangoHoras('09:00', 60)).toBe('9:00');
    expect(rangoHoras('08:00', 120)).toBe('8:00 a 10:00');
  });

  it('alta rápida con la regla del backend', () => {
    expect(erroresAltaRapida({ nombre: '', apellido: 'X', telefono: '55 1234 5678' })).toEqual({ nombre: 'Escribe el nombre.' });
    expect(erroresAltaRapida({ nombre: 'A', apellido: 'B', telefono: '123' }).telefono).toBeDefined();
    expect(erroresAltaRapida({ nombre: 'A', apellido: 'B', telefono: '(55) 1234-5678' })).toEqual({});
  });

  it('hora para la API', () => {
    expect(horaApi('09:00')).toBe('09:00:00');
    expect(horaApi('09:00:00')).toBe('09:00:00');
  });
});
