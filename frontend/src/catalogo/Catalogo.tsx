import {
  Aviso,
  Avatar,
  Boton,
  Busqueda,
  CampoAreaTexto,
  CampoContrasena,
  CampoSeleccion,
  CampoTexto,
  Contador,
  EncabezadoPagina,
  GrupoVista,
  GruposMenu,
  Icono,
  Insignia,
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
  TarjetaIndicador,
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
          <Insignia tono="rojo" icono="alerta">
            Penicilina
          </Insignia>
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
          <Aviso tono="advertencia" titulo="Horario bloqueado">
            Se libera mañana a las 10:42.
          </Aviso>
          <Aviso tono="error" titulo="No se pudo guardar" anunciar alCerrar={() => {}}>
            Ese horario ya tiene una cita.
          </Aviso>
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
              <Insignia tono="rojo" icono="alerta">
                Penicilina
              </Insignia>
            </td>
            <td>
              <Boton variante="texto" icono="siguiente" aria-label="Ver ficha de Ernesto Vega">
                Ver ficha
              </Boton>
            </td>
          </tr>
        </Tabla>
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
