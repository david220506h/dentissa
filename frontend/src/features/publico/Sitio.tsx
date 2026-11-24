import { Aviso, Boton, CampoAreaTexto, CampoSeleccion, CampoTexto, Icono } from '@dentissa/ui';
import { QueryClient, QueryClientProvider, useMutation, useQuery } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';
import { Link, Route, Routes } from 'react-router-dom';
import { API_URL } from '../../services/authService';
import {
  cuerpo,
  errores,
  fechaLarga,
  FORMULARIO_VACIO,
  hoyEnClinica,
  precio,
  sumarDias,
  type DiaDisponible,
  type FormularioSolicitud,
  type ServicioPublico,
} from './logica';
import estilos from './Sitio.module.css';

// Sitio público de la clínica (sin sesión). Datos de la clínica: DE EJEMPLO (clínica ficticia de demostración).
const CLINICA = {
  nombre: 'Dentissa',
  direccion: 'Calle Ficticia 123, Col. Centro, Ciudad de México',
  telefono: '55 5550 0000',
  correo: 'contacto@clinica-demo.test',
};

class ErrorPublico extends Error {
  constructor(
    mensaje: string,
    public estado: number,
  ) {
    super(mensaje);
  }
}

async function pedir<T>(ruta: string, opciones?: RequestInit): Promise<T> {
  let r: Response;
  try {
    r = await fetch(`${API_URL}${ruta}`, { ...opciones, headers: { 'Content-Type': 'application/json', ...opciones?.headers } });
  } catch {
    throw new ErrorPublico('No pudimos conectar con la clínica. Revisa tu conexión e inténtalo de nuevo.', 0);
  }
  const datos = (await r.json().catch(() => null)) as { mensaje?: string } | null;
  if (!r.ok) {
    const mensaje =
      r.status === 429
        ? 'Recibimos muchas solicitudes desde tu conexión. Espera un momento o llámanos.'
        : r.status >= 500
          ? 'El sistema de citas no está disponible por ahora. Llámanos para agendar.'
          : (datos?.mensaje ?? 'Revisa los datos e inténtalo de nuevo.');
    throw new ErrorPublico(mensaje, r.status);
  }
  return datos as T;
}

const clientePublico = new QueryClient({ defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } } });

export function SitioPublico() {
  return (
    <QueryClientProvider client={clientePublico}>
      <Routes>
        <Route path="/" element={<Inicio />} />
        <Route path="/solicitar-cita" element={<SolicitarCita />} />
        <Route path="*" element={<NoEncontrada />} />
      </Routes>
    </QueryClientProvider>
  );
}

function Marco({ children }: { children: ReactNode }) {
  return (
    <div className={estilos.raiz}>
      <a href="#contenido" className={estilos.saltar}>
        Saltar al contenido
      </a>
      <p className={estilos.ejemplo} role="note">
        Sitio de demostración: la clínica y sus datos son de ejemplo.
      </p>
      <header className={estilos.encabezado}>
        <Link to="/" className={estilos.marca}>
          <span className={estilos.marcaIcono}>
            <Icono nombre="diente" />
          </span>
          {CLINICA.nombre}
        </Link>
        <nav aria-label="Sitio" className={estilos.nav}>
          <a href="/#servicios">Servicios</a>
          <a href="/#horario">Horario</a>
          <a href="/#contacto">Contacto</a>
          <a href="/panel" className={estilos.personal}>
            Acceso del personal
          </a>
        </nav>
      </header>
      <main id="contenido" className={estilos.principal}>
        {children}
      </main>
      <footer className={estilos.pie}>
        <p>
          {CLINICA.nombre} · {CLINICA.direccion} (datos de ejemplo)
        </p>
        <a href="/panel">Acceso del personal</a>
      </footer>
    </div>
  );
}

function BotonSolicitar() {
  return (
    <Link to="/solicitar-cita" className={estilos.cta}>
      <Icono nombre="agenda" tamano={20} />
      Solicitar cita
    </Link>
  );
}

