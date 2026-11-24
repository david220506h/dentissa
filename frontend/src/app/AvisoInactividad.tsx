import { Boton, Dialogo } from '@dentissa/ui';
import { useNavigate } from 'react-router-dom';
import { useInactividad } from './inactividad';
import { useSesion } from './sesion';

/**
 * "¿Sigues ahí?" (Estados.dc.html · Cierre por inactividad). "Cerrar", Esc o "Mantener sesión" mantienen
 * la sesión. Al abrir, el foco va al diálogo y se anuncia el título con la cuenta regresiva.
 */
export function AvisoInactividad() {
  const { usuario, cerrar } = useSesion();
  const navegar = useNavigate();
  const salir = async (porInactividad: boolean) => {
    await cerrar(porInactividad ? { motivo: 'inactividad' } : undefined);
    navegar('/iniciar-sesion', { replace: true });
  };
  const { avisando, segundosRestantes, mantener } = useInactividad(!!usuario, () => void salir(true));

  return (
    <Dialogo
      abierto={avisando}
      alCerrar={mantener}
      titulo="¿Sigues ahí?"
      icono="reloj"
      tamano="chico"
      acciones={
        <>
          <Boton variante="secundario" icono="cerrarSesion" onClick={() => void salir(false)}>
            Cerrar sesión
          </Boton>
          <Boton variante="principal" icono="confirmar" onClick={mantener}>
            Mantener sesión
          </Boton>
        </>
      }
    >
      <p>
        Por seguridad, cerraremos tu sesión en <b>{segundosRestantes === 1 ? '1 segundo' : `${segundosRestantes} segundos`}</b>. No
        se pierde nada de lo guardado.
      </p>
    </Dialogo>
  );
}
