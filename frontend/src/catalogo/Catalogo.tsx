import {
  Aviso,
  Avatar,
  Boton,
  Busqueda,
  CargandoLista,
  ChipAlergia,
  CampoAreaTexto,
  CampoContrasena,
  CampoSeleccion,
  CampoTexto,
  Contador,
  Dialogo,
  EncabezadoPagina,
  GrupoVista,
  GruposMenu,
  HojaMas,
  Icono,
  Insignia,
  MensajeEstado,
  InsigniaEstadoCita,
  MenuInferior,
  MenuLateral,
  NOMBRES_ICONOS,
  Odontograma,
  Paginacion,
  Pestanas,
  SinConexion,
  Tabla,
  Tarjeta,
  TarjetaPaciente,
  TarjetaIndicador,
  type ComponenteEnlace,
  type EstadoCita,
  type GrupoMenu,
} from '@dentissa/ui';
import { useState, type ReactNode } from 'react';
import estilos from './Catalogo.module.css';

// Datos de ejemplo tomados del propio diseño (docs/design/panel). No son datos reales.
const GRUPOS: GrupoMenu[] = [
  {
    titulo: 'Principal',
    elementos: [
      { id: 'inicio', etiqueta: 'Inicio', icono: 'inicio', href: '#' },
      { id: 'agenda', etiqueta: 'Agenda', icono: 'agenda', href: '#' },
      {
        id: 'solicitudes',
        etiqueta: 'Solicitudes',
        icono: 'solicitudes',
        href: '#',
        contador: { valor: 5, descripcion: '5 solicitudes por revisar' },
      },
      { id: 'pacientes', etiqueta: 'Pacientes', icono: 'pacientes', href: '#' },
    ],
  },
  {
    titulo: 'Clínica',
    elementos: [
      { id: 'expediente', etiqueta: 'Expediente clínico', icono: 'expediente', href: '#' },
      { id: 'servicios', etiqueta: 'Servicios', icono: 'servicios', href: '#' },
      { id: 'ofertas', etiqueta: 'Ofertas y contenido', icono: 'ofertas', href: '#' },
      { id: 'inventario', etiqueta: 'Inventario', icono: 'inventario', href: '#' },
      { id: 'pagos', etiqueta: 'Pagos', icono: 'pagos', href: '#' },
    ],
  },
  {
    titulo: 'Administración',
    elementos: [
      { id: 'reportes', etiqueta: 'Reportes', icono: 'reportes', href: '#' },
      { id: 'equipo', etiqueta: 'Equipo y usuarios', icono: 'equipo', href: '#' },
      { id: 'configuracion', etiqueta: 'Configuración', icono: 'configuracion', href: '#' },
      { id: 'auditoria', etiqueta: 'Auditoría', icono: 'auditoria', href: '#' },
    ],
  },
];

// Piezas de ejemplo del diseño (Expediente.dc.html)
const PIEZAS = {
  18: 'ausente', 16: 'restaurado', 11: 'corona', 21: 'corona', 24: 'restaurado', 26: 'restaurado', 28: 'ausente',
  48: 'ausente', 46: 'endodoncia', 35: 'caries', 36: 'caries', 38: 'ausente',
} as const;

const ESTADOS: EstadoCita[] = ['confirmada', 'pendiente', 'completada', 'cancelada', 'no_asistio'];

function Seccion({ titulo, fuente, children }: { titulo: string; fuente: string; children: ReactNode }) {
  return (
    <section className={estilos.seccion} aria-labelledby={`sec-${titulo}`}>
      <h2 id={`sec-${titulo}`} className={estilos.tituloSeccion}>
        {titulo}
      </h2>
      <p className={estilos.fuente}>Fuente: {fuente}</p>
      {children}
    </section>
  );
}

/** "¿Sigues ahí?" (Estados · Cierre por inactividad), con cuenta fija de ejemplo. */
function DialogoDeMuestra() {
  const [abierto, setAbierto] = useState(false);
  return (
    <>
      <Boton variante="secundario" icono="reloj" onClick={() => setAbierto(true)}>
        Ver aviso de inactividad
      </Boton>
      <Dialogo
        abierto={abierto}
        alCerrar={() => setAbierto(false)}
        titulo="¿Sigues ahí?"
        icono="reloj"
        tamano="chico"
        acciones={
          <>
            <Boton variante="secundario" icono="cerrarSesion" onClick={() => setAbierto(false)}>
              Cerrar sesión
            </Boton>
            <Boton variante="principal" icono="confirmar" onClick={() => setAbierto(false)}>
              Mantener sesión
            </Boton>
          </>
        }
      >
        <p>
          Por seguridad, cerraremos tu sesión en <b>60 segundos</b>. No se pierde nada de lo guardado.
        </p>
      </Dialogo>
    </>
  );
}

