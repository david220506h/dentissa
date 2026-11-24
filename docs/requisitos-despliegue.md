# Requisitos del primer despliegue real

Nada de esto bloquea el desarrollo; **todo bloquea el primer despliegue con datos reales**. Cada punto se marca con fecha y quién lo cerró.

| # | Requisito | Detalle | Responsable | Estado |
|---|---|---|---|---|
| 1 | **Cuenta de administración nueva** | Crear la cuenta inicial con una contraseña nueva y única (user-secrets o variables de entorno: `Seed:AdminEmail`, `Seed:AdminPassword`), nunca la de desarrollo. Cambiarla tras el primer acceso. | Dueño del proyecto | Pendiente |
| 2 | **Tokens fuera de `localStorage`** | Refresh token en cookie `HttpOnly`, `Secure`, `SameSite=Strict`; token de acceso solo en memoria. Requiere cambios de backend (emisión y renovación por cookie, protección CSRF) y del `authService` del panel. | Desarrollo (fase por definir) | Pendiente |
| 3 | **Hosting** | Elegir dónde corren API, base de datos (PostgreSQL 17 gestionado o propio) y panel; respaldos cifrados y prueba de restauración; HTTPS obligatorio. | Dueño del proyecto | Pendiente |
| 4 | **Correo y dominio** | Dominio propio, proveedor SMTP, SPF, DKIM y DMARC (necesarios para confirmaciones de cita y enlaces de la Fase 2). | Dueño del proyecto | Pendiente |
| 5 | **Revisión legal** | Con especialista: LFPDPPP (aviso de privacidad, finalidades, derechos ARCO, transferencias), NOM-004-SSA3-2012 (expediente clínico) y NOM-024-SSA3-2012 (sistemas de información en salud), CAPTCHA del formulario público (Fase 2, commit 11), y plazos de **retención** (expediente, bitácora de lecturas, citas). Incluye las decisiones provisionales de `docs/pacientes/propuesta-api-pacientes.md`, **aprobada solo para la demo con datos ficticios (2026-11-24): la Fase 2b no se usa con datos reales sin esta revisión**. | Especialista + dueño | Pendiente |
| 6 | **Aviso de privacidad publicado** | Texto aprobado por el especialista, enlazado desde el sitio, el portal y los correos. | Especialista + dueño | Pendiente |
| 7 | **Servidor por inactividad** | Que el servidor también expire la sesión por inactividad (hoy solo el panel, 15 min). Fase de Configuración. | Desarrollo | Pendiente |
| 8 | **Dependencias** | `npm audit` y `dotnet list package --vulnerable` sin altas ni críticas; versiones con al menos dos semanas de publicadas. | Desarrollo | Pendiente |
| 9 | **Correo de vencimiento antes de las solicitudes en línea** | El sitio (y el portal) **no aceptan solicitudes en línea** hasta que exista el aviso por correo al paciente cuando su solicitud vence sin confirmarse (decisión 14 del diseño de la agenda). Desde el commit 10 de la Fase 2, la reserva que cancela una vencida encola el correo `vencimiento` (`ReservasService.LiberarVencidasAsync`); falta el barrido (commit 15) y un proveedor de correo real (punto 4). Comprobarlo con una prueba de punta a punta antes de activar `POST /api/publico/citas`. | Desarrollo | Pendiente |

Relacionado: `CLAUDE.md` (sección 5, seguridad) y `docs/frontend/plan-fase-3a.md` (sección 14.6, tareas con fecha).
