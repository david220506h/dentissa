import { Aviso, Boton, CampoContrasena, CampoTexto, Icono } from '@dentissa/ui';
import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { CuentaSinAccesoAlPanel, useSesion } from '../../app/sesion';
import estilos from './IniciarSesion.module.css';

// Validación rápida en el navegador; la autoridad es el backend (CLAUDE.md, regla 6).
const CORREO_VALIDO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

type Errores = { correo?: string; contrasena?: string };

/**
 * Iniciar sesión (Login.dc.html). Solo para el personal: una cuenta de paciente no entra al panel.
 * "¿Olvidaste tu contraseña?" no se muestra: aún no existe su backend (decisión 5 del plan).
 */
export function IniciarSesion() {
  const { usuario, iniciar, aviso, limpiarAviso } = useSesion();
  const navegar = useNavigate();
  const ubicacion = useLocation();
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [errores, setErrores] = useState<Errores>({});
  const [errorGeneral, setErrorGeneral] = useState<{ titulo: string; texto?: string } | null>(null);
  const [enviando, setEnviando] = useState(false);

  const destino = (ubicacion.state as { desde?: string } | null)?.desde ?? '/';
  if (usuario) return <Navigate to={destino} replace />;

  const alEnviar = async (e: FormEvent) => {
    e.preventDefault();
    limpiarAviso();
    setErrorGeneral(null);
    const nuevos: Errores = {};
    if (!CORREO_VALIDO.test(correo.trim())) nuevos.correo = 'Escribe un correo válido.';
    if (!contrasena) nuevos.contrasena = 'Escribe tu contraseña.';
    setErrores(nuevos);
    if (nuevos.correo || nuevos.contrasena) {
      document.getElementById(nuevos.correo ? 'correo' : 'clave')?.focus();
      return;
    }

    setEnviando(true);
    try {
      await iniciar(correo, contrasena);
      navegar(destino, { replace: true });
    } catch (err) {
      setContrasena('');
      if (err instanceof CuentaSinAccesoAlPanel) {
        setErrorGeneral({ titulo: 'Acceso solo para el personal de la clínica.', texto: '¿Eres paciente? Entra desde el sitio web de la clínica.' });
      } else {
        // Mensaje del servicio ("Credenciales incorrectas", "Demasiados intentos…"): nunca revela si la cuenta existe
        setErrorGeneral({ titulo: err instanceof Error ? err.message : 'No se pudo iniciar sesión.' });
      }
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className={estilos.raiz}>
      {/* El formulario va primero en el documento (lectura y celular); en escritorio la presentación se dibuja a la izquierda */}
      <main className={estilos.formularioLado}>
        <form className={estilos.formulario} onSubmit={alEnviar} noValidate aria-describedby={errorGeneral || aviso ? 'aviso-sesion' : undefined}>
          <div>
            <h1 className={estilos.titulo}>Iniciar sesión</h1>
            <p className={estilos.subtitulo}>Entra con tu correo y contraseña.</p>
          </div>

          {(errorGeneral || aviso) && (
            <div id="aviso-sesion">
              {errorGeneral ? (
                <Aviso tono="error" titulo={errorGeneral.titulo} anunciar>
                  {errorGeneral.texto}
                </Aviso>
              ) : (
                <Aviso tono="advertencia" titulo={aviso!} anunciar />
              )}
            </div>
          )}

          <CampoTexto
            id="correo"
            etiqueta="Correo electrónico"
            type="email"
            autoComplete="username"
            placeholder="nombre@ejemplo.com"
            grande
            value={correo}
            onChange={(e) => setCorreo(e.target.value)}
            {...(errores.correo ? { error: errores.correo } : {})}
          />
          <CampoContrasena
            id="clave"
            etiqueta="Contraseña"
            autoComplete="current-password"
            placeholder="Tu contraseña"
            grande
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
            {...(errores.contrasena ? { error: errores.contrasena } : {})}
          />

          <Boton type="submit" variante="principal" grande anchoCompleto cargando={enviando} textoCargando="Entrando…">
            Entrar
          </Boton>
          <p className={estilos.nota}>¿Eres paciente? Entra desde el sitio web de la clínica.</p>
        </form>
      </main>

      <div className={estilos.presentacion}>
        <div className={estilos.marca}>
          <span className={estilos.marcaIcono}>
            <Icono nombre="diente" tamano={27} />
          </span>
          <span className={estilos.marcaNombre}>Dentissa</span>
        </div>
        <div className={estilos.lema}>
          <h2 className={estilos.lemaTitulo}>Citas, pacientes, expediente e inventario en un solo lugar.</h2>
          <p className={estilos.lemaTexto}>Acceso exclusivo para el personal de la clínica.</p>
        </div>
        <div className={estilos.ilustracion} aria-hidden="true">
          <Icono nombre="diente" tamano={220} trazo={1.2} />
        </div>
      </div>
    </div>
  );
}
