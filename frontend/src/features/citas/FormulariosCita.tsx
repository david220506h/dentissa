import {
  Aviso,
  Boton,
  CampoAreaTexto,
  CampoSeleccion,
  CampoTexto,
  ChipAlergia,
  Dialogo,
  InsigniaEstadoCita,
} from '@dentissa/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';
import { enviarJson, ErrorApi, mensajeDe, obtenerJson } from '../../app/api';
import {
  fechaHoraCorta,
  isoDeApi,
  MOTIVOS_CANCELACION,
  type CitaAgenda,
} from '../agenda/logica';
import {
  accionesDe,
  alternativas,
  ESTADO_TRAS_REAGENDAR,
  erroresAltaRapida,
  horaApi,
  MAX_NOTA,
  motivoCancelacion,
  type HorarioLibre,
  type RolPanel,
  type Servicio,
} from './logica';
import estilos from './Formularios.module.css';

// Formularios de cita (CitaFormularios.dc.html). Textos en género neutro ("la persona"). Los avisos al
// paciente por correo los decide el backend (bandeja de salida): no hay casilla "Avisar por correo".

const invalidarAgenda = async (qc: ReturnType<typeof useQueryClient>) => {
  await qc.invalidateQueries({ queryKey: ['citas'] });
  await qc.invalidateQueries({ queryKey: ['disponibilidad'] });
  await qc.invalidateQueries({ queryKey: ['libres'] });
  await qc.invalidateQueries({ queryKey: ['pacientes'] });
};

const esConflicto = (e: unknown) =>
  e instanceof ErrorApi && e.estado === 409 && e.codigo === 'HORARIO_OCUPADO';

function Resumen({
  cita,
  conDentista = false,
}: {
  cita: CitaAgenda;
  conDentista?: boolean;
}) {
  return (
    <div className={estilos.resumen}>
      <b>{cita.paciente}</b> · {cita.servicio}
      <br />
      {fechaHoraCorta(isoDeApi(cita.fecha), cita.hora)}
      {conDentista && ` · ${cita.dentista}`}
    </div>
  );
}

function useDisponibilidad(fecha: string, servicioId: number | null) {
  return useQuery({
    queryKey: ['disponibilidad', fecha, servicioId],
    queryFn: () =>
      obtenerJson<HorarioLibre[]>(
        `/agenda/disponibilidad?fecha=${fecha}&servicioId=${servicioId}`,
      ),
    enabled: !!fecha && !!servicioId,
    retry: false,
  });
}

const corta = (h: string) => h.replace(/^0(\d)/, '$1');

/** Horario libre (select del diseño). Sin horarios, una nota en lugar del campo. */
function HorariosLibres({
  libres,
  elegido,
  alElegir,
  cargando,
  error,
  etiqueta = 'Horario libre',
}: {
  libres: HorarioLibre[] | undefined;
  elegido: string;
  alElegir: (hora: string) => void;
  cargando: boolean;
  error: unknown;
  etiqueta?: string;
}) {
  if (cargando)
    return <p className={estilos.ayuda}>Buscando horarios libres…</p>;
  if (error) return <p className={estilos.ayuda}>{mensajeDe(error)}</p>;
  if (!libres) return null;
  if (libres.length === 0)
    return (
      <p className={estilos.ayuda}>
        No hay horarios libres ese día. Prueba con otra fecha.
      </p>
    );
  return (
    <CampoSeleccion
      etiqueta={etiqueta}
      value={elegido}
      onChange={(e) => alElegir(e.target.value)}
    >
      <option value="">Elige un horario</option>
      {libres.map((h) => (
        <option key={h.hora} value={h.hora}>
          {corta(h.hora)}
        </option>
      ))}
    </CampoSeleccion>
  );
}

