import type { ReactElement, SVGProps } from 'react';

// Íconos de trazo del diseño aprobado (docs/design/panel/*.dc.html), copiados tal cual:
// viewBox 24×24, sin relleno, trazo redondeado. Ningún ícono es inventado.
const TRAZOS = {
  diente: (
    <path d="M12 5.6C11 4.7 9.8 4 8.4 4 5.9 4 4 5.9 4 8.4 4 10 4.5 11.1 5 12.5 5.6 14.1 5.7 16.1 6.3 17.9 6.6 18.9 7.1 19.5 7.9 19.5 9.2 19.5 9.2 17.2 9.6 15.9 9.8 15.3 10.2 14.9 10.7 14.9L13.3 14.9C13.8 14.9 14.2 15.3 14.4 15.9 14.8 17.2 14.8 19.5 16.1 19.5 16.9 19.5 17.4 18.9 17.7 17.9 18.3 16.1 18.4 14.1 19 12.5 19.5 11.1 20 10 20 8.4 20 5.9 18.1 4 15.6 4 14.2 4 13 4.7 12 5.6Z" />
  ),
  inicio: <path d="M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z" />,
  agenda: (
    <>
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <path d="M16 2v4M8 2v4M3 10h18" />
    </>
  ),
  solicitudes: (
    <>
      <path d="M22 12h-6l-2 3h-4l-2-3H2" />
      <path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z" />
    </>
  ),
  pacientes: (
    <>
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
    </>
  ),
  expediente: (
    <>
      <path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7z" />
      <path d="M14 2v4a2 2 0 0 0 2 2h4M10 9H8M16 13H8M16 17H8" />
    </>
  ),
  servicios: <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />,
  ofertas: (
    <>
      <path d="M12.586 2.586A2 2 0 0 0 11.172 2H4a2 2 0 0 0-2 2v7.172a2 2 0 0 0 .586 1.414l8.704 8.704a2.426 2.426 0 0 0 3.42 0l6.58-6.58a2.426 2.426 0 0 0 0-3.42z" />
      <circle cx="7.5" cy="7.5" r=".5" />
    </>
  ),
  inventario: (
    <>
      <path d="M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z" />
      <path d="M12 22V12M3.29 7 12 12l8.71-5" />
    </>
  ),
  pagos: (
    <>
      <path d="M19 7V4a1 1 0 0 0-1-1H5a2 2 0 0 0 0 4h15a1 1 0 0 1 1 1v4h-3a2 2 0 0 0 0 4h3a1 1 0 0 0 1-1v-2a1 1 0 0 0-1-1" />
      <path d="M3 5v14a2 2 0 0 0 2 2h15a1 1 0 0 0 1-1v-4" />
    </>
  ),
  reportes: <path d="M3 3v16a2 2 0 0 0 2 2h16M18 17V9M13 17V5M8 17v-3" />,
  equipo: (
    <>
      <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
      <circle cx="12" cy="7" r="4" />
    </>
  ),
  configuracion: <path d="M4 21v-7M4 10V3M12 21v-9M12 8V3M20 21v-5M20 12V3M1 14h6M9 8h6M17 16h6" />,
  auditoria: (
    <>
      <path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  cerrarSesion: <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9" />,
  buscar: (
    <>
      <circle cx="11" cy="11" r="8" />
      <path d="m21 21-4.3-4.3" />
    </>
  ),
  notificaciones: (
    <path d="M10.268 21a2 2 0 0 0 3.464 0M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326" />
  ),
  agregar: <path d="M5 12h14M12 5v14" />,
  anterior: <path d="m15 18-6-6 6-6" />,
  siguiente: <path d="m9 18 6-6-6-6" />,
  confirmar: <path d="M20 6 9 17l-5-5" />,
  quitar: <path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />,
  alerta: (
    <>
      <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3z" />
      <path d="M12 9v4M12 17h.01" />
    </>
  ),
  informacion: (
    <>
      <circle cx="12" cy="12" r="10" />
      <path d="M12 16v-4M12 8h.01" />
    </>
  ),
  telefono: (
    <path d="M13.832 16.568a1 1 0 0 0 1.213-.303l.355-.465A2 2 0 0 1 17 15h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2A18 18 0 0 1 2 4a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v3a2 2 0 0 1-.8 1.6l-.468.351a1 1 0 0 0-.292 1.233 14 14 0 0 0 6.392 6.384" />
  ),
  cerrar: <path d="M18 6 6 18M6 6l12 12" />,
  reloj: (
    <>
      <circle cx="12" cy="12" r="10" />
      <path d="M12 6v6l4 2" />
    </>
  ),
  // Flecha circular para "Reintentar" (decisión del arquitecto; Estados.dc.html aún dibuja un reloj)
  reintentar: (
    <>
      <path d="M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8" />
      <path d="M21 3v5h-5" />
    </>
  ),
  ver: (
    <>
      <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z" />
      <circle cx="12" cy="12" r="3" />
    </>
  ),
  descargar: <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3" />,
  menu: <path d="M4 6h16M4 12h16M4 18h16" />,
  mensaje: <path d="M7.9 20A9 9 0 1 0 4 16.1L2 22Z" />,
  sinConexion: (
    <>
      <path d="M5 12.55a11 11 0 0 1 5.17-2.39M19 12.55a11 11 0 0 0-2.56-1.9M1.42 9a16 16 0 0 1 4.7-2.88M22.58 9a16 16 0 0 0-10.2-3.9" />
      <path d="M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01M2 2l20 20" />
    </>
  ),
  editar: (
    <>
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z" />
    </>
  ),
} satisfies Record<string, ReactElement>;

export type NombreIcono = keyof typeof TRAZOS;
export const NOMBRES_ICONOS = Object.keys(TRAZOS) as NombreIcono[];

export interface IconoProps extends Omit<SVGProps<SVGSVGElement>, 'name'> {
  nombre: NombreIcono;
  /** Lado en px (el diseño usa 14, 18, 20, 22, 24 y 34). */
  tamano?: number;
  /** Grosor del trazo (el diseño usa 1.8; 2 en los íconos de 14 px). */
  trazo?: number;
  /** Texto para lectores de pantalla. Sin él, el ícono es decorativo (aria-hidden). */
  etiqueta?: string;
}

export function Icono({ nombre, tamano = 20, trazo = 1.8, etiqueta, ...resto }: IconoProps) {
  return (
    <svg
      width={tamano}
      height={tamano}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={trazo}
      strokeLinecap="round"
      strokeLinejoin="round"
      focusable="false"
      {...(etiqueta ? { role: 'img', 'aria-label': etiqueta } : { 'aria-hidden': true })}
      {...resto}
    >
      {TRAZOS[nombre]}
    </svg>
  );
}
