import {
  Aviso,
  Avatar,
  Boton,
  Busqueda,
  CampoAreaTexto,
  CampoSeleccion,
  CampoTexto,
  CargandoLista,
  ChipAlergia,
  Dialogo,
  EncabezadoPagina,
  Icono,
  Insignia,
  InsigniaEstadoCita,
  MensajeEstado,
  Paginacion,
  Tabla,
  TarjetaPaciente,
  type ComponenteEnlace,
} from '@dentissa/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ErrorApi, enviarJson, mensajeDe, obtenerJson } from '../../app/api';
import { useSesion } from '../../app/sesion';
import { hoyIsoEnClinica } from '../inicio/datos';
import {
  cuerpoDe,
  erroresFormulario,
  fechaCorta,
  FORMULARIO_VACIO,
  formularioDe,
  nombreCompleto,
  rangoPagina,
  SEXOS,
  TAMANO_PAGINA,
  terminoValido,
  type CitaResumen,
  type FormularioPaciente,
  type PacienteDetalle,
  type PacienteLista,
  type PaginaPacientes,
} from './logica';
import estilos from './Pacientes.module.css';

const EnlaceRouter: ComponenteEnlace = ({ href, children, ...resto }) => (
  <Link to={href} {...resto}>
    {children}
  </Link>
);

const Cuenta = ({ cuenta }: { cuenta: PacienteLista['cuenta'] }) =>
  cuenta === 'con-cuenta' ? <Insignia tono="azul">Con cuenta</Insignia> : <Insignia tono="gris">Invitado</Insignia>;

// Una cita pendiente es una solicitud sin confirmar (Pacientes.dc.html: "19 nov 2026 (solicitada)")
const fechaCita = (c: CitaResumen | null) => (c ? `${fechaCorta(c.fecha)}${c.estado === 'pendiente' ? ' (solicitada)' : ''}` : '—');

function Alergias({ alergias }: { alergias: string | null }) {
  return alergias ? <ChipAlergia alergia={alergias} /> : <span className={estilos.sinRegistro}>Sin registro</span>;
}

/**
 * Pacientes (Pacientes.dc.html; en celular, Movil2 · Pacientes en tarjetas, con la alergia siempre visible).
 * Lista paginada con búsqueda por nombre o teléfono; alta y edición para recepción. Sin expediente.
 */
