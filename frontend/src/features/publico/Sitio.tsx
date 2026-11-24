import '@dentissa/tokens/sitio.css';
import { Aviso, Boton, CampoAreaTexto, CampoSeleccion, CampoTexto, Icono } from '@dentissa/ui';
import { QueryClient, QueryClientProvider, useMutation, useQuery } from '@tanstack/react-query';
import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Link, Route, Routes, useLocation } from 'react-router-dom';
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

// Horario de la clínica (el mismo que la agenda configurada en la demo)
const HORARIO = [
  { dia: 'Lunes a viernes', horas: '8:00 a 16:00', dias: [1, 2, 3, 4, 5] },
  { dia: 'Sábado', horas: '8:00 a 16:00', dias: [6] },
  { dia: 'Domingo', horas: 'Cerrado', dias: [0] },
];

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

/** Pin de ubicación de línea (mismo trazo que el set de @dentissa/ui, que no lo trae). */
function IconoUbicacion() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
      <path d="M12 21s-7-6.2-7-11.5A7 7 0 0 1 19 9.5C19 14.8 12 21 12 21Z" />
      <circle cx="12" cy="9.5" r="2.5" />
    </svg>
  );
}

/**
 * Revela las secciones al entrar en pantalla. Visibles por defecto (sin JS, con movimiento reducido o si el
 * navegador no tiene IntersectionObserver); solo las que aún están abajo se ocultan y aparecen al llegar.
 */
