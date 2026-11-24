# Pendientes

Lo que quedó fuera de la versión de demostración (`v0.1-demo`, `docs/DEMO.md`). La sección 1 **bloquea el primer despliegue con datos reales**; su estado vive en `docs/requisitos-despliegue.md`. Las secciones 2 a 7 van en orden de prioridad.

## 1. Requisitos del despliegue con datos reales

1. **Tokens fuera de `localStorage`:** refresh token en cookie `HttpOnly`, `Secure`, `SameSite=Strict`, con protección CSRF, y token de acceso **solo en memoria** (backend y `authService` del panel).
2. **Cuenta de administración nueva** con contraseña nueva y única (user-secrets o variables de entorno), nunca la de desarrollo ni la de la demo. Cambiarla tras el primer acceso.
3. **Hosting:** API, PostgreSQL 17 gestionado o propio, respaldos cifrados con prueba de restauración, HTTPS obligatorio. La imagen y el compose de la demo **no** son de producción.
4. **Correo real con dominio:** proveedor SMTP, dominio propio, SPF, DKIM y DMARC (hoy solo Mailpit).
5. **Revisión legal** con especialista: LFPDPPP (finalidades, derechos ARCO, transferencias), NOM-004-SSA3-2012 (expediente clínico), NOM-024-SSA3-2012 (sistemas de información en salud), **aviso de privacidad** publicado y enlazado desde el sitio, el portal y los correos, y plazos de **retención** (expediente, bitácora de lecturas, citas). Incluye la propuesta de pacientes (`docs/pacientes/propuesta-api-pacientes.md`), aprobada **solo para la demo con datos ficticios**.
6. **Turnstile** (o CAPTCHA equivalente) en todo formulario público antes de aceptar solicitudes en línea (`IVerificadorCaptcha`, commit 11), junto con el correo de vencimiento probado de punta a punta (requisitos de despliegue, punto 9).
7. **Servidor por inactividad:** que el servidor también expire la sesión (hoy solo el panel, 15 min).
8. **Dependencias:** `npm audit` y `dotnet list package --vulnerable` sin altas ni críticas.

## 2. Portal de pacientes

Requiere la etapa 2 (sitio). Commits pendientes del plan de la agenda (`docs/agenda/diseno-agenda.md`):

| Commit | Contenido |
|---|---|
| 11 | Solicitud pública `POST /api/publico/citas`: campo trampa, Turnstile, aviso de privacidad, teléfono normalizado (migración), tope por contacto, límites de tasa. |
| 12 | Enlaces del correo: `consultar`, `confirmar` y `solicitar-cambio` (los tokens ya existen, commit 9). |
| 13 | Portal del paciente (9.3). |
| 14 | Solicitudes de cambio del paciente y su resolución por recepción (pantalla Solicitudes del panel). |
| 15 | Barrido de vencidas en segundo plano (`BarridoVencidasService`). Hoy solo se liberan al reservar el horario. |
| 19 | Sin enumeración de correos en el registro y el alta de usuarios (P9, decisión 18). **Prioridad alta en cuanto el registro de pacientes se publique.** |

## 3. Sitio web comercial

Astro, con la landing, el inicio de sesión de pacientes y el registro que esperan en `frontend/src/migrar-a-sitio/`; reseñas de Google en lugar de comentarios.

## 4. Expediente clínico

Odontograma editable (el componente presentacional ya existe en `@dentissa/ui`), notas por consulta, fotos, radiografías, consentimientos, plan de tratamiento y recetas. Solo el dentista edita; la administración lee; la asistente nunca lo ve.

## 5. Inventario

Consulta y movimientos (asistente), consumo (dentista), completo (administración), según la matriz de `Main.dc.html`.

## 6. Pagos

Registro por recepción y administración; ligado a las citas (`costo_total`). Nada financiero se borra físicamente.

## 7. Reportes

Completos para la administración y los propios para cada dentista.

## Mejoras del panel para el personal

- **Agenda del dentista** en el panel con `GET /api/mi-agenda` (hoy el dentista solo ve Inicio y Pacientes).
- **Pantallas de Configuración y Auditoría:** los endpoints de horario y cierres existen (commit 17).
- **Pacientes:** filtros "Con cuenta / Invitados / Con alergias" y búsqueda por correo, como en el diseño (requieren backend); baja lógica desde la ficha (el endpoint ya existe).
- **Bandeja interna:** agrupar los avisos "Sin correo" repetidos del mismo paciente (`docs/decisiones.md`, commit 10).
- **Retirar los endpoints de compatibilidad** (`PUT /api/citas/{id}` y `GET /api/citas/horarios-disponibles`) cuando el panel deje de usarlos, con autorización.
- **Tolerancia de "paciente presente"** (15 min) y **cierre por inactividad** configurables por la administración.
- **typescript-eslint 8.74.0**, tarea fechada para el 2026-12-07 (plan 3a, 14.6).
