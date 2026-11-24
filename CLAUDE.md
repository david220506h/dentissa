# Dentissa — Contexto del proyecto para Claude Code

Este archivo se coloca en la raíz del repositorio (`Dentissa/CLAUDE.md`). Léelo completo antes de cada tarea.

## 1. Qué es el proyecto

Sistema integral para una clínica dental de **una sola sucursal**: panel administrativo web (instalable como PWA en el celular del personal) y sitio web comercial con portal del paciente, conectados a un mismo backend y una misma base de datos.

Orden de trabajo, de manera estricta:

1. **Panel administrativo + backend** (React + Vite, ASP.NET Core). Estamos aquí.
2. Sitio web comercial (Astro) y portal del paciente.
3. ~~App móvil (React Native + Expo)~~: **descartada.** Solo habrá **PWA instalable del panel**; no habrá app nativa ni Expo (decisión 2026-11-23).

No avances a una etapa posterior sin que se indique expresamente.

## 2. Estado actual del código (resultado de la auditoría)

- La **Fase 0 (seguridad base)** y la **Fase 1 (base de datos, roles y bitácora)** están completadas (informe en `docs/fase-1-informe.md`).
- `backend/`: ASP.NET Core en **.NET 10** (SDK fijado en `global.json`), **EF Core 10 con Npgsql** sobre **PostgreSQL 17** (Docker; en local, puerto **5433**). Pruebas de integración contra PostgreSQL real con **Testcontainers** (requieren Docker).
- La migración `Inicial` queda **cerrada** al integrarse la Fase 1 a `main`: todo cambio de esquema posterior es una **migración nueva**.
- **Fase 2 (agenda): en diseño** (`docs/agenda/diseno-agenda.md`). **Panel nuevo: Fase 3.**
- `frontend/`: React + Vite + TypeScript, con `@dentissa/tokens` y `@dentissa/ui` en `packages/` (Fase 3a, rama `fase-3a-panel-base`). El panel nuevo es solo para el personal: hoy conecta a la API el inicio y el cierre de sesión (con revocación del token de renovación) y el cierre por inactividad; las secciones sin pantalla dicen "Próximamente". **Hasta que existan Agenda y Equipo, citas y usuarios solo se manejan por la API.** La landing, el inicio de sesión de pacientes y el registro anteriores están en `frontend/src/migrar-a-sitio/`, para el sitio en Astro.
- **El panel se rediseña por completo; solo se conservan los colores actuales.** El diseño (sistema visual, componentes y pantallas) lo entrega el arquitecto y se guardará en `docs/design/`. No rediseñes ni reorganices la interfaz por tu cuenta; los únicos cambios de frontend permitidos hasta recibir el diseño son los que pida cada fase de manera expresa.
- Autenticación: JWT con refresh token y BCrypt. Se conserva.
- Patrón repositorio y servicios en `backend/`. Se conserva.
- Roles (ya renombrados en la Fase 1): `admin`, `asistente`, `dentista`, `paciente`. Un rol por cuenta. Los nombres viven centralizados en `backend/Constants/Roles.cs` y `frontend/src/constants/roles.js`.

## 3. Decisiones de negocio vigentes

- Horario: lunes a sábado, 8:00 a 16:00, descansos de 11:00 a 12:00 y de 14:00 a 15:00. Seis horarios al día. Debe ser **configurable**, no escrito en el código.
- Cita estándar de 60 minutos; un servicio más largo ocupa 2 horarios seguidos. Tratamientos de varias sesiones: recepción programa cada sesión.
- Capacidad: una cita por dentista activo y por horario. **No hay entidad de consultorio** (confirmado).
- La tabla `Comentarios` se **elimina** (confirmado); el sitio usará reseñas de Google.
- La clínica asigna dentista; el paciente solo elige servicio, fecha y hora.
- Estados de cita: Pendiente, Confirmada, Completada, Cancelada, No asistió.
- Una solicitud pendiente **bloquea** el horario hasta resolverse; vence según la regla en horas de atención aprobada en `docs/agenda/diseno-agenda.md`, sección 2 (vida máxima 12 h, liberación 2 h antes, anticipación en línea 3 h).
- Confirmación: correo automático con enlace; si no hay respuesta, recepción llama y confirma. El paciente solo **solicita** reagendar o cancelar; recepción aprueba.
- Notificaciones al paciente por correo. Los comentarios en citas se eliminan; el sitio usará reseñas de Google.
- Expediente clínico completo (odontograma, notas por consulta, fotos, radiografías, consentimiento, plan de tratamiento, recetas), además de inventario y pagos. Se construirán por fases.
- Todos los usuarios inician sesión: paciente, asistente, dentista, administrador.