function useRevelar() {
  const { pathname } = useLocation();
  useEffect(() => {
    if (typeof IntersectionObserver === 'undefined' || window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return;
    const nodos = [...document.querySelectorAll<HTMLElement>('[data-revelar]')].filter(
      (n) => n.getBoundingClientRect().top > window.innerHeight * 0.9,
    );
    const io = new IntersectionObserver(
      (entradas) =>
        entradas.forEach((e) => {
          if (e.isIntersecting) {
            e.target.removeAttribute('data-oculto');
            io.unobserve(e.target);
          }
        }),
      { rootMargin: '0px 0px -10% 0px' },
    );
    nodos.forEach((n) => {
      n.setAttribute('data-oculto', '');
      io.observe(n);
    });
    return () => io.disconnect();
  }, [pathname]);
}

function Marco({ children }: { children: ReactNode }) {
  const [desplazado, setDesplazado] = useState(false);
  const [menu, setMenu] = useState(false);
  const centinela = useRef<HTMLDivElement>(null);
  useRevelar();

  // La barra gana fondo al desplazarse (sin escuchar cada evento de scroll)
  useEffect(() => {
    const el = centinela.current;
    if (!el || typeof IntersectionObserver === 'undefined') return;
    const io = new IntersectionObserver(([e]) => setDesplazado(!e!.isIntersecting));
    io.observe(el);
    return () => io.disconnect();
  }, []);

  useEffect(() => {
    if (!menu) return;
    const alTeclear = (e: KeyboardEvent) => e.key === 'Escape' && setMenu(false);
    document.addEventListener('keydown', alTeclear);
    return () => document.removeEventListener('keydown', alTeclear);
  }, [menu]);

  const cerrar = () => setMenu(false);
  return (
    <div className={estilos.raiz}>
      <a href="#contenido" className={estilos.saltar}>
        Saltar al contenido
      </a>
      <div ref={centinela} className={estilos.centinela} aria-hidden="true" />
      <p className={estilos.ejemplo} role="note">
        Sitio de demostración: la clínica y sus datos son de ejemplo.
      </p>
      <header className={estilos.barra} data-desplazado={desplazado || undefined}>
        <div className={estilos.barraInterior}>
          <Link to="/" className={estilos.marca} onClick={cerrar}>
            <span className={estilos.marcaIcono}>
              <Icono nombre="diente" tamano={22} />
            </span>
            {CLINICA.nombre}
          </Link>
          <button
            type="button"
            className={estilos.botonMenu}
            aria-expanded={menu}
            aria-controls="menu-sitio"
            onClick={() => setMenu((m) => !m)}
          >
            <Icono nombre={menu ? 'cerrar' : 'menu'} tamano={22} />
            <span>Menú</span>
          </button>
          <nav id="menu-sitio" aria-label="Sitio" className={estilos.nav} data-abierto={menu || undefined}>
            <a href="/#servicios" onClick={cerrar}>
              Servicios
            </a>
            <a href="/#como-funciona" onClick={cerrar}>
              Cómo funciona
            </a>
            <a href="/#ubicacion" onClick={cerrar}>
              Ubicación
            </a>
            <a href="/panel" className={estilos.personalNav}>
              Acceso del personal
            </a>
          </nav>
          <Link to="/solicitar-cita" className={`${estilos.pildora} ${estilos.ctaBarra}`} onClick={cerrar}>
            Solicitar cita
          </Link>
        </div>
      </header>
      <main id="contenido" className={estilos.principal}>
        {children}
      </main>
      <footer className={estilos.pie}>
        <div className={estilos.pieInterior}>
          <div className={estilos.pieMarca}>
            <span className={estilos.marca}>
              <span className={estilos.marcaIcono}>
                <Icono nombre="diente" tamano={22} />
              </span>
              {CLINICA.nombre}
            </span>
            <p>Odontología general y especialidades, con cita y sin prisas.</p>
          </div>
          <div className={estilos.pieColumna}>
            <h2>Visítanos</h2>
            <p>{CLINICA.direccion}</p>
            <p>Lunes a sábado, 8:00 a 16:00</p>
          </div>
          <div className={estilos.pieColumna}>
            <h2>Contacto</h2>
            <p>{CLINICA.telefono}</p>
            <p>{CLINICA.correo}</p>
          </div>
          <div className={estilos.pieColumna}>
            <h2>Clínica</h2>
            <a href="/panel">Acceso del personal</a>
          </div>
        </div>
        <p className={estilos.pieNota}>Demostración con datos de ejemplo: la clínica, sus datos y el aviso de privacidad no son reales.</p>
      </footer>
    </div>
  );
}

function BotonSolicitar({ texto = 'Solicitar cita' }: { texto?: string }) {
  return (
    <Link to="/solicitar-cita" className={`${estilos.pildora} ${estilos.cta}`}>
      {texto}
      <Icono nombre="siguiente" tamano={20} trazo={2.2} />
    </Link>
  );
}

/** Ilustración del hero: la tarjeta de una cita con horarios libres, como la verá quien la pida. */
function TarjetaHero() {
  return (
    <div className={estilos.heroVisual} aria-hidden="true">
      <div className={estilos.heroForma} />
      <div className={estilos.heroTarjeta}>
        <div className={estilos.heroTarjetaCabeza}>
          <span className={estilos.heroIcono}>
            <Icono nombre="agenda" tamano={20} />
          </span>
          <div>
            <p className={estilos.heroTarjetaTitulo}>Limpieza dental</p>
            <p className={estilos.heroTarjetaSub}>Martes 1 de diciembre</p>
          </div>
        </div>
        <p className={estilos.heroTarjetaEtiqueta}>Horarios libres</p>
        <div className={estilos.heroHoras}>
          <span>9:00</span>
          <span data-elegida="">12:00</span>
          <span>13:00</span>
          <span>15:00</span>
        </div>
        <div className={estilos.heroEstado}>
          <Icono nombre="confirmar" tamano={18} trazo={2.4} />
          Solicitud recibida
        </div>
      </div>
      <div className={estilos.heroNota}>
        <Icono nombre="telefono" tamano={18} />
        Te llamamos para confirmar
      </div>
    </div>
  );
}

function Eyebrow({ children }: { children: ReactNode }) {
  return <p className={estilos.eyebrow}>{children}</p>;
}

function Inicio() {
  const servicios = useQuery({ queryKey: ['publico', 'servicios'], queryFn: () => pedir<ServicioPublico[]>('/publico/servicios') });
  const hoy = new Date().getDay();
  return (
    <Marco>
      <section className={estilos.hero} aria-labelledby="titulo">
        <div className={`${estilos.contenedor} ${estilos.heroRejilla}`}>
          <div className={estilos.heroTexto}>
            <Eyebrow>Clínica dental en la Ciudad de México</Eyebrow>
            <h1 id="titulo">Tu sonrisa, atendida con calma y con cuidado</h1>
            <p className={estilos.entrada}>
              Odontología general, ortodoncia y endodoncia en un solo lugar. Elige el horario que te acomode en línea y la
              clínica te contacta para confirmarlo. Sin crear cuenta.
            </p>
            <div className={estilos.heroAcciones}>
              <BotonSolicitar />
              <a href="#servicios" className={`${estilos.pildora} ${estilos.secundario}`}>
                Ver servicios
              </a>
            </div>
            <ul className={estilos.confianza}>
              <li>
                <Icono nombre="reloj" tamano={18} />
                Lunes a sábado, 8:00 a 16:00
              </li>
              <li>
                <Icono nombre="telefono" tamano={18} />
                Confirmamos por teléfono
              </li>
            </ul>
          </div>
          <TarjetaHero />
        </div>
      </section>

      <section id="servicios" className={estilos.seccion} aria-labelledby="t-servicios">
        <div className={estilos.contenedor}>
          <div className={estilos.cabezaSeccion} data-revelar="">
            <Eyebrow>Servicios</Eyebrow>
            <h2 id="t-servicios">Lo que hacemos por tu boca</h2>
            <p className={estilos.lead}>
              Precios de referencia; el costo final depende de tu valoración. La clínica asigna a quien te atiende según el
              tratamiento.
            </p>
          </div>
          {servicios.isPending && <p className={estilos.nota}>Cargando servicios…</p>}
          {servicios.isError && <p className={estilos.nota}>No pudimos cargar los servicios. Llámanos al {CLINICA.telefono}.</p>}
          {servicios.data && (
            <ul className={estilos.carta} data-revelar="">
              {servicios.data.map((s) => (
                <li key={s.id} className={estilos.cartaFila}>
                  <div>
                    <h3>{s.nombre}</h3>
                    <p>{s.duracionSlots > 1 ? `Unas ${s.duracionSlots} horas · puede requerir varias sesiones` : 'Alrededor de 1 hora'}</p>
                  </div>
                  <p className={estilos.precio}>
                    <span>desde</span> {precio(s.precio)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <section id="como-funciona" className={`${estilos.seccion} ${estilos.seccionTinte}`} aria-labelledby="t-como">
        <div className={estilos.contenedor}>
          <div className={estilos.cabezaSeccion} data-revelar="">
            <Eyebrow>Cómo funciona</Eyebrow>
            <h2 id="t-como">Tu cita en tres pasos</h2>
          </div>
          <ol className={estilos.pasos} data-revelar="">
            <li>
              <span className={estilos.pasoNumero}>1</span>
              <h3>Elige servicio y horario</h3>
              <p>Ves solo los horarios libres de los próximos 14 días, ya sin descansos ni días cerrados.</p>
            </li>
            <li>
              <span className={estilos.pasoNumero}>2</span>
              <h3>Envía tus datos</h3>
              <p>Nombre, teléfono y correo. Apartamos ese horario para ti mientras recepción revisa tu solicitud.</p>
            </li>
            <li>
              <span className={estilos.pasoNumero}>3</span>
              <h3>Te confirmamos</h3>
              <p>La clínica te llama o te escribe para confirmar. Si algo cambia, te ofrecemos otro horario.</p>
            </li>
          </ol>
        </div>
      </section>

      <section id="ubicacion" className={estilos.seccion} aria-labelledby="t-ubicacion">
        <div className={`${estilos.contenedor} ${estilos.ubicacion}`}>
          <div data-revelar="">
            <Eyebrow>Ubicación y horario</Eyebrow>
            <h2 id="t-ubicacion">Te esperamos en el centro</h2>
            <p className={estilos.etiquetaEjemplo}>Datos de ejemplo</p>
            <ul className={estilos.contacto}>
              <li>
                <IconoUbicacion />
                {CLINICA.direccion}
              </li>
              <li>
                <Icono nombre="telefono" tamano={20} />
                {CLINICA.telefono}
              </li>
              <li>
                <Icono nombre="mensaje" tamano={20} />
                {CLINICA.correo}
              </li>
            </ul>
          </div>
          <div className={estilos.horarioTarjeta} data-revelar="">
            <h3>Horario</h3>
            <dl className={estilos.horario}>
              {HORARIO.map((h) => (
                <div key={h.dia} data-hoy={h.dias.includes(hoy) || undefined}>
                  <dt>
                    {h.dia}
                    {h.dias.includes(hoy) && <span className={estilos.hoy}>Hoy</span>}
                  </dt>
                  <dd>{h.horas}</dd>
                </div>
              ))}
            </dl>
            <p className={estilos.nota}>Descansos de 11:00 a 12:00 y de 14:00 a 15:00.</p>
          </div>
        </div>
      </section>

      <section className={estilos.cierre} aria-labelledby="t-cierre">
        <div className={`${estilos.contenedor} ${estilos.cierreInterior}`} data-revelar="">
          <h2 id="t-cierre">Agenda tu visita: elige el horario que te acomode</h2>
          <p>Tarda un par de minutos. La clínica te confirma por teléfono.</p>
          <BotonSolicitar />
        </div>
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
  const servicioElegido = servicios.data?.find((s) => String(s.id) === f.servicioId);

  if (enviar.isSuccess) {
    const servicio = servicioElegido?.nombre;
    return (
      <Marco>
        <section className={estilos.paginaFormulario} aria-labelledby="t-recibida">
          <div className={`${estilos.contenedor} ${estilos.confirmacionMarco}`}>
            <div className={estilos.confirmacion}>
              <span className={estilos.iconoExito}>
                <Icono nombre="confirmar" tamano={32} trazo={2.4} />
              </span>
              <h1 id="t-recibida" tabIndex={-1} ref={(el) => el?.focus()}>
                Recibimos tu solicitud
              </h1>
              <p className={estilos.confirmacionTexto}>La clínica te contactará para confirmar.</p>
              <div className={estilos.boleto}>
                <p className={estilos.boletoServicio}>{servicio}</p>
                <p className={estilos.boletoFecha}>
                  {fechaLarga(f.fecha)}, {f.hora.replace(/^0/, '')}
                </p>
              </div>
              <ol className={estilos.siguientes}>
                <li>
                  <Icono nombre="reloj" tamano={20} />
                  Apartamos ese horario mientras recepción revisa tu solicitud.
                </li>
                <li>
                  <Icono nombre="telefono" tamano={20} />
                  Te llamamos o escribimos para confirmar. Si necesitas cambiarla, llámanos al {CLINICA.telefono}.
                </li>
              </ol>
              <Link to="/" className={`${estilos.pildora} ${estilos.secundario}`}>
                Volver al inicio
              </Link>
            </div>
          </div>
        </section>
      </Marco>
    );
  }

  return (
    <Marco>
      <section className={estilos.paginaFormulario} aria-labelledby="t-solicitar">
        <div className={estilos.contenedor}>
          <div className={estilos.cabezaFormulario}>
            <Eyebrow>Cita en línea · sin crear cuenta</Eyebrow>
            <h1 id="t-solicitar">Solicitar cita</h1>
            <p className={estilos.lead}>
              Elige servicio, fecha y horario. La clínica asigna a quien te atiende y te contacta para confirmar.
            </p>
          </div>
          <div className={estilos.formularioRejilla}>
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
                <legend>
                  <span className={estilos.legendNumero}>1</span>
                  Tu cita
                </legend>
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
                  <p className={estilos.nota}>Prueba con otra fecha. Las solicitudes en línea se piden con al menos 3 horas de anticipación.</p>
                )}
              </fieldset>

              <fieldset className={estilos.grupo}>
                <legend>
                  <span className={estilos.legendNumero}>2</span>
                  Tus datos
                </legend>
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
                    Texto de ejemplo, pendiente de revisión legal. La clínica usa tu nombre, teléfono y correo solo para
                    gestionar tu cita y contactarte. No compartimos tus datos con terceros. Puedes pedir que los corrijamos o
                    eliminemos llamando a la clínica.
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
              <Boton
                type="submit"
                variante="principal"
                grande
                anchoCompleto
                className={estilos.enviar}
                cargando={enviar.isPending}
                textoCargando="Enviando…"
              >
                Enviar solicitud
              </Boton>
            </form>

            <aside className={estilos.resumenLateral} aria-label="Resumen de tu solicitud">
              <h2>Tu solicitud</h2>
              <dl className={estilos.resumenLista}>
                <div>
                  <dt>Servicio</dt>
                  <dd>{servicioElegido?.nombre ?? 'Sin elegir'}</dd>
                </div>
                <div>
                  <dt>Fecha</dt>
                  <dd>{f.fecha ? fechaLarga(f.fecha) : 'Sin elegir'}</dd>
                </div>
                <div>
                  <dt>Horario</dt>
                  <dd>{f.hora ? f.hora.replace(/^0/, '') : 'Sin elegir'}</dd>
                </div>
              </dl>
              <div className={estilos.resumenNota}>
                <Icono nombre="informacion" tamano={20} />
                <p>Es una solicitud: la clínica la confirma por teléfono o correo. ¿Prefieres llamar? {CLINICA.telefono}</p>
              </div>
            </aside>
          </div>
        </div>
      </section>
    </Marco>
  );
}

function NoEncontrada() {
  return (
    <Marco>
      <section className={estilos.paginaFormulario}>
        <div className={`${estilos.contenedor} ${estilos.confirmacionMarco}`}>
          <div className={estilos.confirmacion}>
            <h1>No encontramos esta página</h1>
            <Link to="/" className={`${estilos.pildora} ${estilos.secundario}`}>
              Ir al inicio
            </Link>
          </div>
        </div>
      </section>
    </Marco>
  );
}
