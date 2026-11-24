import { Avatar } from '../Avatar/Avatar';
import { Icono } from '../Icono/Icono';
import { Insignia } from '../Insignia/Insignia';
import type { ComponenteEnlace } from '../Navegacion/Navegacion';
import estilos from './Paciente.module.css';

/**
 * Alergia (LEEME del diseño; Pacientes, Expediente y Movil2): chip sólido Rojo 800 con texto blanco e
 * ícono de alerta. Es un componente distinto de la insignia de estado: "Cancelada" sigue en tono suave.
 * El texto siempre dice "Alergia:", así que no depende del color.
 */
export function ChipAlergia({ alergia }: { alergia: string }) {
  return (
    <span className={estilos.alergia}>
      <Icono nombre="alerta" tamano={14} trazo={2.2} />
      {`Alergia: ${alergia}`}
    </span>
  );
}

export type CuentaPaciente = 'con-cuenta' | 'invitado';

export interface TarjetaPacienteProps {
  nombre: string;
  /** Sin teléfono se muestra "—". */
  telefono?: string | null;
  cuenta: CuentaPaciente;
  /** Alergias registradas. Si existen, el chip aparece siempre, dentro de la tarjeta y sin desplazar. */
  alergia?: string | null;
  /** Línea final: "Próxima: 14 dic", "Solicitada: 19 nov". */
  detalle?: string;
  href: string;
  enlace?: ComponenteEnlace;
}

const EnlaceNativo: ComponenteEnlace = ({ href, children, ...resto }) => (
  <a href={href} {...resto}>
    {children}
  </a>
);

/** Paciente en tarjeta (Movil2 · Pacientes en tarjetas): la lista del celular, con la alergia siempre visible. */
export function TarjetaPaciente({
  nombre,
  telefono,
  cuenta,
  alergia,
  detalle,
  href,
  enlace: Enlace = EnlaceNativo,
}: TarjetaPacienteProps) {
  return (
    <Enlace href={href} className={estilos.tarjeta}>
      <span className={estilos.fila}>
        <Avatar nombre={nombre} />
        <span className={estilos.datos}>
          <span className={estilos.nombre}>{nombre}</span>
          <span className={estilos.telefono}>{telefono || '—'}</span>
        </span>
        {cuenta === 'con-cuenta' ? <Insignia tono="azul">Con cuenta</Insignia> : <Insignia tono="gris">Invitado</Insignia>}
      </span>
      {alergia && (
        <span className={estilos.bloque}>
          <ChipAlergia alergia={alergia} />
        </span>
      )}
      {detalle && <span className={estilos.detalle}>{detalle}</span>}
    </Enlace>
  );
}
