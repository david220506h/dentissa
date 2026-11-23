import {
  useId,
  useState,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from 'react';
import { Icono } from '../Icono/Icono';
import { clases } from '../utilidades/clases';
import estilos from './Campo.module.css';

interface PropsDeCampo {
  /** Etiqueta visible (14 px · 700). Obligatoria: todo campo tiene nombre visible. */
  etiqueta: string;
  /** Texto de ayuda bajo el campo. */
  ayuda?: string;
  /** Mensaje de error ("Escribe un correo válido."). Marca el campo como inválido. */
  error?: string;
  /** 48 px de alto y letra de 16 px (inicio de sesión). */
  grande?: boolean;
  className?: string;
}

/** Ids y atributos de accesibilidad compartidos por todos los campos. */
function useCampo(idPropio: string | undefined, ayuda?: string, error?: string) {
  const idGenerado = useId();
  const id = idPropio ?? idGenerado;
  const idAyuda = ayuda ? `${id}-ayuda` : undefined;
  const idError = error ? `${id}-error` : undefined;
  const describedBy = [idAyuda, idError].filter(Boolean).join(' ') || undefined;
  return {
    id,
    idAyuda,
    idError,
    atributos: { id, 'aria-invalid': error ? (true as const) : undefined, 'aria-describedby': describedBy },
  };
}

function Envoltura({
  id,
  etiqueta,
  ayuda,
  idAyuda,
  error,
  idError,
  className,
  children,
}: {
  id: string;
  etiqueta: string;
  ayuda?: string;
  idAyuda?: string;
  error?: string;
  idError?: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div className={clases(estilos.campo, className)}>
      <label htmlFor={id} className={estilos.etiqueta}>
        {etiqueta}
      </label>
      {children}
      {ayuda && (
        <div id={idAyuda} className={estilos.ayuda}>
          {ayuda}
        </div>
      )}
      {error && (
        <div id={idError} className={estilos.error}>
          <Icono nombre="alerta" tamano={16} trazo={2} />
          {error}
        </div>
      )}
    </div>
  );
}

export type CampoTextoProps = PropsDeCampo & Omit<InputHTMLAttributes<HTMLInputElement>, 'className'>;

export function CampoTexto({ etiqueta, ayuda, error, grande, className, id: idPropio, type = 'text', ...resto }: CampoTextoProps) {
  const { id, idAyuda, idError, atributos } = useCampo(idPropio, ayuda, error);
  return (
    <Envoltura {...{ id, etiqueta, ayuda, idAyuda, error, idError, className }}>
      <input type={type} className={clases(estilos.control, grande && estilos.grande)} {...atributos} {...resto} />
    </Envoltura>
  );
}

export type CampoContrasenaProps = Omit<CampoTextoProps, 'type'>;

/** Contraseña con botón para mostrarla (Login). El botón mantiene el ícono del diseño y cambia su nombre. */
export function CampoContrasena({ etiqueta, ayuda, error, grande, className, id: idPropio, ...resto }: CampoContrasenaProps) {
  const { id, idAyuda, idError, atributos } = useCampo(idPropio, ayuda, error);
  const [visible, setVisible] = useState(false);
  return (
    <Envoltura {...{ id, etiqueta, ayuda, idAyuda, error, idError, className }}>
      <div className={estilos.conBoton}>
        <input
          type={visible ? 'text' : 'password'}
          className={clases(estilos.control, grande && estilos.grande)}
          {...atributos}
          {...resto}
        />
        <button
          type="button"
          className={estilos.botonInterno}
          aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
          aria-pressed={visible}
          aria-controls={id}
          onClick={() => setVisible((v) => !v)}
        >
          <Icono nombre="ver" />
        </button>
      </div>
    </Envoltura>
  );
}

export type CampoSeleccionProps = PropsDeCampo &
  Omit<SelectHTMLAttributes<HTMLSelectElement>, 'className'> & { children: ReactNode };

export function CampoSeleccion({ etiqueta, ayuda, error, grande, className, id: idPropio, children, ...resto }: CampoSeleccionProps) {
  const { id, idAyuda, idError, atributos } = useCampo(idPropio, ayuda, error);
  return (
    <Envoltura {...{ id, etiqueta, ayuda, idAyuda, error, idError, className }}>
      <select className={clases(estilos.control, estilos.seleccion, grande && estilos.grande)} {...atributos} {...resto}>
        {children}
      </select>
    </Envoltura>
  );
}

export type CampoAreaTextoProps = Omit<PropsDeCampo, 'grande'> & Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'className'>;

export function CampoAreaTexto({ etiqueta, ayuda, error, className, id: idPropio, ...resto }: CampoAreaTextoProps) {
  const { id, idAyuda, idError, atributos } = useCampo(idPropio, ayuda, error);
  return (
    <Envoltura {...{ id, etiqueta, ayuda, idAyuda, error, idError, className }}>
      <textarea className={clases(estilos.control, estilos.areaTexto)} {...atributos} {...resto} />
    </Envoltura>
  );
}

export interface BusquedaProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'className'> {
  /** Nombre accesible (el diseño no muestra etiqueta visible: "Buscar"). */
  etiqueta: string;
  className?: string;
}

/** Campo de búsqueda con ícono. Solo se usa donde exista un backend que busque (decisión 5). */
export function Busqueda({ etiqueta, className, ...resto }: BusquedaProps) {
  return (
    <div className={clases(estilos.busqueda, className)}>
      <span className={estilos.iconoBusqueda}>
        <Icono nombre="buscar" />
      </span>
      <input type="search" aria-label={etiqueta} className={estilos.control} {...resto} />
    </div>
  );
}
