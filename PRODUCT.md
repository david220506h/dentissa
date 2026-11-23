# Product

<!-- impeccable:product-schema 1 -->

<!-- Verdad del producto para las herramientas de diseño (Impeccable). No contiene decisiones visuales:
     el diseño aprobado vive en docs/design/panel/ y su implementación en packages/tokens. -->

## Platform

web

El panel administrativo es una aplicación web instalable como PWA (computadora y celular). Los pacientes no lo usan: tienen el portal del sitio web, que es otro producto con su propio contexto.

## Users

El personal de una clínica dental de **una sola sucursal** en México:

- **Administración** (una persona): ve toda la clínica, gestiona el equipo, los servicios, la configuración y los reportes; consulta el expediente sin editarlo.
- **Asistente de recepción** (una persona): atiende solicitudes de cita, confirma por llamada o WhatsApp, agenda y reagenda, registra pacientes y pagos. Ve datos generales y alergias, nunca el expediente.
- **Dentista** (una persona por ahora): ve su agenda y sus pacientes, edita el expediente clínico.

Lo usan **todo el día**, en la computadora del mostrador y en el celular. Prima la rapidez y la claridad sobre el efecto visual.

## Product Purpose

Que cada persona sepa al abrir el panel **qué hay que hacer hoy**: solicitudes por revisar, citas por confirmar y la agenda del día. Además, que la clínica administre citas, pacientes, expediente clínico, inventario y pagos en un solo lugar. Los módulos se construyen por fases; los que aún no tienen backend se muestran como "Próximamente".

## Operating Context

- **Citas:**
  - Solicitudes en línea (sitio y portal) y citas creadas por recepción.
  - Horario configurable (hoy, lunes a sábado de 8:00 a 16:00, con descansos), citas de 60 minutos y servicios de uno o dos horarios seguidos.
  - La clínica asigna el dentista.
- **Confirmación:** correo automático; si no hay respuesta, recepción llama y confirma. El paciente solo solicita reagendar o cancelar; recepción aprueba.
- **Estados de la cita:** Pendiente, Confirmada, Completada, Cancelada y No asistió.

## Capabilities and Constraints

- **Datos de salud:** Ley Federal de Protección de Datos Personales en Posesión de los Particulares y NOM-004-SSA3-2012 (a validar con un especialista).
- **Permisos por rol:** el backend es la autoridad; la interfaz solo oculta lo que no corresponde.
- **Borrado:** los datos clínicos y financieros nunca se borran físicamente (borrado lógico, cancelación con motivo, desactivación de cuentas). Otros datos, como los cierres de agenda, sí se pueden quitar.
- **Sin datos de pacientes en el dispositivo:** la PWA solo guarda archivos de la interfaz; sin conexión muestra "Sin conexión con la clínica".
- **Idioma:** español de México en toda la interfaz y los mensajes.
- **Controles sin backend:** no se muestran hasta que existan. Nunca se muestran cifras inventadas.

## Brand Commitments

- Nombre: **Dentissa**. Se conserva la paleta de la clínica.
- El logo actual es provisional: falta el archivo original con fondo transparente.

## Evidence on Hand

- **Diseño aprobado del panel:** `docs/design/panel/*.dc.html` (`Main` = sistema visual y matriz de permisos; `Movil` = celular).
- **Requerimientos:** `docs/requirements/requerimientos.md`. **Modelo de datos:** `docs/database/modelo-datos.md`. **Agenda:** `docs/agenda/diseno-agenda.md`.
- **Los datos de las pantallas del diseño son de ejemplo:** no existen testimonios, cifras ni pacientes reales que se puedan usar.

## Product Principles

1. Cada pantalla responde qué hay que hacer hoy.
2. Una acción principal por pantalla.
3. El estado nunca depende solo del color.
4. Los datos sensibles se muestran solo a quien corresponde.
5. Rapidez y claridad antes que efecto: es una herramienta de uso diario.

## Accessibility & Inclusion

- WCAG 2.2 nivel AA.
- Todo lo que se toca mide al menos 44 px.
- Texto de 12.5 px como mínimo.
- Foco visible.
- Se respeta `prefers-reduced-motion`.