/** Aviso del 409 (CitaFormularios · "Ese horario ya no está libre"): los datos siguen y se elige otro horario. */
function Conflicto({
  intentado,
  opciones,
  elegido,
  alElegir,
}: {
  intentado: string;
  opciones: string[];
  elegido: string;
  alElegir: (h: string) => void;
}) {
  return (
    <div className={estilos.grupo}>
      <Aviso tono="error" anunciar>
        <b>Ese horario ya no está libre.</b> Otra persona tomó el horario de{' '}
        {corta(intentado)} mientras llenabas el formulario. Tus datos siguen
        aquí.
      </Aviso>
      {opciones.length > 0 ? (
        <CampoSeleccion
          etiqueta="Elige otro horario libre"
          value={elegido}
          onChange={(e) => alElegir(e.target.value)}
        >
          {opciones.map((h) => (
            <option key={h} value={h}>
              {corta(h)}
            </option>
          ))}
        </CampoSeleccion>
      ) : (
        <p className={estilos.ayuda}>
          Ya no quedan horarios libres ese día. Elige otra fecha.
        </p>
      )}
    </div>
  );
}

interface PacienteOpcion {
  id: number;
  nombre: string;
  apellido: string;
  telefono: string | null;
  alergias: string | null;
}

/** Paciente existente (búsqueda por nombre o teléfono) o alta rápida sin salir del formulario. */
function SelectorPaciente({
  elegido,
  alElegir,
}: {
  elegido: PacienteOpcion | null;
  alElegir: (p: PacienteOpcion | null) => void;
}) {
  const qc = useQueryClient();
  const [texto, setTexto] = useState('');
  const [nuevo, setNuevo] = useState(false);
  const [alta, setAlta] = useState({ nombre: '', apellido: '', telefono: '' });
  const [errores, setErrores] = useState<ReturnType<typeof erroresAltaRapida>>(
    {},
  );
  const buscar = texto.trim();
  const resultados = useQuery({
    queryKey: ['pacientes', 'buscar', buscar],
    queryFn: () =>
      obtenerJson<{ elementos: PacienteOpcion[] }>(
        `/pacientes?buscar=${encodeURIComponent(buscar)}&tamano=6`,
      ),
    enabled: buscar.length >= 2 && !elegido,
  });
  const crear = useMutation({
    mutationFn: () => enviarJson<PacienteOpcion>('/pacientes', 'POST', alta),
    onSuccess: async (p) => {
      alElegir(p);
      setNuevo(false);
      await qc.invalidateQueries({ queryKey: ['pacientes'] });
    },
  });

  if (elegido)
    return (
      <div className={estilos.grupo}>
        <span className={estilos.etiqueta}>Paciente</span>
        <div className={estilos.elegido}>
          <span>
            <b>
              {elegido.nombre} {elegido.apellido}
            </b>
            {elegido.telefono && (
              <span className={estilos.secundario}> · {elegido.telefono}</span>
            )}
          </span>
          <Boton variante="texto" onClick={() => alElegir(null)}>
            Cambiar
          </Boton>
        </div>
        {elegido.alergias && <ChipAlergia alergia={elegido.alergias} />}
      </div>
    );

  if (nuevo)
    return (
      <div className={estilos.grupo}>
        <span className={estilos.etiqueta}>Paciente nuevo</span>
        <div className={estilos.dosColumnas}>
          <CampoTexto
            etiqueta="Nombre"
            value={alta.nombre}
            error={errores.nombre}
            onChange={(e) => setAlta({ ...alta, nombre: e.target.value })}
          />
          <CampoTexto
            etiqueta="Apellido"
            value={alta.apellido}
            error={errores.apellido}
            onChange={(e) => setAlta({ ...alta, apellido: e.target.value })}
          />
        </div>
        <CampoTexto
          etiqueta="Teléfono"
          type="tel"
          inputMode="tel"
          value={alta.telefono}
          error={errores.telefono}
          onChange={(e) => setAlta({ ...alta, telefono: e.target.value })}
        />
        {crear.isError && <Aviso tono="error">{mensajeDe(crear.error)}</Aviso>}
        <div className={estilos.fila}>
          <Boton variante="secundario" onClick={() => setNuevo(false)}>
            Buscar existente
          </Boton>
          <Boton
            variante="principal"
            cargando={crear.isPending}
            onClick={() => {
              const e = erroresAltaRapida(alta);
              setErrores(e);
              if (Object.keys(e).length === 0) crear.mutate();
            }}
          >
            Crear paciente
          </Boton>
        </div>
      </div>
    );

  return (
    <div className={estilos.grupo}>
      <CampoTexto
        etiqueta="Paciente"
        type="search"
        ayuda="Escribe nombre o teléfono. Si no existe, créala sin salir de aquí."
        value={texto}
        onChange={(e) => setTexto(e.target.value)}
      />
      {resultados.data && (
        <ul className={estilos.resultados} aria-label="Pacientes encontrados">
          {resultados.data.elementos.map((p) => (
            <li key={p.id}>
              <button
                type="button"
                className={estilos.resultado}
                onClick={() => alElegir(p)}
              >
                <b>
                  {p.nombre} {p.apellido}
                </b>
                <span className={estilos.secundario}>{p.telefono ?? '—'}</span>
              </button>
            </li>
          ))}
          {resultados.data.elementos.length === 0 && (
            <li className={estilos.ayuda}>Sin resultados.</li>
          )}
        </ul>
      )}
      <div>
        <Boton variante="texto" icono="agregar" onClick={() => setNuevo(true)}>
          Crear paciente nuevo
        </Boton>
      </div>
    </div>
  );
}