const ENLACE_MUESTRA: ComponenteEnlace = ({ href, children, ...resto }) => (
  <a href={href} {...resto}>
    {children}
  </a>
);

/** Hoja "Más" de la administradora (Movil2), con los datos de ejemplo del diseño. */
function HojaDeMuestra() {
  const [abierta, setAbierta] = useState(false);
  return (
    <>
      <Boton variante="secundario" icono="menu" onClick={() => setAbierta(true)}>
        Ver hoja Más
      </Boton>
      <HojaMas
        abierta={abierta}
        alCerrar={() => setAbierta(false)}
        usuario={{ nombre: 'Laura Mendoza', rol: 'Administradora' }}
        elementos={[
          { id: 'servicios', etiqueta: 'Servicios', icono: 'servicios', href: '#' },
          { id: 'inventario', etiqueta: 'Inventario', icono: 'inventario', href: '#' },
          { id: 'pagos', etiqueta: 'Pagos', icono: 'pagos', href: '#' },
          { id: 'reportes', etiqueta: 'Reportes', icono: 'reportes', href: '#' },
          { id: 'equipo', etiqueta: 'Equipo y usuarios', icono: 'equipo', href: '#' },
          { id: 'configuracion', etiqueta: 'Configuración', icono: 'configuracion', href: '#' },
          { id: 'auditoria', etiqueta: 'Auditoría', icono: 'auditoria', href: '#' },
        ]}
        alCerrarSesion={() => setAbierta(false)}
        enlace={ENLACE_MUESTRA}
      />
    </>
  );
}

