import { Aviso, Avatar, Boton, CargandoLista, Insignia, MensajeEstado, Tabla, type NombreIcono, type TonoInsignia } from '@dentissa/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { enviarJson, obtenerJson } from '../../app/api';
import { horaCorta, hoyIsoEnClinica, ZONA_CLINICA } from '../inicio/datos';
import { esRolPanel, NOMBRE_ROL, type RolPanel } from '../../app/permisos';
import { useSesion } from '../../app/sesion';
import estilos from './Equipo.module.css';

/** Lo que Equipo usa de UsuarioListaDto (GET /api/usuarios, solo administración). */
export interface UsuarioApi {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  rol: string;
  activo: boolean;
  /** Último inicio de sesión, ISO 8601 en UTC; null si nunca ha entrado. */
  ultimoAccesoEn?: string | null;
}

// Insignia de cada rol (Equipo.dc.html): color e ícono; el nombre del rol es el neutro del panel
const ROL: Record<RolPanel, { tono: TonoInsignia; icono: NombreIcono; descripcion: string }> = {
  admin: { tono: 'rosa', icono: 'auditoria', descripcion: 'Todo el panel, usuarios, configuración y auditoría. Consulta el expediente sin editarlo.' },
  asistente: { tono: 'azul', icono: 'equipo', descripcion: 'Agenda, solicitudes y pacientes. Ve alergias, nunca el expediente.' },
  // Lo que ve HOY (restricción temporal, plan 14.2): actualizar cuando tenga su agenda y el expediente (plan 14.8)
  dentista: { tono: 'verde', icono: 'diente', descripcion: 'Su inicio, con sus citas del día.' },
};

const nombreCompleto = (u: UsuarioApi) => `${u.nombre} ${u.apellido}`.trim();

const MESES_CORTOS = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

