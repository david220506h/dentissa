# Decisiones de bajo riesgo tomadas durante el trabajo

Registro de lo que se eligió sin preguntar por ser ambiguo y de bajo riesgo. Cada entrada: fecha, contexto, decisión y cómo revertirla. Las decisiones del dueño del proyecto van en `CLAUDE.md` y en los planes de cada fase.

| Fecha | Contexto | Decisión | Revertir |
|---|---|---|---|
| 2026-11-23 | Integración: "integra la documentación de `fase-2-agenda`" | `fase-2-agenda` ya estaba completa dentro de `main` (merge `a3107a4`, 0 commits por delante). No hubo nada que integrar; la rama queda como registro del diseño. | — |
| 2026-11-23 | Rama del backend de la Fase 2 | Se usa `fase-2-backend` (indicación del dueño) en lugar de `fase-2-agenda`, que el plan de `docs/agenda/diseno-agenda.md` (sección 11) nombraba. El contenido del plan no cambia. | — |
| 2026-11-23 | Equipo, botón "Acceso" | Sin ícono, para que la tabla quepa junto al panel a 1440 px; el correo solo se parte antes de la arroba. | Volver a poner el ícono cuando vuelva a ser "Editar". |
| 2026-11-23 | Fase 2, commit 4: aviso por servicio sin dentistas | "Una sin leer por servicio" se interpreta **por cuenta de administración**: cada admin activo recibe una notificación interna sin leer por servicio, y no se repite mientras siga sin leer. Se identifica por el asunto ("Servicio sin dentistas asignados (#id)"). El aviso se crea al calcular disponibilidad (es lo que pide el diseño, 1.3.8). | Deduplicar por servicio sin importar la cuenta. |
| 2026-11-23 | Fase 2, commit 4: tipo de notificación | Nuevo tipo `aviso_configuracion` en `ck_notificaciones_tipo` (migración `AvisoConfiguracion`); los tipos existentes no cambian. La migración de `notificaciones` del commit 10 lo conserva. | Migración nueva que lo quite. |
| 2026-11-23 | Fase 2, commit 2: configuración ya personalizada | La migración solo convierte horas a minutos; la fila sembrada (2 h, 24 h, 2 h) pasa a los valores aprobados. Si una configuración no cumpliría las restricciones nuevas, se corrige lo mínimo (liberación 120 si era 0; anticipación = liberación + 60 si no era mayor). | — |
| 2026-11-23 | Fase 2: generación de migraciones | Se generan con `dotnet ef … --configuration Release` y las pruebas corren con `--artifacts-path` aparte, porque el backend en marcha en este equipo bloquea `backend/bin/Debug`. No cambia nada del proyecto. | — |
| 2026-11-23 | Equipo, insignias "Activo"/"Inactivo" | Se conserva el texto del diseño: describen el estado de la cuenta, no a la persona. | Cambiar a "Cuenta activa"/"Cuenta inactiva" si se prefiere. |
