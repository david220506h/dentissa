import { Aviso, Boton, CampoSeleccion, Dialogo, InsigniaEstadoCita } from '@dentissa/ui';
import { useState } from 'react';
import { fechaHoraCorta, isoDeApi, MOTIVOS_CANCELACION, type CitaAgenda } from './logica';
import estilos from './Agenda.module.css';

function Resumen({ cita, conDentista = false }: { cita: CitaAgenda; conDentista?: boolean }) {
  return (
    <div className={estilos.resumen}>
      <b>{cita.paciente}</b> · {cita.servicio}
      <br />
      {fechaHoraCorta(isoDeApi(cita.fecha), cita.hora)}
      {conDentista && ` · ${cita.dentista}`}
    </div>
  );
}

/**
 * Detalle de la cita con sus acciones posibles hoy: confirmar (pendiente) y cancelar (pendiente o
 * confirmada). NO está dibujado en el diseño: usa solo patrones aprobados (Dialogo, resumen de
 * CitaFormularios, insignia de estado y botones). Completar, no asistió, reagendar e historial llegan con
 * la Fase 2.
 */
export function DialogoCita({
  cita,
  alCerrar,
  alConfirmar,
  alPedirCancelacion,
  confirmando,
  error,
}: {
  cita: CitaAgenda | null;
  alCerrar: () => void;
  alConfirmar: () => void;
  alPedirCancelacion: () => void;
  confirmando: boolean;
  error: string | null;
}) {
  const activa = cita?.estado === 'pendiente' || cita?.estado === 'confirmada';
  return (
    <Dialogo
      abierto={cita !== null}
      alCerrar={alCerrar}
      titulo="Cita"
      icono="agenda"
      completoEnCelular
      acciones={
        activa && cita ? (
          <>
            <Boton variante="secundario" icono="cerrar" onClick={alPedirCancelacion}>
              Cancelar cita
            </Boton>
            {cita.estado === 'pendiente' && (
              <Boton variante="principal" icono="confirmar" onClick={alConfirmar} cargando={confirmando} textoCargando="Confirmando…">
                Confirmar cita
              </Boton>
            )}
          </>
        ) : undefined
      }
    >
      {cita && (
        <div className={estilos.dialogoCuerpo}>
          <Resumen cita={cita} conDentista />
          <div>
            <InsigniaEstadoCita estado={cita.estado} />
          </div>
          {error && (
            <Aviso tono="error" anunciar>
              {error}
            </Aviso>
          )}
        </div>
      )}
    </Dialogo>
  );
}

/**
 * Cancelar cita (CitaFormularios.dc.html). Sin la nota libre ni "Avisar por correo": la API guarda el
 * motivo también en el historial de la cita (y el diseño promete que el historial no lleva la nota), y el
 * correo llega con la Fase 2.
 */
export function DialogoCancelar({
  cita,
  alCerrar,
  alCancelar,
  cancelando,
  error,
}: {
  cita: CitaAgenda | null;
  alCerrar: () => void;
  alCancelar: (motivo: string) => void;
  cancelando: boolean;
  error: string | null;
}) {
  const [motivo, setMotivo] = useState<string>(MOTIVOS_CANCELACION[0]);
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
          <Boton variante="principal" icono="cerrar" onClick={() => alCancelar(motivo)} cargando={cancelando} textoCargando="Cancelando…">
            Cancelar cita
          </Boton>
        </>
      }
    >
      {cita && (
        <div className={estilos.dialogoCuerpo}>
          <Resumen cita={cita} />
          <CampoSeleccion id="motivo-cancelacion" etiqueta="Motivo de la cancelación" value={motivo} onChange={(e) => setMotivo(e.target.value)}>
            {MOTIVOS_CANCELACION.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </CampoSeleccion>
          {error && (
            <Aviso tono="error" anunciar>
              {error}
            </Aviso>
          )}
        </div>
      )}
    </Dialogo>
  );
}