/** Nueva cita (CitaFormularios · "Nueva cita"). */
export function DialogoNuevaCita({
  abierto,
  fechaInicial,
  hoy,
  alCerrar,
  alCreada,
}: {
  abierto: boolean;
  fechaInicial: string;
  hoy: string;
  alCerrar: () => void;
  alCreada: (mensaje: string) => void;
}) {
  const qc = useQueryClient();
  const [paciente, setPaciente] = useState<PacienteOpcion | null>(null);
  const [servicioId, setServicioId] = useState<number | null>(null);
  const [fecha, setFecha] = useState(() =>
    fechaInicial < hoy ? hoy : fechaInicial,
  );
  const [hora, setHora] = useState('');
  const [dentistaId, setDentistaId] = useState<number | ''>('');
  const [motivo, setMotivo] = useState('');
  const [confirmada, setConfirmada] = useState(false);
  const [presente, setPresente] = useState(false);
  const [conflicto, setConflicto] = useState<string | null>(null);
  const [errorPaciente, setErrorPaciente] = useState<string | undefined>();

  const servicios = useQuery({
    queryKey: ['servicios'],
    queryFn: () => obtenerJson<Servicio[]>('/citas/servicios'),
    staleTime: 10 * 60_000,
    enabled: abierto,
  });
  const servicio = servicioId ?? servicios.data?.[0]?.id ?? null;
  const libres = useDisponibilidad(fecha, servicio);
  const elegido = libres.data?.find((h) => h.hora === hora);

  const guardar = useMutation({
    mutationFn: (horaGuardar: string) =>
      enviarJson('/citas', 'POST', {
        pacienteId: paciente!.id,
        servicioId: servicio,
        fecha,
        hora: horaApi(horaGuardar),
        dentistaId: dentistaId === '' ? null : dentistaId,
        motivo: motivo.trim() || null,
        confirmarAlCrear: confirmada,
        pacientePresente: presente,
      }),
    onSuccess: async () => {
      await invalidarAgenda(qc);
      alCreada(confirmada ? 'Cita creada y confirmada.' : 'Cita creada.');
      reiniciar();
    },
    onError: async (e, horaIntentada) => {
      if (esConflicto(e)) {
        setConflicto(horaIntentada);
        const nuevos = (await libres.refetch()).data ?? [];
        setHora(alternativas(nuevos, horaIntentada)[0] ?? '');
        setDentistaId('');
      }
    },
  });

  const reiniciar = () => {
    setPaciente(null);
    setHora('');
    setDentistaId('');
    setMotivo('');
    setConfirmada(false);
    setPresente(false);
    setConflicto(null);
    guardar.reset();
  };

  const enviar = (horaGuardar: string) => {
    if (!paciente) {
      setErrorPaciente('Elige o crea la persona.');
      return;
    }
    setErrorPaciente(undefined);
    setHora(horaGuardar);
    guardar.mutate(horaGuardar);
  };

  return (
    <Dialogo
      abierto={abierto}
      alCerrar={() => {
        reiniciar();
        alCerrar();
      }}
      titulo="Nueva cita"
      icono="agenda"
      completoEnCelular
      acciones={
        <>
          <Boton
            variante="secundario"
            onClick={() => {
              reiniciar();
              alCerrar();
            }}
          >
            Cancelar
          </Boton>
          <Boton
            variante="principal"
            icono="confirmar"
            disabled={!hora}
            cargando={guardar.isPending}
            onClick={() => enviar(hora)}
          >
            {conflicto && hora ? `Guardar en ${corta(hora)}` : 'Guardar cita'}
          </Boton>
        </>
      }
    >
      <div className={estilos.cuerpo}>
        <p className={estilos.ayuda}>
          Los horarios libres ya excluyen descansos y cierres.
        </p>
        <SelectorPaciente
          elegido={paciente}
          alElegir={(p) => {
            setPaciente(p);
            setErrorPaciente(undefined);
          }}
        />
        {errorPaciente && <p className={estilos.error}>{errorPaciente}</p>}
        <CampoSeleccion
          etiqueta="Servicio"
          value={servicio ?? ''}
          onChange={(e) => {
            setServicioId(Number(e.target.value));
            setHora('');
            setDentistaId('');
          }}
        >
          {servicios.data?.map((s) => (
            <option key={s.id} value={s.id}>
              {s.nombre}
              {s.duracionSlots > 1 ? ` · ${s.duracionSlots} horarios` : ''}
            </option>
          ))}
        </CampoSeleccion>
        <div className={estilos.dosColumnas}>
          <CampoTexto
            etiqueta="Fecha"
            type="date"
            min={hoy}
            value={fecha}
            onChange={(e) => {
              setFecha(e.target.value);
              setHora('');
              setDentistaId('');
            }}
          />
          {!conflicto && (
            <HorariosLibres
              libres={libres.data}
              elegido={hora}
              cargando={libres.isFetching && !libres.data}
              error={libres.error}
              alElegir={(h) => {
                setHora(h);
                setDentistaId('');
              }}
            />
          )}
        </div>
        {conflicto && (
          <Conflicto
            intentado={conflicto}
            opciones={alternativas(libres.data ?? [], conflicto, 6)}
            elegido={hora}
            alElegir={setHora}
          />
        )}
        {elegido && (
          <CampoSeleccion
            etiqueta="Dentista"
            value={dentistaId}
            onChange={(e) =>
              setDentistaId(e.target.value === '' ? '' : Number(e.target.value))
            }
          >
            <option value="">Que la clínica asigne</option>
            {elegido.dentistasLibres.map((d) => (
              <option key={d.id} value={d.id}>
                {d.nombre}
              </option>
            ))}
          </CampoSeleccion>
        )}
        <CampoAreaTexto
          etiqueta="Motivo (opcional)"
          ayuda="Solo lo ve el personal autorizado. No escribas diagnósticos aquí."
          maxLength={500}
          value={motivo}
          onChange={(e) => setMotivo(e.target.value)}
        />
        <Casilla marcada={confirmada} alCambiar={setConfirmada}>
          Crear ya confirmada (la persona aceptó por teléfono o en persona)
        </Casilla>
        <Casilla marcada={presente} alCambiar={setPresente}>
          La persona está en la clínica (sin la anticipación mínima; hasta 15
          min después del inicio)
        </Casilla>
        {guardar.isError && !esConflicto(guardar.error) && (
          <Aviso tono="error">{mensajeDe(guardar.error)}</Aviso>
        )}
      </div>
    </Dialogo>
  );
}

