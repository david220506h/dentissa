import { describe, expect, it } from 'vitest';
import {
  citasDelDia,
  deducirHorario,
  diaLargo,
  fechaHoraCorta,
  filasDelDia,
  horariosDeCita,
  rangoCorto,
  rangoLargo,
  semanaDe,
  ubicacion,
  type CitaAgenda,
} from './logica';

// Datos de ejemplo (no reales). Horario de la clínica: 8 a 16 h con descansos de 11 a 12 y de 14 a 15.
const cita = (id: number, fecha: string, hora: string, horas = 1, estado: CitaAgenda['estado'] = 'confirmada', dentistaId = 1): CitaAgenda => {
  const [d, m, a] = fecha.split('/');
  const inicio = new Date(`${a}-${m}-${d}T${hora}:00-06:00`);
  return {
    id,
    paciente: `Paciente ${id}`,
    servicio: 'Limpieza dental',
    dentista: 'Dra. Ejemplo',
    dentistaId,
    pacienteId: id,
    fecha,
    hora,
    inicio: inicio.toISOString(),
    fin: new Date(inicio.getTime() + horas * 3_600_000).toISOString(),
    estado,
  };
};

const TODOS = ['08:00', '09:00', '10:00', '12:00', '13:00', '15:00'];

describe('semana y textos', () => {
  it('lunes a sábado; el domingo pertenece a la semana que termina', () => {
    expect(semanaDe('2026-11-18')).toEqual(['2026-11-16', '2026-11-17', '2026-11-18', '2026-11-19', '2026-11-20', '2026-11-21']);
    expect(semanaDe('2026-11-22')[0]).toBe('2026-11-16');
    expect(semanaDe('2026-11-23')[0]).toBe('2026-11-23');
  });

  it('rangos como en el diseño, también entre meses y años', () => {
    expect(rangoCorto(semanaDe('2026-11-18'))).toBe('16 – 21 nov 2026');
    expect(rangoLargo(semanaDe('2026-11-18'))).toBe('Semana del 16 al 21 de noviembre de 2026');
    expect(rangoCorto(semanaDe('2026-12-02'))).toBe('30 nov – 5 dic 2026');
    expect(rangoLargo(semanaDe('2026-12-02'))).toBe('Semana del 30 de noviembre al 5 de diciembre de 2026');
    expect(rangoCorto(semanaDe('2026-12-30'))).toBe('28 dic 2026 – 2 ene 2027');
    expect(diaLargo('2026-11-18')).toBe('Miércoles 18 de noviembre de 2026');
    expect(fechaHoraCorta('2026-12-02', '09:00')).toBe('Mié 2 dic 2026, 9:00');
  });
});

describe('deducirHorario (TEMPORAL, se reemplaza en la Fase 2)', () => {
  const dias = (libres: string[][]) => libres.map((l, i) => ({ fecha: `2026-11-${String(19 + i).padStart(2, '0')}`, libres: l }));

  it('deduce los dos descansos del horario de la clínica', () => {
    expect(deducirHorario(dias([TODOS, TODOS]), [])).toEqual({ horarios: TODOS, descansos: ['11:00', '14:00'] });
  });

  it('un horario lleno en todos los días no se confunde con descanso: está ocupado por citas', () => {
    const sinLas9 = TODOS.filter((h) => h !== '09:00');
    const citas = [cita(1, '19/11/2026', '09:00'), cita(2, '20/11/2026', '09:00')];
    expect(deducirHorario(dias([sinLas9, sinLas9]), citas)?.descansos).toEqual(['11:00', '14:00']);
  });

  it('una cita de dos horarios cuenta ambos horarios como de atención', () => {
    const sin12y13 = TODOS.filter((h) => h !== '12:00' && h !== '13:00');
    expect(deducirHorario(dias([sin12y13]), [cita(1, '19/11/2026', '12:00', 2)])?.descansos).toEqual(['11:00', '14:00']);
  });

  it('un día cerrado o con la anticipación consumida no inventa descansos: otro día sí tiene esas horas', () => {
    expect(deducirHorario(dias([[], ['12:00', '13:00', '15:00'], TODOS]), [])?.descansos).toEqual(['11:00', '14:00']);
  });

  it('las citas canceladas no cuentan como ocupación', () => {
    const sinLas10 = TODOS.filter((h) => h !== '10:00');
    expect(deducirHorario(dias([sinLas10]), [cita(1, '19/11/2026', '10:00', 1, 'cancelada')])?.descansos).toEqual(['10:00', '11:00', '14:00']);
  });

  it('sin datos (agenda sin configurar o días sin horarios) no deduce nada', () => {
    expect(deducirHorario([], [])).toBeNull();
    expect(deducirHorario(dias([[], []]), [])).toBeNull();
  });
});

describe('cuadrícula', () => {
  const horario = { horarios: TODOS, descansos: ['11:00', '14:00'] };

  it('filas: horarios y descansos en orden, más horas con citas fuera del patrón', () => {
    expect(filasDelDia(horario, []).map((f) => `${f.hora}${f.tipo === 'descanso' ? '*' : ''}`)).toEqual([
      '08:00', '09:00', '10:00', '11:00*', '12:00', '13:00', '14:00*', '15:00',
    ]);
    expect(filasDelDia(horario, [cita(1, '16/11/2026', '07:00')])[0]).toEqual({ tipo: 'horario', hora: '07:00' });
  });

  it('ubicación: una cita de 2 horarios ocupa 2 filas', () => {
    const filas = filasDelDia(horario, []);
    expect(ubicacion(cita(1, '18/11/2026', '12:00', 2), filas)).toEqual({ fila: 4, filas: 2 });
    expect(ubicacion(cita(2, '18/11/2026', '08:00'), filas)).toEqual({ fila: 0, filas: 1 });
    expect(horariosDeCita(cita(3, '18/11/2026', '12:00', 2))).toEqual(['12:00', '13:00']);
  });

  it('citas del día: sin canceladas y del dentista elegido', () => {
    const citas = [cita(1, '18/11/2026', '09:00'), cita(2, '18/11/2026', '08:00', 1, 'pendiente'), cita(3, '18/11/2026', '10:00', 1, 'cancelada'), cita(4, '18/11/2026', '12:00', 1, 'confirmada', 2), cita(5, '19/11/2026', '08:00')];
    expect(citasDelDia(citas, '2026-11-18', 1).map((c) => c.id)).toEqual([2, 1]);
    expect(citasDelDia(citas, '2026-11-18', null).map((c) => c.id)).toEqual([2, 1, 4]);
  });
});