## 4. Reglas de trabajo (obligatorias)

1. **Lee antes de escribir.** No inventes información sobre código que no hayas revisado.
2. **Plan primero.** Antes de modificar archivos, presenta qué vas a cambiar y por qué, y espera mi aprobación.
3. **No elimines funcionalidad existente** sin autorización expresa.
4. **Cambios pequeños.** Trabaja en una rama por fase (`fase-N-nombre`) y haz commits pequeños y descriptivos.
5. **Secretos:** nunca los escribas en el código, en commits, en logs ni en tus respuestas. Usa variables de entorno o user-secrets.
6. **Validación doble:** en frontend y en backend; el backend es la autoridad.
7. **Sin lógica de negocio duplicada** entre capas.
8. **Antes de dar algo por terminado:** compila el backend, ejecuta las pruebas, compila el frontend y reporta el resultado real, incluidos los fallos.
9. **No cambies el proveedor de base de datos ni el esquema** fuera de la fase que lo indique.
10. Al terminar cada fase entrega un informe con: qué cambió, qué probaste, qué **requiere acción manual mía** y qué riesgos quedan.

## 5. Seguridad: este proyecto maneja datos de salud

- Autorización por rol y por recurso en el backend, no solo en pantallas.
- Nunca devolver entidades de EF con datos sensibles (hashes, tokens): siempre DTOs.
- Registro de auditoría para acciones críticas.
- Los eliminados son lógicos; nada clínico ni financiero se borra físicamente.
- **No hay borrado físico de datos clínicos ni de citas.** Las citas se cancelan con motivo; los usuarios se desactivan. (En la base lo impiden triggers en las tablas del expediente, `auditoria`, `cita_historial` y `citas`.)
- **El administrador inicial y las contraseñas** vienen de user-secrets (`Seed:AdminEmail`, `Seed:AdminPassword`) o de variables de entorno, y **nunca se imprimen** en consola, logs ni respuestas.
- **El token de acceso dura 15 minutos** (`JwtSettings:ExpirationMinutes`, configurable).
- **Regla del último administrador:** no se puede desactivar, eliminar ni cambiar de rol al último administrador activo (lo valida el backend).
- **BLOQUEANTE del primer despliegue** (no de la Fase 3a): hoy el frontend guarda los tokens en `localStorage`. Antes de desplegar, el refresh token pasa a una cookie `HttpOnly` y el access token vive solo en memoria. Requiere cambios de backend; se hace en la fase que lo indique.
- **Requisitos del primer despliegue real:** la lista completa (cuenta de administración con contraseña nueva, tokens en cookie, hosting, correo y dominio, revisión legal) está en `docs/requisitos-despliegue.md`. No se despliega con ninguno pendiente.
- **Cierre por inactividad:** 15 minutos para todos los roles, con aviso a los 14 y opción de "Mantener sesión". En la Fase 3a solo el frontend, con el tiempo fijo; que la administración lo configure y que el servidor también expire la sesión por inactividad va en la fase de Configuración.
- **La bitácora enmascara las columnas sensibles** (`[DatoSensible]`): se registran como `"[cambió]"`, nunca con su valor. Hoy: `password_hash`, `refresh_token`, `confirmacion_token_hash`, `alergias`, `condiciones_medicas`, `curp`, `direccion` y los motivos de las citas (`motivo`, `motivo_cancelacion`, `motivo_cambio`, `solicitud_cambio_motivo`).
- Legislación mexicana aplicable: protección de datos personales en posesión de particulares y NOM-004-SSA3-2012 (expediente clínico). Se valida con un especialista; no la interpretes por tu cuenta.

## 6. Convenciones

- Idioma de la interfaz y de los mensajes al usuario: español de México.
- **Género neutro en todo texto del panel y del sitio** ("la persona", "la cuenta", "Administración"). Lo que el diseño escriba con género se adapta y se reporta al arquitecto.
- Decisiones de bajo riesgo tomadas durante el trabajo: `docs/decisiones.md`.
- Nombres de código existentes se respetan; los nuevos siguen la convención ya usada en cada capa.
- Documentación del proyecto en `docs/` (requisitos en `docs/requirements/`).