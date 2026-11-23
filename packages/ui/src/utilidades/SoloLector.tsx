import type { ReactNode } from 'react';
import estilos from './SoloLector.module.css';

/** Texto que solo leen los lectores de pantalla (no se ve). */
export function SoloLector({ children }: { children: ReactNode }) {
  return <span className={estilos.soloLector}>{children}</span>;
}
