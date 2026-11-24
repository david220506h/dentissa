import { Aviso, Avatar, Boton, CargandoLista, Insignia, MensajeEstado, Tabla, type NombreIcono, type TonoInsignia } from '@dentissa/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { enviarJson, obtenerJson } from '../../app/api';
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
}

// Insignia de cada rol (Equipo.dc.html): color e ícono; el nombre del rol es el neutro del panel
const ROL: Record<RolPanel, { tono: TonoInsignia; icono: NombreIcono; descripcion: string }> = {
  admin: { tono: 'rosa', icono: 'auditoria', descripcion: 'Todo el panel, usuarios, configuración y auditoría. Consulta el expediente sin editarlo.' },
  asistente: { tono: 'azul', icono: 'equipo', descripcion: 'Agenda, solicitudes y pacientes. Ve alergias, nunca el expediente.' },
  dentista: { tono: 'verde', icono: 'diente', descripcion: 'Su agenda y el expediente clínico completo.' },
};

const nombreCompleto = (u: UsuarioApi) => `${u.nombre} ${u.apellido}`.trim();

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
 * backend: "Invitar usuario", editar nombre, correo y rol, "Enviar enlace para nueva contraseña", ni la
 * columna "Último acceso" (la base lo guarda, pero la API no lo devuelve).
 */
export function Equipo() {
  const queryClient = useQueryClient();
  const { usuario: yo } = useSesion();
  const [elegidoId, setElegidoId] = useState<number | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const tituloPanel = useRef<HTMLHeadingElement>(null);

  const usuarios = useQuery({ queryKey: ['usuarios'], queryFn: () => obtenerJson<UsuarioApi[]>('/usuarios') });
  // Solo el personal: las cuentas de pacientes no se administran aquí
  const personal = (usuarios.data ?? []).filter((u): u is UsuarioApi & { rol: RolPanel } => esRolPanel(u.rol));
  const elegido = personal.find((u) => u.id === elegidoId) ?? null;

  const cambiarAcceso = useMutation({
    mutationFn: (u: UsuarioApi) => enviarJson(`/usuarios/${u.id}/${u.activo ? 'desactivar' : 'activar'}`, 'PATCH'),
    onSuccess: async (_, u) => {
      setAviso(u.activo ? `${nombreCompleto(u)} quedó inactivo. Sus sesiones se cerraron.` : `${nombreCompleto(u)} quedó activo.`);
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
              <Tabla etiqueta="Usuarios de la clínica" columnas={['Persona', 'Rol', 'Estado', '']} anchoMinimo={560}>
                {personal.map((u) => (
                  <tr key={u.id} className={u.id === elegidoId ? estilos.filaElegida : undefined}>
                    <td>
                      <div className={estilos.persona}>
                        <Avatar nombre={nombreCompleto(u)} tamano={36} />
                        <div className={estilos.personaDatos}>
                          <div className={estilos.personaNombre}>{nombreCompleto(u)}</div>
                          <div className={estilos.personaCorreo}>{u.email}</div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <InsigniaRol rol={u.rol} />
                    </td>
                    <td>
                      <InsigniaEstado activo={u.activo} />
                    </td>
                    <td className={estilos.accion}>
                      <Boton
                        variante="texto"
                        icono="editar"
                        aria-label={`Editar ${nombreCompleto(u)}`}
                        onClick={() => {
                          cambiarAcceso.reset();
                          setAviso(null);
                          setElegidoId(u.id);
                        }}
                      >
                        Editar
                      </Boton>
                    </td>
                  </tr>
                ))}
              </Tabla>
            )}
          </section>


        {elegido && (
          <div className={estilos.lateral}>
            <section className={estilos.tarjeta} aria-labelledby="titulo-editar">
              <div className={estilos.encabezado}>
                <h2 id="titulo-editar" className={estilos.tituloTarjeta} ref={tituloPanel} tabIndex={-1}>
                  Editar usuario
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
                  <dt>Estado</dt>
                  <dd>
                    <InsigniaEstado activo={elegido.activo} />
                  </dd>
                </div>
              </dl>
              <div className={estilos.acceso}>
                <h3 className={estilos.accesoTitulo}>Acceso</h3>
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