export function Pacientes() {
  const { usuario } = useSesion();
  const recepcion = usuario?.rol === 'admin' || usuario?.rol === 'asistente';
  const [texto, setTexto] = useState('');
  const [busqueda, setBusqueda] = useState('');
  const [pagina, setPagina] = useState(1);
  const [altaAbierta, setAltaAbierta] = useState(false);
  const [aviso, setAviso] = useState<string | null>(null);

  // Espera 300 ms tras dejar de escribir antes de buscar
  useEffect(() => {
    const t = setTimeout(() => {
      setBusqueda(terminoValido(texto));
      setPagina(1);
    }, 300);
    return () => clearTimeout(t);
  }, [texto]);

  const lista = useQuery({
    queryKey: ['pacientes', 'lista', busqueda, pagina],
    queryFn: () =>
      obtenerJson<PaginaPacientes>(`/pacientes?pagina=${pagina}&tamano=${TAMANO_PAGINA}${busqueda ? `&buscar=${encodeURIComponent(busqueda)}` : ''}`),
    placeholderData: (previo) => previo,
  });
  const rango = lista.data ? rangoPagina(lista.data.pagina, lista.data.tamano, lista.data.total, lista.data.elementos.length) : null;

  return (
    <div className={estilos.pagina}>
      <EncabezadoPagina
        titulo="Pacientes"
        subtitulo="Registro de pacientes de la clínica"
        acciones={
          recepcion && (
            <Boton variante="principal" icono="agregar" onClick={() => setAltaAbierta(true)}>
              Nuevo paciente
            </Boton>
          )
        }
      />
      <Aviso tono="info">Esta lista solo muestra datos generales y alergias. El expediente clínico lo ve y edita únicamente el dentista.</Aviso>
      {aviso && (
        <Aviso tono="exito" anunciar alCerrar={() => setAviso(null)}>
          {aviso}
        </Aviso>
      )}

      <div className={estilos.busqueda}>
        <Busqueda
          etiqueta="Buscar paciente"
          placeholder="Buscar por nombre o teléfono"
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
        />
      </div>

      {lista.isPending && (
        <section className={estilos.tarjeta}>
          <CargandoLista etiqueta="Cargando pacientes" filas={4} />
        </section>
      )}
      {lista.isError && (
        <section className={estilos.tarjeta}>
          <MensajeEstado
            nivel={2}
            icono="sinConexion"
            titulo="No pudimos cargar los pacientes"
            texto={lista.error instanceof ErrorApi && lista.error.estado === 400 ? lista.error.message : 'Revisa tu conexión con la clínica e inténtalo de nuevo. No se perdió ningún dato.'}
            accion={
              <Boton variante="principal" icono="reintentar" onClick={() => void lista.refetch()} cargando={lista.isFetching}>
                Reintentar
              </Boton>
            }
          />
        </section>
      )}
      {lista.data && lista.data.elementos.length === 0 && (
        <section className={estilos.tarjeta}>
          <MensajeEstado
            nivel={2}
            icono="pacientes"
            titulo={busqueda ? 'Sin resultados' : 'Aún no hay pacientes'}
            texto={busqueda ? 'Prueba con otro nombre o teléfono.' : 'Registra a la primera persona con "Nuevo paciente".'}
          />
        </section>
      )}
      {lista.data && lista.data.elementos.length > 0 && rango && (
        <>
          <div className={estilos.soloEscritorio}>
            <Tabla
              etiqueta="Pacientes"
              columnas={['Paciente', 'Teléfono', 'Cuenta', 'Alergias', 'Última cita', 'Próxima cita', '']}
              anchoMinimo={860}
              pie={
                <Paginacion
                  {...rango}
                  total={lista.data.total}
                  alAnterior={() => setPagina((p) => p - 1)}
                  alSiguiente={() => setPagina((p) => p + 1)}
                />
              }
            >
              {lista.data.elementos.map((p) => (
                <tr key={p.id}>
                  <td>
                    <div className={estilos.persona}>
                      <Avatar nombre={nombreCompleto(p)} tamano={36} />
                      <span className={`${estilos.nombre} ${estilos.sinCorte}`}>{nombreCompleto(p)}</span>
                    </div>
                  </td>
                  <td className={estilos.sinCorte}>{p.telefono ?? '—'}</td>
                  <td>
                    <Cuenta cuenta={p.cuenta} />
                  </td>
                  <td>
                    <Alergias alergias={p.alergias} />
                  </td>
                  <td className={estilos.sinCorte}>{fechaCita(p.ultimaCita)}</td>
                  <td className={estilos.sinCorte}>{fechaCita(p.proximaCita)}</td>
                  <td className={estilos.accion}>
                    <Link to={`/pacientes/${p.id}`} className={estilos.enlace} aria-label={`Ver ficha de ${nombreCompleto(p)}`}>
                      <Icono nombre="siguiente" tamano={18} />
                      <span>Ver ficha</span>
                    </Link>
                  </td>
                </tr>
              ))}
            </Tabla>
          </div>
          <div className={estilos.soloCelular}>
            <ul className={estilos.tarjetas} aria-label="Pacientes">
              {lista.data.elementos.map((p) => (
                <li key={p.id}>
                  <TarjetaPaciente
                    nombre={nombreCompleto(p)}
                    telefono={p.telefono}
                    cuenta={p.cuenta}
                    alergia={p.alergias}
                    detalle={p.proximaCita ? `Próxima: ${fechaCita(p.proximaCita)}` : undefined}
                    href={`/pacientes/${p.id}`}
                    enlace={EnlaceRouter}
                  />
                </li>
              ))}
            </ul>
            <Paginacion {...rango} total={lista.data.total} alAnterior={() => setPagina((p) => p - 1)} alSiguiente={() => setPagina((p) => p + 1)} />
          </div>
        </>
      )}

      {recepcion && altaAbierta && (
        <DialogoPaciente
          alCerrar={() => setAltaAbierta(false)}
          alGuardar={(p) => {
            setAltaAbierta(false);
            setAviso(`Se registró a ${nombreCompleto(p)}.`);
          }}
        />
      )}
    </div>
  );
}

