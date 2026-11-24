import { Aviso, Boton, CampoAreaTexto, CampoSeleccion, CargandoLista, Dialogo, EncabezadoPagina, MensajeEstado } from '@dentissa/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { enviarJson, mensajeDe, obtenerJson } from '../../app/api';
import { fechaCorta } from '../pacientes/logica';
import estilos from './Solicitudes.module.css';

/** Solicitud del sitio pendiente de confirmar (GET /api/solicitudes). */
export interface Solicitud {
  id: number;
  version: number;
  paciente: string;
  telefono: string | null;
  correo: string | null;
  pacienteId: number;
  servicio: string;
  fecha: string;
  hora: string;
  motivo: string | null;
  dentista: string;
  creadaEn: string;
  expiraEn: string | null;
}

/** Motivos neutros para rechazar (el paciente recibe el aviso sin el motivo). */
export const MOTIVOS_RECHAZO = [
  'La clínica no tiene ese horario disponible',
  'No se pudo contactar a la persona',
  'La persona pidió no agendar',
  'Otro motivo',
];

const horaCorta = (h: string) => h.replace(/^0(\d)/, '$1');

/** "Vence hoy a las 14:30" a partir del instante UTC del bloqueo. */
export function textoVencimiento(iso: string | null): string | null {
  if (!iso) return null;
  const f = new Date(iso);
  const hora = f.toLocaleTimeString('es-MX', { hour: 'numeric', minute: '2-digit', timeZone: 'America/Mexico_City' });
  const dia = f.toLocaleDateString('es-MX', { day: 'numeric', month: 'short', timeZone: 'America/Mexico_City' });
  return `Aparta el horario hasta el ${dia}, ${hora}`;
}

/** Bandeja «Solicitudes»: pendientes del sitio. Confirmar las vuelve citas; rechazar las cancela con motivo. */
export function Solicitudes() {
  const qc = useQueryClient();
  const [aviso, setAviso] = useState<string | null>(null);
  const [rechazando, setRechazando] = useState<Solicitud | null>(null);
  const lista = useQuery({ queryKey: ['solicitudes'], queryFn: () => obtenerJson<Solicitud[]>('/solicitudes'), refetchInterval: 60_000 });
  const refrescar = async () => {
    await qc.invalidateQueries({ queryKey: ['solicitudes'] });
    await qc.invalidateQueries({ queryKey: ['citas'] });
    await qc.invalidateQueries({ queryKey: ['pacientes'] });
  };
  const confirmar = useMutation({
    mutationFn: (s: Solicitud) => enviarJson(`/citas/${s.id}/confirmar`, 'PATCH', { canal: 'panel', version: s.version }),
    onSuccess: async (_, s) => {
      setAviso(`Cita confirmada: ${s.paciente}, ${fechaCorta(s.fecha)} a las ${horaCorta(s.hora)}. Ya está en la Agenda.`);
      await refrescar();
    },
  });

  return (
    <div className={estilos.pagina}>
      <EncabezadoPagina titulo="Solicitudes" subtitulo="Citas pedidas desde el sitio, por confirmar" />
      {aviso && (
        <Aviso tono="exito" anunciar alCerrar={() => setAviso(null)}>
          {aviso}
        </Aviso>
      )}
      {confirmar.isError && <Aviso tono="error">{mensajeDe(confirmar.error)}</Aviso>}
      {lista.isPending && (
        <section className={estilos.tarjeta}>
          <CargandoLista etiqueta="Cargando solicitudes" filas={3} />
        </section>
      )}
      {lista.isError && (
        <section className={estilos.tarjeta}>
          <MensajeEstado
            nivel={2}
            icono="sinConexion"
            titulo="No pudimos cargar las solicitudes"
            texto="Revisa tu conexión con la clínica e inténtalo de nuevo."
            accion={
              <Boton variante="principal" icono="reintentar" onClick={() => void lista.refetch()} cargando={lista.isFetching}>
                Reintentar
              </Boton>
            }
          />
        </section>
      )}
      {lista.data && lista.data.length === 0 && (
        <section className={estilos.tarjeta}>
          <MensajeEstado nivel={2} icono="solicitudes" titulo="No hay solicitudes por revisar" texto="Las que lleguen desde el sitio aparecen aquí." />
        </section>
      )}
      {lista.data && lista.data.length > 0 && (
        <ul className={estilos.lista} aria-label="Solicitudes por revisar">
          {lista.data.map((s) => (
            <li key={s.id} className={estilos.tarjeta}>
              <div className={estilos.cabeza}>
                <div>
                  <h2 className={estilos.nombre}>{s.paciente}</h2>
                  <p className={estilos.cuando}>
                    {s.servicio} · {fechaCorta(s.fecha)}, {horaCorta(s.hora)}
                  </p>
                </div>
              </div>
              <dl className={estilos.datos}>
                <div>
                  <dt>Teléfono</dt>
                  <dd>{s.telefono ?? '—'}</dd>
                </div>
                <div>
                  <dt>Correo</dt>
                  <dd>{s.correo ?? '—'}</dd>
                </div>
                <div>
                  <dt>Dentista asignado</dt>
                  <dd>{s.dentista}</dd>
                </div>
                {s.motivo && (
                  <div className={estilos.ancho}>
                    <dt>Motivo</dt>
                    <dd>{s.motivo}</dd>
                  </div>
                )}
              </dl>
              {textoVencimiento(s.expiraEn) && <p className={estilos.vence}>{textoVencimiento(s.expiraEn)}</p>}
              <div className={estilos.acciones}>
                <Boton variante="secundario" onClick={() => setRechazando(s)} aria-label={`Rechazar la solicitud de ${s.paciente}`}>
                  Rechazar
                </Boton>
                <Boton
                  variante="principal"
                  icono="confirmar"
                  cargando={confirmar.isPending && confirmar.variables?.id === s.id}
                  textoCargando="Confirmando…"
                  onClick={() => confirmar.mutate(s)}
                  aria-label={`Confirmar la solicitud de ${s.paciente}`}
                >
                  Confirmar
                </Boton>
              </div>
            </li>
          ))}
        </ul>
      )}
      {rechazando && (
        <DialogoRechazo
          key={rechazando.id}
          solicitud={rechazando}
          alCerrar={() => setRechazando(null)}
          alRechazar={async () => {
            setAviso(`Se rechazó la solicitud de ${rechazando.paciente}. El horario quedó libre.`);
            setRechazando(null);
            await refrescar();
          }}
        />
      )}
    </div>
  );
}

