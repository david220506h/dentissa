import { Aviso, Boton, CargandoLista, GrupoVista, Icono, InsigniaEstadoCita, MensajeEstado, ESTADOS_CITA } from '@dentissa/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { enviarJson, obtenerJson } from '../../app/api';
import { hoyIsoEnClinica, horaCorta } from '../inicio/datos';
import {
  citasDelDia,
  deducirHorario,
  diaCorto,
  diaLargo,
  duracionMinutos,
  filasDelDia,
  horariosDeCita,
  nombreDentista,
  numeroDia,
  rangoCorto,
  rangoLargo,
  semanaDe,
  sumarDias,
  ubicacion,
  type CitaAgenda,
  type Dentista,
} from './logica';
import { DialogoCancelar, DialogoCita } from './DialogosCita';
import estilos from './Agenda.module.css';

type Vista = 'semana' | 'dia';

// Días que se consultan para deducir el horario (TEMPORAL; ver deducirHorario)
const DIAS_MUESTRA = 7;

const mensajeDe = (e: unknown) => (e instanceof Error ? e.message : 'No se pudo completar la acción. Inténtalo de nuevo.');

/**
 * Agenda (Agenda.dc.html en escritorio; Movil.dc.html · Agenda del día en celular y en la vista Día).
 * Con la API actual: citas (GET /api/citas, filtradas por fecha en el cliente), dentistas, horarios libres,
 * confirmar y cancelar. Los descansos se deducen (TEMPORAL). Sin "Nueva cita", "Agendar", "Mes", buscador
 * ni notificaciones; completar, no asistió, reagendar e historial llegan con la Fase 2.
 */