function Casilla({
  marcada,
  alCambiar,
  children,
}: {
  marcada: boolean;
  alCambiar: (v: boolean) => void;
  children: ReactNode;
}) {
  return (
    <label className={estilos.casilla}>
      <input
        type="checkbox"
        checked={marcada}
        onChange={(e) => alCambiar(e.target.checked)}
      />
      <span>{children}</span>
    </label>
  );
}

/** Reagendar cita (CitaFormularios · "Reagendar cita"): conserva el estado; el precio no cambia. */
export function DialogoReagendar({
  cita,
  hoy,
  alCerrar,
  alListo,
}: {
  cita: CitaAgenda | null;
  hoy: string;
  alCerrar: () => void;
  alListo: (m: string) => void;
}) {
  const qc = useQueryClient();
  // Se monta por cita (key): el estado empieza en la fecha actual de la cita, o en hoy si ya pasó
  const [fecha, setFecha] = useState(() => {
    const actual = cita ? isoDeApi(cita.fecha) : hoy;
    return actual < hoy ? hoy : actual;
  });
  const [hora, setHora] = useState('');
  const [conflicto, setConflicto] = useState<string | null>(null);
  const libres = useDisponibilidad(fecha, cita?.servicioId ?? null);
  const estado = cita?.estado === 'pendiente' ? 'pendiente' : 'confirmada';
  const reagendar = useMutation({
    mutationFn: (h: string) =>
      enviarJson(`/citas/${cita!.id}/reagendar`, 'PATCH', {
        fecha,
        hora: horaApi(h),
        estadoResultante: estado,
        version: cita!.version,
      }),
    onSuccess: async () => {
      await invalidarAgenda(qc);
      alListo('Cita reagendada.');
    },
    onError: async (e, h) => {
      if (esConflicto(e)) {
        setConflicto(h);
        const nuevos = (await libres.refetch()).data ?? [];
        setHora(alternativas(nuevos, h)[0] ?? '');
      }
    },
  });
  const enviar = (h: string) => {
    setHora(h);
    reagendar.mutate(h);
  };
  return (
    <Dialogo
      abierto={cita !== null}
      alCerrar={alCerrar}
      titulo="Reagendar cita"
      icono="agenda"
      completoEnCelular
      acciones={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton
            variante="principal"
            icono="confirmar"
            disabled={!hora}
            cargando={reagendar.isPending}
            onClick={() => enviar(hora)}
          >
            {conflicto && hora ? `Guardar en ${corta(hora)}` : 'Reagendar'}
          </Boton>
        </>
      }
    >
      {cita && (
        <div className={estilos.cuerpo}>
          <p className={estilos.ayuda}>
            Si estaba confirmada, sigue confirmada.
          </p>
          <div className={estilos.resumen}>
            Ahora: {fechaHoraCorta(isoDeApi(cita.fecha), cita.hora)} ·{' '}
            {cita.dentista}
          </div>
          <CampoTexto
            etiqueta="Nueva fecha"
            type="date"
            min={hoy}
            value={fecha}
            onChange={(e) => {
              setFecha(e.target.value);
              setHora('');
            }}
          />
          {!conflicto && (
            <HorariosLibres
              libres={libres.data}
              elegido={hora}
              cargando={libres.isFetching && !libres.data}
              error={libres.error}
              alElegir={setHora}
            />
          )}
          {conflicto && (
            <Conflicto
              intentado={conflicto}
              opciones={alternativas(libres.data ?? [], conflicto, 6)}
              elegido={hora}
              alElegir={setHora}
            />
          )}
          <p className={estilos.ayuda}>
            Quedará: <b>{ESTADO_TRAS_REAGENDAR[estado]}</b>
          </p>
          {reagendar.isError && !esConflicto(reagendar.error) && (
            <Aviso tono="error">{mensajeDe(reagendar.error)}</Aviso>
          )}
        </div>
      )}
    </Dialogo>
  );
}