function DialogoRechazo({ solicitud, alCerrar, alRechazar }: { solicitud: Solicitud; alCerrar: () => void; alRechazar: () => void }) {
  const [motivo, setMotivo] = useState(MOTIVOS_RECHAZO[0]!);
  const [nota, setNota] = useState('');
  const [error, setError] = useState<string | undefined>();
  const rechazar = useMutation({
    mutationFn: () =>
      enviarJson(`/citas/${solicitud.id}/cancelar`, 'PATCH', {
        motivo: motivo === 'Otro motivo' ? nota.trim() : nota.trim() ? `${motivo}. ${nota.trim()}` : motivo,
        canal: 'panel',
        version: solicitud.version,
      }),
    onSuccess: alRechazar,
  });
  return (
    <Dialogo
      abierto
      alCerrar={alCerrar}
      titulo="Rechazar solicitud"
      icono="alerta"
      completoEnCelular
      acciones={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Conservar
          </Boton>
          <Boton
            variante="peligro"
            cargando={rechazar.isPending}
            textoCargando="Rechazando…"
            onClick={() => {
              if (motivo === 'Otro motivo' && !nota.trim()) return setError('Escribe el motivo.');
              rechazar.mutate();
            }}
          >
            Rechazar
          </Boton>
        </>
      }
    >
      <div className={estilos.formulario}>
        <p className={estilos.cuando}>
          <b>{solicitud.paciente}</b> · {solicitud.servicio} · {fechaCorta(solicitud.fecha)}, {horaCorta(solicitud.hora)}
        </p>
        <CampoSeleccion etiqueta="Motivo" value={motivo} onChange={(e) => setMotivo(e.target.value)}>
          {MOTIVOS_RECHAZO.map((m) => (
            <option key={m}>{m}</option>
          ))}
        </CampoSeleccion>
        <CampoAreaTexto
          etiqueta={motivo === 'Otro motivo' ? 'Motivo' : 'Nota (opcional)'}
          ayuda="Solo la ve el personal. El aviso a la persona no incluye el motivo."
          maxLength={400}
          value={nota}
          error={error}
          onChange={(e) => {
            setNota(e.target.value);
            setError(undefined);
          }}
        />
        {rechazar.isError && <Aviso tono="error">{mensajeDe(rechazar.error)}</Aviso>}
      </div>
    </Dialogo>
  );
}
