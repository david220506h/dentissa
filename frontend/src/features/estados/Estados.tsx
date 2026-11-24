import { Boton, MensajeEstado, type NombreIcono } from '@dentissa/ui';
import { useNavigate } from 'react-router-dom';
import estilos from './Estados.module.css';

// Pantallas de estado según Estados.dc.html: el mensaje (ícono, título, texto y acción) dentro de una
// tarjeta, como en el diseño. El título es el h1 de la página.

function VolverAlInicio() {
  const navegar = useNavigate();
  return (
    <Boton variante="secundario" icono="inicio" onClick={() => navegar('/')}>
      Volver al inicio
    </Boton>
  );
}

/** Sin permiso (Estados · Sin permiso): para quien abre una dirección que su rol no puede ver. */
export function SinPermiso({ seccion }: { seccion?: string }) {
  return (
    <section className={estilos.tarjeta}>
      <MensajeEstado
        nivel={1}
        icono="auditoria"
        titulo="No tienes acceso a esta sección"
        texto={
          seccion
            ? `Tu rol no incluye ${seccion}. Si lo necesitas, pídeselo a la administración.`
            : 'Tu rol no incluye esta sección. Si la necesitas, pídesela a la administración.'
        }
        accion={<VolverAlInicio />}
      />
    </section>
  );
}

/** Dirección inexistente. Estados no la dibuja: usa el mismo patrón que "Sin permiso". */
export function NoEncontrado() {
  return (
    <section className={estilos.tarjeta}>
      <MensajeEstado
        nivel={1}
        icono="buscar"
        titulo="No encontramos esta página"
        texto="La dirección no existe en el panel. Revisa el enlace o vuelve al inicio."
        accion={<VolverAlInicio />}
      />
    </section>
  );
}

/** Sección sin pantalla todavía. Estados no la dibuja: patrón de "Vacío", sin acción. */
export function Proximamente({ titulo, icono }: { titulo: string; icono: NombreIcono }) {
  return (
    <section className={estilos.tarjeta}>
      <MensajeEstado
        nivel={1}
        icono={icono}
        titulo={titulo}
        texto="Próximamente. Esta sección se habilitará cuando su servicio esté listo."
      />
    </section>
  );
}