function Inicio() {
  const servicios = useQuery({ queryKey: ['publico', 'servicios'], queryFn: () => pedir<ServicioPublico[]>('/publico/servicios') });
  return (
    <Marco>
      <section className={estilos.portada} aria-labelledby="titulo">
        <h1 id="titulo">Cuidamos tu sonrisa, sin prisas</h1>
        <p>
          Consultas de odontología general, ortodoncia y endodoncia en un solo lugar. Pide tu cita en línea y la clínica te
          contacta para confirmarla.
        </p>
        <BotonSolicitar />
      </section>

      <section id="servicios" className={estilos.seccion} aria-labelledby="t-servicios">
        <h2 id="t-servicios">Servicios</h2>
        {servicios.isPending && <p className={estilos.secundario}>Cargando servicios…</p>}
        {servicios.isError && <p className={estilos.secundario}>No pudimos cargar los servicios. Llámanos al {CLINICA.telefono}.</p>}
        {servicios.data && (
          <ul className={estilos.servicios}>
            {servicios.data.map((s) => (
              <li key={s.id} className={estilos.servicio}>
                <h3>{s.nombre}</h3>
                <p>
                  Desde {precio(s.precio)} · {s.duracionSlots > 1 ? `${s.duracionSlots} horas` : '1 hora'}
                </p>
              </li>
            ))}
          </ul>
        )}
      </section>

      <div className={estilos.columnas}>
        <section id="horario" className={estilos.seccion} aria-labelledby="t-horario">
          <h2 id="t-horario">Horario</h2>
          <dl className={estilos.horario}>
            <div>
              <dt>Lunes a sábado</dt>
              <dd>8:00 a 16:00</dd>
            </div>
            <div>
              <dt>Descansos</dt>
              <dd>11:00 a 12:00 y 14:00 a 15:00</dd>
            </div>
            <div>
              <dt>Domingo</dt>
              <dd>Cerrado</dd>
            </div>
          </dl>
        </section>
        <section id="contacto" className={estilos.seccion} aria-labelledby="t-contacto">
          <h2 id="t-contacto">Ubicación y contacto</h2>
          <p className={estilos.etiquetaEjemplo}>Datos de ejemplo</p>
          <ul className={estilos.contacto}>
            <li>
              <Icono nombre="inicio" tamano={20} />
              {CLINICA.direccion}
            </li>
            <li>
              <Icono nombre="telefono" tamano={20} />
              {CLINICA.telefono}
            </li>
            <li>
              <Icono nombre="notificaciones" tamano={20} />
              {CLINICA.correo}
            </li>
          </ul>
        </section>
      </div>

      <section className={estilos.cierre}>
        <h2>Agenda tu visita: elige el horario que te acomode</h2>
        <BotonSolicitar />
      </section>
    </Marco>
  );
}