/** Cambiar de dentista en el mismo horario (CitaFormularios · "Cambiar de dentista"). */
export function DialogoCambiarDentista({
  cita,
  alCerrar,
  alListo,
}: {
  cita: CitaAgenda | null;
  alCerrar: () => void;
  alListo: (m: string) => void;
}) {
  const qc = useQueryClient();
  const [dentistaId, setDentistaId] = useState<number | ''>('');
  const libres = useDisponibilidad(
    cita ? isoDeApi(cita.fecha) : '',
    cita?.servicioId ?? null,
  );
  const opciones = (
    libres.data?.find((h) => h.hora === cita?.hora)?.dentistasLibres ?? []
  ).filter((d) => d.id !== cita?.dentistaId);
  const cambiar = useMutation({
    mutationFn: () =>
      enviarJson(`/citas/${cita!.id}/dentista`, 'PATCH', {
        dentistaId,
        motivo: 'Cambio de dentista desde el panel',
        version: cita!.version,
      }),
    onSuccess: async () => {
      await invalidarAgenda(qc);
      alListo('Dentista cambiado.');
    },
  });
  return (
    <Dialogo
      abierto={cita !== null}
      alCerrar={alCerrar}
      titulo="Cambiar de dentista"
      icono="equipo"
      completoEnCelular
      acciones={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton
            variante="principal"
            icono="confirmar"
            disabled={dentistaId === ''}
            cargando={cambiar.isPending}
            onClick={() => cambiar.mutate()}
          >
            Cambiar dentista
          </Boton>
        </>
      }
    >
      {cita && (
        <div className={estilos.cuerpo}>
          <Resumen cita={cita} conDentista />
          {libres.isFetching && !libres.data && (
            <p className={estilos.ayuda}>Buscando dentistas libres…</p>
          )}
          {libres.data && opciones.length === 0 && (
            <p className={estilos.ayuda}>
              Ningún otro dentista está libre en ese horario. Puedes reagendar
              la cita.
            </p>
          )}
          {opciones.length > 0 && (
            <CampoSeleccion
              etiqueta="Dentista"
              value={dentistaId}
              onChange={(e) =>
                setDentistaId(
                  e.target.value === '' ? '' : Number(e.target.value),
                )
              }
            >
              <option value="">Elige un dentista</option>
              {opciones.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.nombre}
                </option>
              ))}
            </CampoSeleccion>
          )}
          {cambiar.isError && (
            <Aviso tono="error">{mensajeDe(cambiar.error)}</Aviso>
          )}
        </div>
      )}
    </Dialogo>
  );
}

