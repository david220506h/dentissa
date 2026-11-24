# Informe de la Fase 2: agenda (versión para el personal)

Plan: `docs/agenda/diseno-agenda.md`, sección 11. Decisiones tomadas en el camino: `docs/decisiones.md`. Lo que falta: `docs/pendientes.md`.

## Qué cambió

| # | Commit | Estado |
|---|---|---|
| 1–6 | Citas sin borrado, configuración en horas de atención, reloj de atención y `CalcularBloqueo`, motor de disponibilidad, endpoints de disponibilidad, reservas con candado | Hecho (etiqueta `fase-2-commits-1-6`), con los ajustes aprobados: vencimiento a nombre del sistema en la bitácora y "paciente presente" |
| 7 | Transiciones de estado, historial genérico, `xmin` | Hecho |
| 8 | Asignación y reasignación de dentista | Hecho |
| 9 | Tokens de cita | Hecho (solo el servicio; los enlaces son del commit 12) |
| 10 | Bandeja de salida y envío | Hecho, **solo con Mailpit** |
| 11–15 | Solicitud pública, enlaces, portal, solicitudes de cambio, barrido | **Fuera** (lo público; ver pendientes) |
| 16 | Recordatorios | Hecho |
| 17 | Configuración y cierres | Hecho (backend) |
| 18 | Bandeja interna | Hecho |
| 19 | Sin enumeración de correos | **Fuera** (registro público) |
| — | `GET /api/citas` con filtro por fechas y dentista (plan 3a, 14.7) | Hecho |

## Qué se probó

Pruebas de integración contra PostgreSQL 17 real (Testcontainers):
- la tabla 2.5 de punta a punta;
- concurrencia de 2 y de 10 solicitudes, y de 2 dentistas con 10 solicitudes;
- reintentos con errores reales de PostgreSQL;
- tokens de un solo uso;
- la bandeja de correo con reintentos, arrendamiento y varios trabajadores;
- entrega real en un contenedor de Mailpit.

Total de esta etapa: 304 pruebas en verde.

## Acción manual

- **Mailpit en desarrollo:** `docs/correo/desarrollo.md`.
- **Proveedor de correo real:** se decide antes del despliegue, con SPF, DKIM y DMARC.

## Riesgos

- **Entrega "al menos una vez":** un paciente podría recibir dos veces el mismo correo si el proceso cae entre el envío y el registro.
- **Avisos repetidos:** los avisos internos "Sin correo" se repiten con pacientes que solo tienen teléfono.
- **Sin solicitudes en línea:** aún no se ejercita la liberación de vencidas en producción.