/** Alta y edición (mismas reglas que el backend). Las alergias son dato de salud: su lectura queda en la bitácora. */
export function DialogoPaciente({
  paciente,
  alCerrar,
  alGuardar,
}: {
  paciente?: PacienteDetalle;
  alCerrar: () => void;
  alGuardar: (p: PacienteDetalle) => void;
}) {
  const qc = useQueryClient();
  const [ahora] = useState(() => new Date());
  const [f, setF] = useState<FormularioPaciente>(() => (paciente ? formularioDe(paciente) : FORMULARIO_VACIO));
  const [errores, setErrores] = useState<ReturnType<typeof erroresFormulario>>({});
  const guardar = useMutation({
    mutationFn: () =>
      paciente ? enviarJson<PacienteDetalle>(`/pacientes/${paciente.id}`, 'PUT', cuerpoDe(f)) : enviarJson<PacienteDetalle>('/pacientes', 'POST', cuerpoDe(f)),
    onSuccess: async (p) => {
      await qc.invalidateQueries({ queryKey: ['pacientes'] });
      alGuardar(p);
    },
  });
  const campo = (clave: keyof FormularioPaciente) => ({
    value: f[clave],
    error: errores[clave],
    onChange: (e: { target: { value: string } }) => setF({ ...f, [clave]: e.target.value }),
  });

  return (
    <Dialogo
      abierto
      alCerrar={alCerrar}
      titulo={paciente ? 'Editar paciente' : 'Nuevo paciente'}
      icono="pacientes"
      completoEnCelular
      acciones={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton
            variante="principal"
            icono="confirmar"
            cargando={guardar.isPending}
            onClick={() => {
              const e = erroresFormulario(f, hoyIsoEnClinica(ahora));
              setErrores(e);
              if (Object.keys(e).length === 0) guardar.mutate();
            }}
          >
            Guardar
          </Boton>
        </>
      }
    >
      <div className={estilos.formulario}>
        <div className={estilos.dosColumnas}>
          <CampoTexto etiqueta="Nombre" autoComplete="off" {...campo('nombre')} />
          <CampoTexto etiqueta="Apellido" autoComplete="off" {...campo('apellido')} />
          <CampoTexto etiqueta="Teléfono" type="tel" inputMode="tel" autoComplete="off" {...campo('telefono')} />
          <CampoTexto etiqueta="Correo" type="email" autoComplete="off" {...campo('correo')} />
          <CampoTexto etiqueta="Fecha de nacimiento (opcional)" type="date" {...campo('fechaNacimiento')} />
          <CampoSeleccion etiqueta="Sexo (opcional)" {...campo('sexo')}>
            {SEXOS.map((s) => (
              <option key={s.valor} value={s.valor}>
                {s.texto}
              </option>
            ))}
          </CampoSeleccion>
        </div>
        <CampoAreaTexto
          etiqueta="Alergias (opcional)"
          ayuda="Dato de salud: déjalo vacío si no hay registro. Se guarda quién lo consulta."
          maxLength={1000}
          {...campo('alergias')}
        />
        {guardar.isError && <Aviso tono="error">{mensajeDe(guardar.error)}</Aviso>}
      </div>
    </Dialogo>
  );
}