export function Agenda() {
  const queryClient = useQueryClient();
  const [ahora] = useState(() => new Date());
  const hoy = hoyIsoEnClinica(ahora);
  const [vista, setVista] = useState<Vista>('semana');
  const [fecha, setFecha] = useState(hoy);
  const [dentistaElegido, setDentistaElegido] = useState<number | null>(null);
  const [citaAbierta, setCitaAbierta] = useState<CitaAgenda | null>(null);
  const [citaACancelar, setCitaACancelar] = useState<CitaAgenda | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const citas = useQuery({ queryKey: ['citas'], queryFn: () => obtenerJson<CitaAgenda[]>('/citas') });
  const dentistas = useQuery({ queryKey: ['dentistas'], queryFn: () => obtenerJson<Dentista[]>('/citas/dentistas'), staleTime: 10 * 60_000 });
  // TEMPORAL: muestra de horarios libres desde mañana para deducir horarios y descansos (Fase 2 lo reemplaza)
  const muestra = useQuery({
    queryKey: ['horario-deducido', hoy],
    queryFn: () => {
      const dias = Array.from({ length: DIAS_MUESTRA }, (_, i) => sumarDias(hoy, i + 1));
      return Promise.all(dias.map(async (f) => ({ fecha: f, libres: await obtenerJson<string[]>(`/citas/horarios-disponibles?fecha=${f}`) })));
    },
    staleTime: 60 * 60_000,
    retry: false,
  });

  // Dentista: el elegido o, si no hay elección, el primero (una cita por dentista y horario)
  const dentistaId = dentistaElegido ?? dentistas.data?.[0]?.id ?? null;
  const libres = useQuery({
    queryKey: ['libres', fecha, dentistaId],
    queryFn: () => obtenerJson<string[]>(`/citas/horarios-disponibles?fecha=${fecha}${dentistaId ? `&dentistaId=${dentistaId}` : ''}`),
    enabled: fecha >= hoy && !muestra.isError,
    retry: false,
  });

  const horario = useMemo(() => (muestra.data && citas.data ? deducirHorario(muestra.data, citas.data) : null), [muestra.data, citas.data]);

  const confirmar = useMutation({
    mutationFn: (c: CitaAgenda) => enviarJson(`/citas/${c.id}/confirmar`, 'PATCH'),
    onSuccess: async () => {
      setCitaAbierta(null);
      setAviso('Cita confirmada.');
      await queryClient.invalidateQueries({ queryKey: ['citas'] });
    },
  });
  const cancelar = useMutation({
    mutationFn: ({ cita, motivo }: { cita: CitaAgenda; motivo: string }) => enviarJson(`/citas/${cita.id}/cancelar`, 'PATCH', { motivo }),
    onSuccess: async () => {
      setCitaACancelar(null);
      setCitaAbierta(null);
      setAviso('Cita cancelada.');
      await queryClient.invalidateQueries({ queryKey: ['citas'] });
      await queryClient.invalidateQueries({ queryKey: ['libres'] });
    },
  });

  const semana = semanaDe(fecha);
  const paso = vista === 'dia' ? 1 : 7;
  const mover = (dias: number) => setFecha((f) => sumarDias(f, dias));
  const abrir = (c: CitaAgenda) => {
    confirmar.reset();
    setAviso(null);
    setCitaAbierta(c);
  };

  return (
    <div className={estilos.pagina}>
      <header>
        <h1 className={estilos.titulo}>Agenda</h1>
        <p className={estilos.subtitulo}>{vista === 'dia' ? diaLargo(fecha) : rangoLargo(semana)}</p>
      </header>

      {aviso && (
        <Aviso tono="exito" anunciar alCerrar={() => setAviso(null)}>
          {aviso}
        </Aviso>
      )}

      <section className={estilos.barra} aria-label="Controles de la agenda">
        <div className={estilos.navegacion}>
          <button type="button" className={estilos.botonIcono} aria-label={paso === 1 ? 'Día anterior' : 'Semana anterior'} onClick={() => mover(-paso)}>
            <Icono nombre="anterior" />
          </button>
          <button type="button" className={estilos.botonIcono} aria-label={paso === 1 ? 'Día siguiente' : 'Semana siguiente'} onClick={() => mover(paso)}>
            <Icono nombre="siguiente" />
          </button>
          <Boton variante="secundario" onClick={() => setFecha(hoy)}>
            Hoy
          </Boton>
          <span className={estilos.rango} aria-live="polite">
            {vista === 'dia' ? diaLargo(fecha) : rangoCorto(semana)}
          </span>
        </div>
        <div className={estilos.filtros}>
          <div className={estilos.soloEscritorio}>
            <GrupoVista
              etiqueta="Vista"
              opciones={[
                { valor: 'dia', texto: 'Día' },
                { valor: 'semana', texto: 'Semana' },
              ]}
              valor={vista}
              alCambiar={setVista}
            />
          </div>
          {dentistas.data && dentistas.data.length > 0 && (
            <label className={estilos.dentista}>
              Dentista
              <select value={dentistaId ?? ''} onChange={(e) => setDentistaElegido(Number(e.target.value))}>
                {dentistas.data.map((d) => (
                  <option key={d.id} value={d.id}>
                    {nombreDentista(d)}
                  </option>
                ))}
              </select>
            </label>
          )}
        </div>
      </section>

      {muestra.isError && (
        <Aviso tono="advertencia">
          No se pudo leer el horario de la clínica. La agenda muestra solo las horas que tienen citas.
        </Aviso>
      )}

      {citas.isPending && (
        <section className={estilos.tarjeta}>
          <CargandoLista etiqueta="Cargando citas" filas={4} />
        </section>
      )}
      {citas.isError && (
        <section className={estilos.tarjeta}>
          <MensajeEstado
            nivel={2}
            icono="sinConexion"
            titulo="No pudimos cargar las citas"
            texto="Revisa tu conexión con la clínica e inténtalo de nuevo. No se perdió ningún dato."
            accion={
              <Boton variante="principal" icono="reintentar" onClick={() => void citas.refetch()} cargando={citas.isFetching}>
                Reintentar
              </Boton>
            }
          />
        </section>
      )}

      {citas.data && (
        <>
          {vista === 'semana' && (
            <div className={estilos.soloEscritorio}>
              <Semana dias={semana} hoy={hoy} citas={citas.data} dentistaId={dentistaId} horario={horario} alAbrir={abrir} />
            </div>
          )}
          <div className={vista === 'semana' ? estilos.soloCelular : undefined}>
            <Dia
              dias={semana}
              fecha={fecha}
              hoy={hoy}
              alElegirDia={setFecha}
              citas={citas.data}
              dentistaId={dentistaId}
              horario={horario}
              libres={fecha >= hoy ? (libres.data ?? []) : []}
              alAbrir={abrir}
            />
          </div>
        </>
      )}

      <DialogoCita
        cita={citaAbierta}
        alCerrar={() => setCitaAbierta(null)}
        alConfirmar={() => citaAbierta && confirmar.mutate(citaAbierta)}
        alPedirCancelacion={() => {
          cancelar.reset();
          setCitaACancelar(citaAbierta);
        }}
        confirmando={confirmar.isPending}
        error={confirmar.isError ? mensajeDe(confirmar.error) : null}
      />
      <DialogoCancelar
        key={citaACancelar?.id ?? 0}
        cita={citaACancelar}
        alCerrar={() => setCitaACancelar(null)}
        alCancelar={(motivo) => citaACancelar && cancelar.mutate({ cita: citaACancelar, motivo })}
        cancelando={cancelar.isPending}
        error={cancelar.isError ? mensajeDe(cancelar.error) : null}
      />
    </div>
  );
}