/** Cancelar cita (CitaFormularios · "Cancelar cita"): motivo neutro y nota opcional, que solo vive en la cita. */
export function DialogoCancelar({
  cita,
  alCerrar,
  alListo,
}: {
  cita: CitaAgenda | null;
  alCerrar: () => void;
  alListo: (m: string) => void;
}) {
  const qc = useQueryClient();
  const [motivo, setMotivo] = useState<string>(MOTIVOS_CANCELACION[0]);
  const [nota, setNota] = useState('');
  const cancelar = useMutation({
    mutationFn: () =>
      enviarJson(`/citas/${cita!.id}/cancelar`, 'PATCH', {
        motivo: motivoCancelacion(motivo, nota),
        version: cita!.version,
      }),
    onSuccess: async () => {
      await invalidarAgenda(qc);
      alListo('Cita cancelada.');
    },
  });
  return (
    <Dialogo
      abierto={cita !== null}
      alCerrar={alCerrar}
      titulo="Cancelar cita"
      icono="cerrar"
      completoEnCelular
      acciones={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Conservar cita
          </Boton>
          <Boton
            variante="principal"
            icono="cerrar"
            onClick={() => cancelar.mutate()}
            cargando={cancelar.isPending}
            textoCargando="Cancelando…"
          >
            Cancelar cita
          </Boton>
        </>
      }
    >
      {cita && (
        <div className={estilos.cuerpo}>
          <Resumen cita={cita} />
          <CampoSeleccion
            id="motivo-cancelacion"
            etiqueta="Motivo de la cancelación"
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
          >
            {MOTIVOS_CANCELACION.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </CampoSeleccion>
          <CampoAreaTexto
            etiqueta="Nota (opcional)"
            ayuda="La nota es sensible: se guarda solo en la cita. El historial solo dice «Cancelada por recepción»."
            maxLength={MAX_NOTA}
            value={nota}
            onChange={(e) => setNota(e.target.value)}
          />
          {cancelar.isError && (
            <Aviso tono="error" anunciar>
              {mensajeDe(cancelar.error)}
            </Aviso>
          )}
        </div>
      )}
    </Dialogo>
  );
}

/**
 * Diálogo «Cita» al tocar una cita: datos, estado y acciones según el rol (accionesDe). Confirmar, completar
 * y no asistió se aplican aquí; reagendar, cambiar de dentista y cancelar abren su formulario.
 */
export function DialogoCita({
  cita,
  rol,
  ahora,
  alCerrar,
  alReagendar,
  alCambiarDentista,
  alCancelar,
  alListo,
}: {
  cita: CitaAgenda | null;
  rol: RolPanel;
  ahora: Date;
  alCerrar: () => void;
  alReagendar: () => void;
  alCambiarDentista: () => void;
  alCancelar: () => void;
  alListo: (m: string) => void;
}) {
  const qc = useQueryClient();
  const accion = useMutation({
    mutationFn: ({ ruta }: { ruta: string; mensaje: string }) =>
      enviarJson(`/citas/${cita!.id}/${ruta}`, 'PATCH', {
        version: cita!.version,
      }),
    onSuccess: async (_d, { mensaje }) => {
      await invalidarAgenda(qc);
      alListo(mensaje);
    },
  });
  const a = cita ? accionesDe(cita, rol, ahora) : null;
  return (
    <Dialogo
      abierto={cita !== null}
      alCerrar={alCerrar}
      titulo="Cita"
      icono="agenda"
      completoEnCelular
      acciones={
        a && (a.cancelar || a.confirmar || a.completar || a.noAsistio) ? (
          <>
            {a.cancelar && (
              <Boton variante="secundario" icono="cerrar" onClick={alCancelar}>
                Cancelar cita
              </Boton>
            )}
            {a.noAsistio && (
              <Boton
                variante="secundario"
                onClick={() =>
                  accion.mutate({
                    ruta: 'no-asistio',
                    mensaje: 'Inasistencia registrada.',
                  })
                }
              >
                No asistió
              </Boton>
            )}
            {a.completar && (
              <Boton
                variante={a.confirmar ? 'secundario' : 'principal'}
                icono="confirmar"
                cargando={accion.isPending}
                onClick={() =>
                  accion.mutate({
                    ruta: 'completar',
                    mensaje: 'Cita completada.',
                  })
                }
              >
                Completar
              </Boton>
            )}
            {a.confirmar && (
              <Boton
                variante="principal"
                icono="confirmar"
                cargando={accion.isPending}
                textoCargando="Confirmando…"
                onClick={() =>
                  accion.mutate({
                    ruta: 'confirmar',
                    mensaje: 'Cita confirmada.',
                  })
                }
              >
                Confirmar cita
              </Boton>
            )}
          </>
        ) : undefined
      }
    >
      {cita && a && (
        <div className={estilos.cuerpo}>
          <Resumen cita={cita} conDentista />
          <div className={estilos.fila}>
            <InsigniaEstadoCita estado={cita.estado} />
            {cita.pacientePresente && (
              <span className={estilos.secundario}>
                Agendada con la persona en la clínica
              </span>
            )}
          </div>
          {(a.reagendar || a.cambiarDentista) && (
            <div className={estilos.fila}>
              {a.reagendar && (
                <Boton variante="texto" icono="agenda" onClick={alReagendar}>
                  Reagendar
                </Boton>
              )}
              {a.cambiarDentista && (
                <Boton
                  variante="texto"
                  icono="equipo"
                  onClick={alCambiarDentista}
                >
                  Cambiar de dentista
                </Boton>
              )}
            </div>
          )}
          {accion.isError && (
            <Aviso tono="error" anunciar>
              {mensajeDe(accion.error)}
            </Aviso>
          )}
        </div>
      )}
    </Dialogo>
  );
}