/** Ficha del paciente: datos generales, alergia y próximas citas (sin expediente). */
export function FichaPaciente() {
  const { id } = useParams();
  const { usuario } = useSesion();
  const recepcion = usuario?.rol === 'admin' || usuario?.rol === 'asistente';
  const [editando, setEditando] = useState(false);
  const [aviso, setAviso] = useState<string | null>(null);
  const ficha = useQuery({ queryKey: ['pacientes', 'ficha', id], queryFn: () => obtenerJson<PacienteDetalle>(`/pacientes/${id}`) });
  const p = ficha.data;

  return (
    <div className={estilos.pagina}>
      <Link to="/pacientes" className={estilos.volver}>
        ← Volver a Pacientes
      </Link>
      {ficha.isPending && (
        <section className={estilos.tarjeta}>
          <CargandoLista etiqueta="Cargando ficha" filas={3} />
        </section>
      )}
      {ficha.isError && (
        <section className={estilos.tarjeta}>
          <MensajeEstado
            nivel={1}
            icono="pacientes"
            titulo={ficha.error instanceof ErrorApi && ficha.error.estado === 404 ? 'No encontramos a esta persona' : 'No pudimos cargar la ficha'}
            texto="Vuelve a la lista de pacientes e inténtalo de nuevo."
          />
        </section>
      )}
      {p && (
        <>
          <EncabezadoPagina
            titulo={nombreCompleto(p)}
            subtitulo="Ficha: datos generales y citas"
            acciones={
              recepcion && (
                <Boton variante="secundario" icono="editar" onClick={() => setEditando(true)}>
                  Editar
                </Boton>
              )
            }
          />
          {aviso && (
            <Aviso tono="exito" anunciar alCerrar={() => setAviso(null)}>
              {aviso}
            </Aviso>
          )}
          <div className={estilos.columnasFicha}>
            <section className={estilos.tarjeta} aria-labelledby="datos-generales">
              <h2 id="datos-generales" className={estilos.tituloTarjeta}>
                Datos generales
              </h2>
              <div className={estilos.alergiaFicha}>
                <Alergias alergias={p.alergias} />
              </div>
              <dl className={estilos.datos}>
                <div>
                  <dt>Teléfono</dt>
                  <dd>{p.telefono ?? '—'}</dd>
                </div>
                <div>
                  <dt>Correo</dt>
                  <dd>{p.correo ?? '—'}</dd>
                </div>
                <div>
                  <dt>Fecha de nacimiento</dt>
                  <dd>{p.fechaNacimiento ? fechaCorta(p.fechaNacimiento) : '—'}</dd>
                </div>
                <div>
                  <dt>Sexo</dt>
                  <dd>{SEXOS.find((s) => s.valor === (p.sexo ?? ''))?.texto ?? '—'}</dd>
                </div>
                <div>
                  <dt>Cuenta</dt>
                  <dd>
                    <Cuenta cuenta={p.cuenta} />
                  </dd>
                </div>
                <div>
                  <dt>Última cita</dt>
                  <dd>{p.ultimaCita ? `${fechaCorta(p.ultimaCita.fecha)} · ${p.ultimaCita.servicio}` : '—'}</dd>
                </div>
              </dl>
            </section>
            <section className={estilos.tarjeta} aria-labelledby="proximas-citas">
              <h2 id="proximas-citas" className={estilos.tituloTarjeta}>
                Próximas citas
              </h2>
              {p.proximasCitas.length === 0 ? (
                <p className={estilos.sinRegistro}>Sin citas próximas.</p>
              ) : (
                <ul className={estilos.citas}>
                  {p.proximasCitas.map((c) => (
                    <li key={c.id} className={estilos.cita}>
                      <span>
                        <b>
                          {fechaCorta(c.fecha)}, {c.hora.replace(/^0(\d)/, '$1')}
                        </b>
                        <span className={estilos.servicio}>{c.servicio}</span>
                      </span>
                      <InsigniaEstadoCita estado={c.estado} />
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
          {recepcion && editando && (
            <DialogoPaciente
              paciente={p}
              alCerrar={() => setEditando(false)}
              alGuardar={() => {
                setEditando(false);
                setAviso('Cambios guardados.');
              }}
            />
          )}
        </>
      )}
    </div>
  );
}
