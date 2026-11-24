import { clases } from '../utilidades/clases';
import { SoloLector } from '../utilidades/SoloLector';
import estilos from './Contador.module.css';

export interface ContadorProps {
  valor: number;
  /** Texto completo para lectores de pantalla, p. ej. "5 solicitudes por revisar". */
  descripcion: string;
  className?: string;
}

/** Número sobre Rosa 700. No se anima al cambiar; el texto descriptivo lo lee el lector de pantalla. */
export function Contador({ valor, descripcion, className }: ContadorProps) {
  return (
    <span className={clases(estilos.contador, className)}>
      <span aria-hidden="true">{valor}</span>
      <SoloLector>{`, ${descripcion}`}</SoloLector>
    </span>
  );
}
