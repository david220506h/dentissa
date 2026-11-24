import {
  Boton,
  CargandoLista,
  InsigniaEstadoCita,
  MensajeEstado,
  TarjetaIndicador,
  TarjetaIndicadorCompacta,
} from '@dentissa/ui';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { obtenerJson } from '../../app/api';
import { useSesion } from '../../app/sesion';
import {
  citasDeHoy,
  citasSinConfirmar,
  fechaLarga,
  hoyIsoEnClinica,
  horaCorta,
  rangoHorario,
  saludo,
  type CitaApi,
} from './datos';
import estilos from './Inicio.module.css';

/**
 * Inicio (Inicio.dc.html en escritorio; Movil.dc.html en celular). Solo muestra lo que la API respalda:
 * - Recepción (administración y asistente): GET /api/citas → citas de hoy y citas sin confirmar.
 * - Dentista: GET /api/citas/mi-agenda (solo hoy) → sus citas de hoy.
 * No se dibujan, porque la API aún no los tiene: solicitudes, existencias bajas, "de N horarios del día",
 * horarios libres y descansos, "Por llamar" con teléfono y correo, buscador, notificaciones,
 * "Nueva cita", "Agendar" y la invitación a instalar.
 */
export function Inicio() {
  const { usuario } = useSesion();
  // La hora se fija al montar: la pantalla se vuelve a montar al navegar y los datos se refrescan al volver a la pestaña
  const [ahora] = useState(() => new Date());
  const esDentista = usuario?.rol === 'dentista';

  const consulta = useQuery({
    queryKey: esDentista ? ['mi-agenda', hoyIsoEnClinica(ahora)] : ['citas'],
    queryFn: () => {
      const hoy = hoyIsoEnClinica(ahora);
      return obtenerJson<CitaApi[]>(esDentista ? `/citas/mi-agenda?desde=${hoy}&hasta=${hoy}` : '/citas');
    },
  });

  if (!usuario) return null;
  const deHoy = consulta.data ? citasDeHoy(consulta.data, ahora) : [];
  const sinConfirmar = consulta.data && !esDentista ? citasSinConfirmar(consulta.data, ahora) : null;

  return (
    <div className={estilos.pagina}>
      <header>
        <h1 className={estilos.titulo}>
          <span className={estilos.soloEscritorio}>Inicio</span>
          <span className={estilos.soloCelular}>{`${saludo(ahora)}, ${usuario.nombre}`}</span>
        </h1>
        <p className={estilos.fecha}>
          <span className={estilos.soloEscritorio}>{fechaLarga(ahora, true)}</span>
          <span className={estilos.soloCelular}>{fechaLarga(ahora, false)}</span>
        </p>
      </header>

      {consulta.data && (
        <>
          <div className={`${estilos.indicadores} ${estilos.soloEscritorio}`}>
            <TarjetaIndicador etiqueta="Citas de hoy" valor={deHoy.length} icono="agenda" />
            {sinConfirmar !== null && (
              <TarjetaIndicador
                etiqueta="Citas sin confirmar"
                valor={sinConfirmar}
                icono="telefono"
                detalle="Pendientes de llamada"
              />
            )}
          </div>
          {sinConfirmar !== null && (
            <div className={`${estilos.compactas} ${estilos.soloCelular}`}>
              <TarjetaIndicadorCompacta etiqueta="Por llamar" valor={sinConfirmar} icono="telefono" />
            </div>
          )}
        </>
      )}

      <section className={estilos.agenda} aria-labelledby="titulo-agenda-hoy">
        <h2 id="titulo-agenda-hoy" className={estilos.agendaTitulo}>
          <span className={estilos.soloEscritorio}>Agenda de hoy</span>
          <span className={estilos.soloCelular}>Hoy</span>
        </h2>
        {consulta.isPending && <CargandoLista etiqueta="Cargando citas" />}
        {consulta.isError && (
          <MensajeEstado
            nivel={3}
            icono="sinConexion"
            titulo="No pudimos cargar las citas"
            texto="Revisa tu conexión con la clínica e inténtalo de nuevo. No se perdió ningún dato."
            accion={
              <Boton variante="principal" icono="reintentar" onClick={() => void consulta.refetch()} cargando={consulta.isFetching}>
                Reintentar
              </Boton>
            }
          />
        )}
        {consulta.data && deHoy.length === 0 && (
          <MensajeEstado nivel={3} icono="agenda" titulo="No hay citas este día" texto="Es un día sin citas." />
        )}
        {deHoy.length > 0 && (
          <ul className={estilos.lista}>
            {deHoy.map((c) => (
              <li key={c.id} className={estilos.fila}>
                <span className={estilos.hora}>
                  <span className={estilos.soloEscritorio}>{rangoHorario(c)}</span>
                  <span className={estilos.soloCelular}>{horaCorta(c.hora)}</span>
                </span>
                <span className={estilos.contenido}>
                  <span className={estilos.datos}>
                    <span className={estilos.paciente}>{c.paciente}</span>
                    <span className={`${estilos.servicio} ${estilos.soloEscritorio}`}>{c.servicio}</span>
                  </span>
                  <InsigniaEstadoCita estado={c.estado} />
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