/** "Hoy, 8:02", "Ayer, 18:40" o "2 oct 2026" (Equipo.dc.html), en la zona de la clínica; "Sin accesos" si nunca entró. */
export function ultimoAcceso(iso: string | null | undefined, ahora: Date): string {
  if (!iso) return 'Sin accesos';
  const instante = new Date(iso);
  const dia = hoyIsoEnClinica(instante);
  const hora = horaCorta(new Intl.DateTimeFormat('es-MX', { timeZone: ZONA_CLINICA, hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(instante));
  if (dia === hoyIsoEnClinica(ahora)) return `Hoy, ${hora}`;
  if (dia === hoyIsoEnClinica(new Date(ahora.getTime() - 86_400_000))) return `Ayer, ${hora}`;
  const [a, m, d] = dia.split('-').map(Number);
  return `${d} ${MESES_CORTOS[m! - 1]} ${a}`;
}

function InsigniaRol({ rol }: { rol: RolPanel }) {
  return (
    <Insignia tono={ROL[rol].tono} icono={ROL[rol].icono}>
      {NOMBRE_ROL[rol]}
    </Insignia>
  );
}

function InsigniaEstado({ activo }: { activo: boolean }) {
  return activo ? <Insignia tono="verde">Activo</Insignia> : <Insignia tono="gris">Inactivo</Insignia>;
}

/**
 * Equipo y usuarios (Equipo.dc.html), solo administración. Con la API actual: lista del personal
 * (GET /api/usuarios, sin pacientes), desactivar y reactivar (PATCH). No se muestran, por no tener
 * backend: "Invitar usuario", editar nombre, correo y rol, ni "Enviar enlace para nueva contraseña".
 */
export function Equipo() {
  const queryClient = useQueryClient();
  const { usuario: yo } = useSesion();
  const [elegidoId, setElegidoId] = useState<number | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [ahora] = useState(() => new Date());
  const tituloPanel = useRef<HTMLHeadingElement>(null);

  const usuarios = useQuery({ queryKey: ['usuarios'], queryFn: () => obtenerJson<UsuarioApi[]>('/usuarios') });
  // Solo el personal: las cuentas de pacientes no se administran aquí
  const personal = (usuarios.data ?? []).filter((u): u is UsuarioApi & { rol: RolPanel } => esRolPanel(u.rol));
  const elegido = personal.find((u) => u.id === elegidoId) ?? null;

  const cambiarAcceso = useMutation({
    mutationFn: (u: UsuarioApi) => enviarJson(`/usuarios/${u.id}/${u.activo ? 'desactivar' : 'activar'}`, 'PATCH'),
    onSuccess: async (_, u) => {
      // Sin género: el mensaje habla de la cuenta, no de la persona
      setAviso(u.activo ? `La cuenta de ${nombreCompleto(u)} quedó inactiva. Sus sesiones se cerraron.` : `La cuenta de ${nombreCompleto(u)} quedó activa.`);
      await queryClient.invalidateQueries({ queryKey: ['usuarios'] });
    },
  });

  // Al elegir a alguien, el foco va al panel (en celular queda debajo de la tabla)
  useEffect(() => {
    if (elegidoId !== null) {
      tituloPanel.current?.focus();
      tituloPanel.current?.scrollIntoView?.({ block: 'nearest' });
    }
  }, [elegidoId]);

  const adminsActivos = personal.filter((u) => u.rol === 'admin' && u.activo).length;
  const esUltimoAdmin = !!elegido && elegido.rol === 'admin' && elegido.activo && adminsActivos <= 1;
  const esMiCuenta = !!elegido && elegido.id === yo?.id;
  const bloqueado = !!elegido?.activo && (esUltimoAdmin || esMiCuenta);

  return (
    <div className={estilos.pagina}>
      <header>
        <h1 className={estilos.titulo}>Equipo y usuarios</h1>
        <p className={estilos.subtitulo}>Quién puede entrar al panel y con qué permisos</p>
      </header>

      <Aviso tono="info">Los cambios de rol y las desactivaciones quedan en la auditoría con quién los hizo y cuándo.</Aviso>
      {aviso && (
        <Aviso tono="exito" anunciar alCerrar={() => setAviso(null)}>
          {aviso}
        </Aviso>
      )}

      {/* Escritorio: tabla y roles a la izquierda, panel a la derecha (Equipo.dc.html). Celular: el panel justo
          después de la tabla, antes de los roles, para que quede cerca de "Editar". */}
      <div className={`${estilos.columnas} ${elegido ? estilos.conPanel : ''}`}>
          <section className={`${estilos.tarjeta} ${estilos.areaTabla}`}>
            <div className={estilos.encabezado}>
              <div>
                <h2 id="titulo-usuarios" className={estilos.tituloTarjeta}>
                  Usuarios de la clínica
                </h2>
                <p className={estilos.descripcion}>Cada persona entra con su propio correo. Nadie comparte cuenta.</p>
              </div>
            </div>
            {usuarios.isPending && <CargandoLista etiqueta="Cargando usuarios" />}
            {usuarios.isError && (
              <MensajeEstado
                nivel={3}
                icono="sinConexion"
                titulo="No pudimos cargar los usuarios"
                texto="Revisa tu conexión con la clínica e inténtalo de nuevo. No se perdió ningún dato."
                accion={
                  <Boton variante="principal" icono="reintentar" onClick={() => void usuarios.refetch()} cargando={usuarios.isFetching}>
                    Reintentar
                  </Boton>
                }
              />
            )}
            {usuarios.data && (
              <Tabla etiqueta="Usuarios de la clínica" columnas={['Persona', 'Rol', 'Estado', 'Último acceso', '']} anchoMinimo={600}>
                {personal.map((u) => (
                  <tr key={u.id} className={u.id === elegidoId ? estilos.filaElegida : undefined}>
                    <td>
                      <div className={estilos.persona}>
                        <Avatar nombre={nombreCompleto(u)} tamano={36} />
                        <div className={estilos.personaDatos}>
                          <div className={estilos.personaNombre}>{nombreCompleto(u)}</div>
                          <div className={estilos.personaCorreo}>
                            {/* Si no cabe junto al panel, el correo solo se parte antes de la arroba */}
                            {u.email.split('@')[0]}
                            <wbr />@{u.email.split('@').slice(1).join('@')}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <InsigniaRol rol={u.rol} />
                    </td>
                    <td>
                      <InsigniaEstado activo={u.activo} />
                    </td>
                    <td className={estilos.acceso}>{ultimoAcceso(u.ultimoAccesoEn, ahora)}</td>
                    <td className={estilos.accion}>
                      {/* "Acceso" (no "Editar") mientras no exista el endpoint para editar nombre, correo y rol */}
                      <Boton
                        variante="texto"
                        aria-label={`Acceso de ${nombreCompleto(u)}`}
                        onClick={() => {
                          cambiarAcceso.reset();
                          setAviso(null);
                          setElegidoId(u.id);
                        }}
                      >
                        Acceso
                      </Boton>
                    </td>
                  </tr>
                ))}
              </Tabla>
            )}
          </section>


        {elegido && (
          <div className={estilos.lateral}>
            <section className={estilos.tarjeta} aria-labelledby="titulo-acceso">
              <div className={estilos.encabezado}>
                <h2 id="titulo-acceso" className={estilos.tituloTarjeta} ref={tituloPanel} tabIndex={-1}>
                  Acceso
                </h2>
                <Insignia tono="azul">{nombreCompleto(elegido)}</Insignia>
              </div>
              <dl className={estilos.datos}>
                <div>
                  <dt>Correo</dt>
                  <dd>{elegido.email}</dd>
                </div>
                <div>
                  <dt>Rol</dt>
                  <dd>
                    <InsigniaRol rol={elegido.rol} />
                  </dd>
                </div>
                <div>
                  <dt>Último acceso</dt>
                  <dd>{ultimoAcceso(elegido.ultimoAccesoEn, ahora)}</dd>
                </div>
                <div>
                  <dt>Estado</dt>
                  <dd>
                    <InsigniaEstado activo={elegido.activo} />
                  </dd>
                </div>
              </dl>
              <div className={estilos.accesoAcciones}>
                {esUltimoAdmin && (
                  <Aviso tono="advertencia">
                    Es la única cuenta de administración activa. Para desactivarla, primero debe haber otra cuenta de administración activa.
                  </Aviso>
                )}
                {esMiCuenta && !esUltimoAdmin && <Aviso tono="advertencia">No puedes desactivar tu propia cuenta.</Aviso>}
                <Boton
                  variante="secundario"
                  icono={elegido.activo ? 'cerrar' : 'confirmar'}
                  anchoCompleto
                  disabled={bloqueado}
                  cargando={cambiarAcceso.isPending}
                  textoCargando={elegido.activo ? 'Desactivando…' : 'Activando…'}
                  onClick={() => cambiarAcceso.mutate(elegido)}
                >
                  {elegido.activo ? 'Desactivar usuario' : 'Activar usuario'}
                </Boton>
                {elegido.activo && <p className={estilos.nota}>Desactivar cierra sus sesiones al instante. Su historial se conserva.</p>}
                {cambiarAcceso.isError && (
                  <Aviso tono="error" anunciar>
                    {cambiarAcceso.error instanceof Error ? cambiarAcceso.error.message : 'No se pudo completar la acción.'}
                  </Aviso>
                )}
              </div>
            </section>
          </div>
        )}

          <section className={`${estilos.tarjeta} ${estilos.areaRoles}`} aria-labelledby="titulo-roles">
            <div className={estilos.encabezado}>
              <h2 id="titulo-roles" className={estilos.tituloTarjeta}>
                Qué puede hacer cada rol
              </h2>
            </div>
            <ul className={estilos.roles}>
              {(['admin', 'asistente', 'dentista'] as const).map((r) => (
                <li key={r} className={estilos.rol}>
                  <InsigniaRol rol={r} />
                  <span>{ROL[r].descripcion}</span>
                </li>
              ))}
            </ul>
          </section>
      </div>
    </div>
  );
}
