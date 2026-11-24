# Pendientes

Lo que quedó fuera de la versión de demostración, ordenado por prioridad. Lo marcado **bloquea el primer despliegue con datos reales**; la lista completa y su estado están en `docs/requisitos-despliegue.md`.

## 1. Bloquean el despliegue con datos reales

1. **Revisión legal** con especialista: LFPDPPP (aviso de privacidad, finalidades, derechos ARCO, transferencias), NOM-004-SSA3-2012 (expediente clínico) y NOM-024-SSA3-2012 (sistemas de información en salud), y plazos de **retención**. Incluye la propuesta de pacientes (`docs/pacientes/propuesta-api-pacientes.md`), aprobada **solo para la demo con datos ficticios**.
2. **Aviso de privacidad** publicado y enlazado desde el sitio, el portal y los correos.
3. **Tokens fuera de `localStorage`:** refresh token en cookie `HttpOnly`, `Secure`, `SameSite=Strict`, y token de acceso solo en memoria (backend y panel).
4. **Cuenta de administración nueva** con contraseña nueva y única (user-secrets o variables de entorno), nunca la de desarrollo o la demo.
5. **Hosting:** API, PostgreSQL 17 gestionado o propio, respaldos cifrados con prueba de restauración, HTTPS obligatorio.
6. **Correo real:** proveedor SMTP, dominio, SPF, DKIM y DMARC (hoy solo Mailpit).
7. **Servidor por inactividad:** que el servidor también expire la sesión (hoy solo el panel, 15 min).
8. **Dependencias:** `npm audit` y `dotnet list package --vulnerable` sin altas ni críticas.
9. **Remoto de git:** no hay remoto configurado; ninguna rama ni etiqueta se ha subido.

## 2. Agenda: lo público (fuera de la demo)

Todo requiere el sitio (Astro), que es la etapa 2.

| Commit del plan | Contenido |
|---|---|
| 11 | Solicitud pública `POST /api/publico/citas`: campo trampa, Turnstile (`IVerificadorCaptcha`), aviso de privacidad, teléfono normalizado (migración), tope por contacto, límites de tasa. |
| 12 | Enlaces del correo: `consultar`, `confirmar` y `solicitar-cambio` (los tokens ya existen, commit 9). |
| 13 | Portal del paciente (9.3). |
| 14 | Solicitudes de cambio del paciente y su resolución por recepción. |
| 15 | Barrido de vencidas en segundo plano (`BarridoVencidasService`). Hoy solo se liberan al reservar el horario; sin solicitudes en línea no hay vencidas. |
| 19 | Sin enumeración de correos en el registro y el alta de usuarios (P9, decisión 18). **Prioridad alta en cuanto el registro de pacientes se publique.** |

Requisito previo de las solicitudes en línea: el correo de vencimiento debe estar probado de punta a punta (`docs/requisitos-despliegue.md`, punto 9).

## 3. Agenda: mejoras para el personal

- **Bandeja interna:** agrupar los avisos "Sin correo" repetidos del mismo paciente (`docs/decisiones.md`, commit 10).
- **Configuración desde el panel:** los endpoints de horario y cierres existen (commit 17), pero el panel aún no tiene pantalla de Configuración.
- **Retirar los endpoints de compatibilidad** (`PUT /api/citas/{id}` y `GET /api/citas/horarios-disponibles`) cuando el panel deje de usarlos, con autorización.
- **Tolerancia de "paciente presente"** (15 min) configurable si se pide.
- **Cierre por inactividad configurable** por la administración (fase de Configuración).
- **typescript-eslint 8.74.0**, tarea fechada para el 2026-12-07 (plan 3a, 14.6).