const CLASE_ESTADO: Record<CitaAgenda['estado'], string> = {
  pendiente: 'pendiente',
  confirmada: 'confirmada',
  completada: 'completada',
  cancelada: 'cancelada',
  no_asistio: 'noAsistio',
};

const textoAccesible = (c: CitaAgenda) => `, ${diaLargo(c.fecha.split('/').reverse().join('-')).toLocaleLowerCase('es-MX')}, ${horaCorta(c.hora)}`;

/** Semana (Agenda.dc.html): cuadrícula de lunes a sábado por horario, con descansos rayados. */
function Semana({
  dias,
  hoy,
  citas,
  dentistaId,
  horario,
  alAbrir,
}: {
  dias: string[];
  hoy: string;
  citas: CitaAgenda[];
  dentistaId: number | null;
  horario: ReturnType<typeof deducirHorario>;
  alAbrir: (c: CitaAgenda) => void;
}) {
  const porDia = dias.map((d) => citasDelDia(citas, d, dentistaId));
  const filas = filasDelDia(horario, porDia.flat());
  const plantilla = ['64px', ...filas.map((f) => (f.tipo === 'descanso' ? '44px' : '84px'))].join(' ');

  return (
    <section className={estilos.tarjeta} aria-label="Semana">
      <div className={estilos.leyenda}>
        Estados:
        {(['confirmada', 'pendiente', 'completada', 'no_asistio'] as const).map((e) => (
          <InsigniaEstadoCita key={e} estado={e} />
        ))}
        <span className={estilos.leyendaNota}>Una solicitud pendiente bloquea su horario.</span>
      </div>
      {filas.length === 0 ? (
        <MensajeEstado nivel={2} icono="agenda" titulo="No hay citas esta semana" texto="Es una semana sin citas." />
      ) : (
        <div className={estilos.desplazable} role="region" aria-label="Citas de la semana" tabIndex={0}>
          <div className={estilos.cuadricula} style={{ gridTemplateRows: plantilla }}>
            <div className={estilos.esquina} style={{ gridColumn: 1, gridRow: 1 }} />
            {dias.map((d, i) => (
              <div key={d} className={`${estilos.cabeceraDia} ${d === hoy ? estilos.hoy : ''}`} style={{ gridColumn: i + 2, gridRow: 1 }}>
                <span className={estilos.diaNombre}>{d === hoy ? `${diaCorto(d)} · hoy` : diaCorto(d)}</span>
                <span className={estilos.diaNumero} {...(d === hoy ? { 'aria-current': 'date' as const } : {})}>
                  {numeroDia(d)}
                </span>
              </div>
            ))}
            {filas.map((f, r) => (
              <FilaSemana key={f.hora} fila={f.tipo} hora={f.hora} r={r} dias={dias} hoy={hoy} />
            ))}
            {porDia.map((lista, i) =>
              lista.map((c) => {
                const lugar = ubicacion(c, filas);
                if (!lugar) return null;
                return (
                  <button
                    key={c.id}
                    type="button"
                    className={`${estilos.bloque} ${estilos[CLASE_ESTADO[c.estado]]}`}
                    style={{ gridColumn: i + 2, gridRow: `${lugar.fila + 2} / span ${lugar.filas}` }}
                    onClick={() => alAbrir(c)}
                  >
                    <span className={estilos.bloquePaciente}>{c.paciente}</span>
                    <span className={estilos.bloqueServicio}>{c.servicio}</span>
                    <span className={estilos.bloqueEstado}>{ESTADOS_CITA[c.estado].texto}</span>
                    <span className={estilos.soloLector}>{textoAccesible(c)}</span>
                  </button>
                );
              }),
            )}
          </div>
        </div>
      )}
    </section>
  );
}