export function Catalogo() {
  const [vista, setVista] = useState<'dia' | 'semana'>('semana');
  const [pieza, setPieza] = useState(36);
  return (
    <main className={estilos.pagina}>
      <EncabezadoPagina
        titulo="Catálogo de componentes"
        subtitulo="Solo en desarrollo. Cada componente sale de docs/design/panel; los datos son de ejemplo del diseño."
      />

      <Seccion titulo="Botones" fuente="Main · Componentes · Botones">
        <div className={estilos.muestra}>
          <Boton variante="principal" icono="confirmar">
            Confirmar cita
          </Boton>
          <Boton variante="secundario">Proponer otro horario</Boton>
          <Boton variante="peligro" icono="cerrar">
            Rechazar solicitud
          </Boton>
          <Boton variante="texto" icono="siguiente">
            Ver agenda
          </Boton>
          <Boton disabled>Sin teléfono</Boton>
          <Boton variante="principal" cargando>
            Guardar cambios
          </Boton>
          <Boton variante="principal" grande>
            Entrar
          </Boton>
        </div>
      </Seccion>

      <Seccion titulo="Insignias y contadores" fuente="Main · Insignias de estado; Elementos del menú">
        <div className={estilos.muestra}>
          {ESTADOS.map((e) => (
            <InsigniaEstadoCita key={e} estado={e} />
          ))}
          <Insignia tono="azul">Con cuenta</Insignia>
          <Insignia tono="gris">Invitado</Insignia>
          {/* Main conserva esta insignia roja suave de muestra; la alergia real usa ChipAlergia */}
          <Insignia tono="rojo">Penicilina</Insignia>
          <Contador valor={5} descripcion="5 solicitudes por revisar" />
          <Avatar nombre="Laura Mendoza" />
          <Avatar nombre="Valeria Núñez" tamano={44} />
        </div>
      </Seccion>

      <Seccion titulo="Campos" fuente="Main · Campos; Login">
        <div className={`${estilos.muestra} ${estilos.columna}`}>
          <div className={estilos.rejilla}>
            <CampoTexto etiqueta="Correo electrónico" type="email" placeholder="nombre@ejemplo.com" />
            <CampoTexto etiqueta="Con error" defaultValue="valeria@" error="Escribe un correo válido." />
            <CampoTexto etiqueta="Con ayuda" ayuda="Diez dígitos, sin espacios." />
            <CampoContrasena etiqueta="Contraseña" placeholder="Tu contraseña" grande />
            <CampoSeleccion etiqueta="Dentista">
              <option>Dra. Ana Torres</option>
            </CampoSeleccion>
            <CampoTexto etiqueta="Deshabilitado" disabled defaultValue="No editable" />
            <CampoAreaTexto etiqueta="Nota clínica" defaultValue="Caries oclusal profunda sin sintomatología." />
            <Busqueda etiqueta="Buscar" placeholder="Buscar paciente o cita" />
          </div>
        </div>
      </Seccion>

      <Seccion titulo="Avisos" fuente="Main · Avisos">
        <div className={`${estilos.muestra} ${estilos.columna}`}>
          <Aviso tono="info" titulo="Información">
            Esta lista solo muestra datos generales.
          </Aviso>
          <Aviso tono="exito" titulo="Cita confirmada" anunciar>
            Se registró la confirmación por llamada.
          </Aviso>
          <Aviso tono="advertencia" titulo="Horario bloqueado" icono="reloj">
            Se libera mañana a las 10:42.
          </Aviso>
          <Aviso tono="error" titulo="No se pudo guardar" anunciar>
            Ese horario ya tiene una cita.
          </Aviso>
        </div>
      </Seccion>

      <Seccion titulo="Estados" fuente="Estados">
        <div className={estilos.rejilla}>
          <div className={estilos.tarjetaEstado}>
            <MensajeEstado
              icono="agenda"
              titulo="No hay citas este día"
              texto="Es un día sin citas. Puedes agendar una o revisar otro día."
              accion={
                <Boton variante="principal" icono="agregar">
                  Nueva cita
                </Boton>
              }
            />
          </div>
          <div className={estilos.tarjetaEstado}>
            <CargandoLista etiqueta="Cargando citas" />
          </div>
          <div className={estilos.tarjetaEstado}>
            <MensajeEstado
              icono="sinConexion"
              titulo="No pudimos cargar las citas"
              texto="Revisa tu conexión con la clínica e inténtalo de nuevo. No se perdió ningún dato."
              accion={
                <Boton variante="principal" icono="reintentar">
                  Reintentar
                </Boton>
              }
            />
          </div>
          <div className={estilos.tarjetaEstado}>
            <MensajeEstado
              icono="auditoria"
              titulo="No tienes acceso a esta sección"
              texto="Tu rol no incluye Equipo y usuarios. Si lo necesitas, pídeselo a la administración."
              accion={
                <Boton variante="secundario" icono="inicio">
                  Volver al inicio
                </Boton>
              }
            />
          </div>
        </div>
        <div className={`${estilos.muestra} ${estilos.columna}`}>
          <Aviso tono="exito" alCerrar={() => {}}>
            Cita confirmada. Se envió el correo a Rosa Aguilar.
          </Aviso>
          <Aviso tono="info" alCerrar={() => {}}>
            El horario de las 15:00 se liberará hoy a las 13:00.
          </Aviso>
          <Aviso tono="advertencia">
            Hay 2 solicitudes por vencer en menos de 2 horas.
          </Aviso>
          <Aviso tono="error">No se pudo enviar el correo. Reintentaremos en 5 minutos; puedes llamar para avisar.</Aviso>
        </div>
        <div className={estilos.muestra}>
          <DialogoDeMuestra />
        </div>
      </Seccion>

      <Seccion titulo="Tarjetas" fuente="Inicio">
        <div className={estilos.rejilla}>
          <TarjetaIndicador etiqueta="Citas de hoy" valor={5} icono="agenda" detalle="de 6 horarios del día" />
          <TarjetaIndicador etiqueta="Citas sin confirmar" valor={2} icono="telefono" detalle="Pendientes de llamada" />
        </div>
        <Tarjeta titulo="Agenda de hoy" holgada accion={<Boton variante="texto" icono="siguiente">Ver agenda completa</Boton>}>
          <p className={estilos.fuente}>Contenido de la tarjeta.</p>
        </Tarjeta>
      </Seccion>

      <Seccion titulo="Encabezado, pestañas y vista" fuente="Agenda; Solicitudes">
        <div className={`${estilos.muestra} ${estilos.columna}`}>
          <GrupoVista
            etiqueta="Vista"
            valor={vista}
            alCambiar={setVista}
            opciones={[
              { valor: 'dia', texto: 'Día' },
              { valor: 'semana', texto: 'Semana' },
            ]}
          />
          <Pestanas
            etiqueta="Tipo de solicitud"
            pestanas={[
              { id: 'pendientes', etiqueta: 'Pendientes', contador: 5, descripcionContador: 'solicitudes', contenido: null },
              { id: 'cambios', etiqueta: 'Cambios solicitados', contador: 1, descripcionContador: 'cambio', contenido: null },
              { id: 'historial', etiqueta: 'Historial', contenido: null },
            ]}
          />
        </div>
      </Seccion>

      <Seccion titulo="Tabla y paginación" fuente="Pacientes">
        <Tabla
          etiqueta="Pacientes de ejemplo"
          columnas={['Paciente', 'Teléfono', 'Cuenta', 'Alergias', '']}
          anchoMinimo={860}
          pie={<Paginacion desde={1} hasta={2} hayAnterior={false} haySiguiente alAnterior={() => {}} alSiguiente={() => {}} />}
        >
          <tr>
            <td>Rosa Aguilar</td>
            <td>55 5555 0101</td>
            <td>
              <Insignia tono="azul">Con cuenta</Insignia>
            </td>
            <td>Sin registro</td>
            <td>
              <Boton variante="texto" icono="siguiente" aria-label="Ver ficha de Rosa Aguilar">
                Ver ficha
              </Boton>
            </td>
          </tr>
          <tr>
            <td>Ernesto Vega</td>
            <td>55 5555 0148</td>
            <td>
              <Insignia tono="azul">Con cuenta</Insignia>
            </td>
            <td>
              <ChipAlergia alergia="Penicilina" />
            </td>
            <td>
              <Boton variante="texto" icono="siguiente" aria-label="Ver ficha de Ernesto Vega">
                Ver ficha
              </Boton>
            </td>
          </tr>
        </Tabla>
      </Seccion>

      <Seccion titulo="Pacientes en tarjetas" fuente="Movil2">
        <div className={estilos.listaTarjetas}>
          <TarjetaPaciente nombre="Rosa Aguilar" telefono="55 5555 0101" cuenta="con-cuenta" detalle="Próxima: 14 dic" href="#" />
          <TarjetaPaciente
            nombre="Ernesto Vega"
            telefono="55 5555 0148"
            cuenta="con-cuenta"
            alergia="Penicilina"
            detalle="Próxima: 18 nov"
            href="#"
          />
          <TarjetaPaciente nombre="Valeria Núñez" cuenta="invitado" detalle="Solicitada: 19 nov" href="#" />
          <TarjetaPaciente
            nombre="Mario Delgado"
            telefono="55 5555 0133"
            cuenta="con-cuenta"
            alergia="Látex"
            detalle="Próxima: 18 nov"
            href="#"
          />
        </div>
      </Seccion>

      <Seccion titulo="Hoja Más" fuente="Movil2">
        <div className={estilos.muestra}>
          <HojaDeMuestra />
        </div>
      </Seccion>

      <Seccion titulo="Menú lateral" fuente="Inicio (desde 1024 px)">
        <div className={estilos.marcoLateral}>
          <MenuLateral
            grupos={GRUPOS}
            activo="inicio"
            usuario={{ nombre: 'Laura Mendoza', rol: 'Administradora' }}
            alCerrarSesion={() => {}}
          />
        </div>
      </Seccion>

      <Seccion titulo="Menú inferior y página Más" fuente="Movil (por debajo de 1024 px); la hoja de Más no está diseñada">
        <div className={estilos.marcoMovil}>
          <GruposMenu grupos={GRUPOS.slice(1)} etiqueta="Más secciones" />
          <MenuInferior
            activo="agenda"
            elementos={[
              ...GRUPOS[0]!.elementos.map((e) =>
                e.id === 'solicitudes' ? { ...e, contador: { valor: 3, descripcion: '3 solicitudes por revisar' } } : e,
              ),
              { id: 'mas', etiqueta: 'Más', icono: 'menu', href: '#' },
            ]}
          />
        </div>
      </Seccion>

      <Seccion titulo="Sin conexión" fuente="Movil">
        <div className={estilos.marcoSinConexion}>
          <SinConexion alReintentar={() => {}} />
        </div>
      </Seccion>

      <Seccion titulo="Odontograma" fuente="Expediente (presentacional; el expediente llega con su backend)">
        <Tarjeta titulo="Odontograma" holgada>
          <Odontograma piezas={PIEZAS} seleccionada={pieza} alSeleccionar={setPieza} />
        </Tarjeta>
      </Seccion>

      <Seccion titulo="Íconos" fuente="Trazos copiados de docs/design/panel">
        <div className={estilos.iconos}>
          {NOMBRES_ICONOS.map((n) => (
            <div key={n} className={estilos.icono}>
              <Icono nombre={n} tamano={24} />
              {n}
            </div>
          ))}
        </div>
      </Seccion>
    </main>
  );
}