function SolicitarCita() {
  const hoy = hoyEnClinica();
  const [f, setF] = useState<FormularioSolicitud>(FORMULARIO_VACIO);
  const [err, setErr] = useState<ReturnType<typeof errores>>({});
  const servicios = useQuery({ queryKey: ['publico', 'servicios'], queryFn: () => pedir<ServicioPublico[]>('/publico/servicios') });
  const disponibilidad = useQuery({
    queryKey: ['publico', 'disponibilidad', f.servicioId, f.fecha],
    queryFn: () => pedir<DiaDisponible[]>(`/publico/disponibilidad?servicioId=${f.servicioId}&desde=${f.fecha}&hasta=${f.fecha}`),
    enabled: !!f.servicioId && !!f.fecha,
  });
  const horas = disponibilidad.data?.[0]?.horas ?? [];
  const enviar = useMutation({
    mutationFn: () => pedir<{ mensaje: string }>('/publico/solicitudes', { method: 'POST', body: JSON.stringify(cuerpo(f)) }),
    onError: (e) => {
      if (e instanceof ErrorPublico && e.estado === 409) {
        setF((x) => ({ ...x, hora: '' }));
        void disponibilidad.refetch();
      }
    },
  });
  const cambiar = <K extends keyof FormularioSolicitud>(k: K, v: FormularioSolicitud[K]) => {
    setF((x) => ({ ...x, [k]: v, ...(k === 'servicioId' || k === 'fecha' ? { hora: '' } : {}) }));
    setErr((e) => ({ ...e, [k]: undefined }));
  };
  const texto = (k: 'nombre' | 'apellido' | 'telefono' | 'correo') => ({
    value: f[k],
    error: err[k],
    onChange: (e: { target: { value: string } }) => cambiar(k, e.target.value),
  });

  if (enviar.isSuccess) {
    const servicio = servicios.data?.find((s) => String(s.id) === f.servicioId)?.nombre;
    return (
      <Marco>
        <section className={estilos.confirmacion} aria-labelledby="t-recibida">
          <span className={estilos.iconoExito}>
            <Icono nombre="confirmar" tamano={28} />
          </span>
          <h1 id="t-recibida" tabIndex={-1} ref={(el) => el?.focus()}>
            Recibimos tu solicitud
          </h1>
          <p>La clínica te contactará para confirmar.</p>
          <p className={estilos.resumen}>
            {servicio} · {fechaLarga(f.fecha)}, {f.hora.replace(/^0/, '')}
          </p>
          <p className={estilos.secundario}>
            Apartamos ese horario mientras recepción revisa tu solicitud. Si necesitas cambiarla, llámanos al {CLINICA.telefono}.
          </p>
          <Link to="/" className={estilos.enlace}>
            Volver al inicio
          </Link>
        </section>
      </Marco>
    );
  }

  return (
    <Marco>
      <section className={estilos.formularioSeccion} aria-labelledby="t-solicitar">
        <h1 id="t-solicitar">Solicitar cita</h1>
        <p className={estilos.secundario}>
          Elige servicio, fecha y horario. La clínica asigna a quien te atiende y te contacta para confirmar.
        </p>
        <form
          className={estilos.formulario}
          noValidate
          onSubmit={(e) => {
            e.preventDefault();
            const x = errores(f);
            setErr(x);
            if (Object.keys(x).length === 0) enviar.mutate();
          }}
        >
          <fieldset className={estilos.grupo}>
            <legend>Tu cita</legend>
            <CampoSeleccion etiqueta="Servicio" value={f.servicioId} error={err.servicioId} onChange={(e) => cambiar('servicioId', e.target.value)}>
              <option value="">Elige un servicio</option>
              {servicios.data?.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.nombre}
                </option>
              ))}
            </CampoSeleccion>
            <div className={estilos.dos}>
              <CampoTexto
                etiqueta="Fecha"
                type="date"
                min={hoy}
                max={sumarDias(hoy, 13)}
                value={f.fecha}
                error={err.fecha}
                onChange={(e) => cambiar('fecha', e.target.value)}
              />
              <CampoSeleccion
                etiqueta="Horario libre"
                value={f.hora}
                error={err.hora}
                disabled={!f.servicioId || !f.fecha || disponibilidad.isFetching || horas.length === 0}
                onChange={(e) => cambiar('hora', e.target.value)}
              >
                <option value="">
                  {!f.servicioId || !f.fecha
                    ? 'Primero elige servicio y fecha'
                    : disponibilidad.isFetching
                      ? 'Buscando horarios…'
                      : horas.length === 0
                        ? 'Sin horarios libres ese día'
                        : 'Elige un horario'}
                </option>
                {horas.map((h) => (
                  <option key={h} value={h}>
                    {h.replace(/^0/, '')}
                  </option>
                ))}
              </CampoSeleccion>
            </div>
            {disponibilidad.isError && <Aviso tono="error">{disponibilidad.error.message}</Aviso>}
            {f.servicioId && f.fecha && !disponibilidad.isFetching && disponibilidad.isSuccess && horas.length === 0 && (
              <p className={estilos.secundario}>Prueba con otra fecha. Las solicitudes en línea se piden con al menos 3 horas de anticipación.</p>
            )}
          </fieldset>

          <fieldset className={estilos.grupo}>
            <legend>Tus datos</legend>
            <div className={estilos.dos}>
              <CampoTexto etiqueta="Nombre" autoComplete="given-name" {...texto('nombre')} />
              <CampoTexto etiqueta="Apellido" autoComplete="family-name" {...texto('apellido')} />
              <CampoTexto etiqueta="Teléfono" type="tel" inputMode="tel" autoComplete="tel" {...texto('telefono')} />
              <CampoTexto etiqueta="Correo" type="email" autoComplete="email" {...texto('correo')} />
            </div>
            <CampoAreaTexto
              etiqueta="Motivo de la consulta (opcional)"
              ayuda="Cuéntanos brevemente qué necesitas. No escribas información médica detallada."
              maxLength={500}
              value={f.motivo}
              error={err.motivo}
              onChange={(e) => cambiar('motivo', e.target.value)}
            />
            {/* Campo trampa: oculto para las personas y los lectores de pantalla */}
            <div className={estilos.trampa} aria-hidden="true">
              <label>
                Sitio web
                <input type="text" tabIndex={-1} autoComplete="off" value={f.sitioWeb} onChange={(e) => cambiar('sitioWeb', e.target.value)} />
              </label>
            </div>
            <details className={estilos.aviso}>
              <summary>Aviso de privacidad (texto de ejemplo)</summary>
              <p>
                Texto de ejemplo, pendiente de revisión legal. La clínica usa tu nombre, teléfono y correo solo para gestionar tu
                cita y contactarte. No compartimos tus datos con terceros. Puedes pedir que los corrijamos o eliminemos llamando a
                la clínica.
              </p>
            </details>
            <label className={estilos.casilla}>
              <input
                type="checkbox"
                checked={f.aceptaAviso}
                aria-invalid={err.aceptaAviso ? true : undefined}
                aria-describedby={err.aceptaAviso ? 'error-aviso' : undefined}
                onChange={(e) => cambiar('aceptaAviso', e.target.checked)}
              />
              <span>Leí y acepto el aviso de privacidad</span>
            </label>
            {err.aceptaAviso && (
              <p id="error-aviso" className={estilos.error}>
                {err.aceptaAviso}
              </p>
            )}
          </fieldset>

          {enviar.isError && (
            <Aviso tono="error" anunciar>
              {enviar.error.message}
            </Aviso>
          )}
          <Boton type="submit" variante="principal" grande anchoCompleto cargando={enviar.isPending} textoCargando="Enviando…">
            Enviar solicitud
          </Boton>
        </form>
      </section>
    </Marco>
  );
}

function NoEncontrada() {
  return (
    <Marco>
      <section className={estilos.confirmacion}>
        <h1>No encontramos esta página</h1>
        <Link to="/" className={estilos.enlace}>
          Ir al inicio
        </Link>
      </section>
    </Marco>
  );
}