function FilaSemana({ fila, hora, r, dias, hoy }: { fila: 'horario' | 'descanso'; hora: string; r: number; dias: string[]; hoy: string }) {
  const gridRow = r + 2;
  return (
    <>
      <div className={estilos.hora} style={{ gridColumn: 1, gridRow }}>
        {horaCorta(hora)}
      </div>
      {fila === 'descanso' ? (
        <div className={estilos.descanso} style={{ gridColumn: `2 / span ${dias.length}`, gridRow }}>
          Descanso
        </div>
      ) : (
        dias.map((d, i) => <div key={d} className={`${estilos.celda} ${d === hoy ? estilos.hoy : ''}`} style={{ gridColumn: i + 2, gridRow }} />)
      )}
    </>
  );
}

/** Día (Movil · Agenda del día): selector de días y lista por horario, con libres y descansos. */
function Dia({
  dias,
  fecha,
  hoy,
  alElegirDia,
  citas,
  dentistaId,
  horario,
  libres,
  alAbrir,
}: {
  dias: string[];
  fecha: string;
  hoy: string;
  alElegirDia: (d: string) => void;
  citas: CitaAgenda[];
  dentistaId: number | null;
  horario: ReturnType<typeof deducirHorario>;
  libres: string[];
  alAbrir: (c: CitaAgenda) => void;
}) {
  const delDia = citasDelDia(citas, fecha, dentistaId);
  const cubiertas = new Set(delDia.flatMap((c) => horariosDeCita(c).slice(1)));
  const filas = filasDelDia(horario, delDia).filter((f) => !cubiertas.has(f.hora));
  const libresSet = new Set(libres);
  const conContenido = filas.filter((f) => f.tipo === 'descanso' || libresSet.has(f.hora) || delDia.some((c) => c.hora === f.hora));
  const hayCitasOLibres = conContenido.some((f) => f.tipo === 'horario');

  return (
    <section className={estilos.dia} aria-label={diaLargo(fecha)}>
      <div className={estilos.chips}>
        {dias.map((d) => (
          <button
            key={d}
            type="button"
            className={estilos.chip}
            aria-pressed={d === fecha}
            {...(d === hoy ? { 'aria-current': 'date' as const } : {})}
            onClick={() => alElegirDia(d)}
          >
            <span className={estilos.chipDia}>{diaCorto(d)}</span>
            <span className={estilos.chipNumero}>{numeroDia(d)}</span>
          </button>
        ))}
      </div>
      {!hayCitasOLibres ? (
        <div className={estilos.vacioDia}>
          <MensajeEstado nivel={2} icono="agenda" titulo="No hay citas este día" texto="Es un día sin citas." />
        </div>
      ) : (
        <ul className={estilos.lista}>
          {conContenido.map((f) => {
            const c = delDia.find((x) => x.hora === f.hora);
            return (
              <li key={f.hora} className={estilos.filaDia}>
                <span className={estilos.horaDia}>{horaCorta(f.hora)}</span>
                <span className={estilos.contenidoDia}>
                  {f.tipo === 'descanso' ? (
                    <span className={estilos.descansoDia}>Descanso</span>
                  ) : c ? (
                    <button type="button" className={estilos.citaDia} onClick={() => alAbrir(c)}>
                      <span className={estilos.citaDatos}>
                        <span className={estilos.citaPaciente}>{c.paciente}</span>
                        <span className={estilos.citaServicio}>
                          {c.servicio}
                          {duracionMinutos(c) > 60 && ` · ${Math.round(duracionMinutos(c) / 60)} horarios`}
                        </span>
                      </span>
                      <InsigniaEstadoCita estado={c.estado} />
                    </button>
                  ) : (
                    <span className={estilos.libre}>Libre</span>
                  )}
                </span>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}

