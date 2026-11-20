# Informe de la Fase 1: base de datos, roles y bitácora

> **Rama:** `fase-1-base-datos` (sin integrar a `main`, pendiente de revisión)
> **Fecha:** 2026-11-19
> **Diseño de referencia:** `docs/database/modelo-datos.md`

## 1. Qué cambió

| Commit | Contenido |
|---|---|
| `ef26d0b` | .NET 10 en `backend` y `backend.Tests`. |
| `f59e071` | PostgreSQL con Npgsql (proveedor), `docker-compose.yml` con `postgres:17-alpine`. |
| `6957d4c` | `global.json`: SDK 10.0.100 con `rollForward: latestFeature` (hoy usa 10.0.404 aunque también está instalado el SDK 11). |
| `53abe00` | Puerto local del contenedor configurable con `POSTGRES_PORT` (el 5432 lo ocupa un PostgreSQL instalado en Windows). |
| `55f5c13` | Modelo nuevo y migración `Inicial` (regenerada en `2d61feb`, ver sección 4). |
| `fc4df99` | Roles `admin`, `asistente`, `dentista`, `paciente`; `GET api/citas/mi-agenda`; ficha automática al crear un dentista o un paciente. |
| `2d61feb` | Borrado lógico; `PATCH …/cancelar` con motivo; `PATCH …/desactivar` y `…/activar`; login y refresh rechazan cuentas inactivas o eliminadas. |
| `881039b` | Token de acceso de 15 minutos (configurable); regla del último administrador. |
| `ef552e7` | Bitácora de auditoría (interceptor en la misma transacción), historial de citas, eventos de login y logout. |
| `55ab963` | Datos iniciales: configuración de agenda y administrador inicial desde user-secrets. |
| `eebae42` | Pruebas con PostgreSQL real (Testcontainers) y corrección de concurrencia (40P01). |
| (este) | Informe y estado final de `modelo-datos.md`. |

### Base de datos (migración `Inicial`)
- Extensiones `citext` y `btree_gist`; 20 tablas en `snake_case`.
- `CHECK` de rol, estados, orígenes, canales, sexo, tipo de sangre, costos, porcentajes, vigencia de ofertas, horarios y la solicitud de cambio «todo o nada».
- Índice único parcial del correo (`citext`, solo cuentas no eliminadas) y los índices de citas, incluido el parcial de `bloqueo_expira_en`.
- Restricción de exclusión `citas_sin_traslape_dentista` (rangos `[inicio, fin)`, solo citas pendientes o confirmadas).
- FK compuestas `(usuario_id, rol)` que amarran la ficha de dentista y la de paciente al rol de su cuenta.
- Triggers: `auditoria` y `cita_historial` de solo inserción (rechazan `UPDATE`, `DELETE` y `TRUNCATE`); `examen_medico`, `examen_clinico`, `consulta_registros` y `consulta_fotos` sin borrado físico.
- Se elimina la tabla `Comentarios`.

### Backend
- **Zona horaria:** las citas guardan `inicio` y `fin` en UTC. La única conversión con la hora local vive en `RelojClinica` (`Clinica:ZonaHoraria`, por defecto `America/Mexico_City`).
- **Agenda:** los horarios salen de `configuracion_agenda`, no del código.
- **Traslapes:** la restricción de la base es la autoridad. El backend solo traduce `23P01` a `409` y reintenta el interbloqueo `40P01`.
- **Borrado lógico:** filtro global con nombre `BorradoLogico`. Un `Remove()` se guarda como `eliminado_en = now()`. Las consultas de citas ignoran solo ese filtro y marcan `pacienteEliminado`, `dentistaEliminado` y `servicioEliminado`.
- **Bitácora:** `AuditoriaInterceptor` escribe en la misma transacción que el cambio. `[DatoSensible]` es la única lista de columnas sensibles: `password_hash`, `refresh_token`, `confirmacion_token_hash`, `alergias`, `condiciones_medicas`, `curp` y `direccion` se registran como `"[cambió]"`, nunca con su valor. Los guardados que solo tocan el control de sesión no generan fila; login correcto, login fallido y logout se registran como eventos.
- **Historial de citas:** se registra en creación, reagenda, cambio de estado, modificación, confirmación y cancelación, en el mismo `SaveChanges` que la cita.
- **Último administrador:** el `DbContext` rechaza cualquier guardado que desactive, elimine o cambie de rol al último administrador activo; por la API responde `409`.
- **Arranque:** `DatosInicialesService` aplica migraciones solo con `BaseDeDatos:AplicarMigracionesAlArrancar` (activado en Development).
  - Crea la configuración de agenda inicial: lunes a sábado de 8:00 a 16:00, descansos de 11 a 12 y de 14 a 15, citas de 60 minutos.
  - Crea el administrador inicial solo si no hay ninguno, con `Seed:AdminEmail` y `Seed:AdminPassword` (mínimo 12 caracteres). Si faltan o no son válidos, avisa sin escribir los valores.

### Frontend (ajustes mínimos, sin rediseño)
- `citas.jsx`:
  - Campos `pacienteId`/`dentistaId` y rutas `citas/pacientes` y `citas/dentistas`.
  - Se retira «Agendado por».
  - El estado «Cancelada» se sustituye por «No asistió».
  - Cancelar pide el motivo con `prompt()`.
- `usuarios.jsx`: roles nuevos; desactivar y activar en lugar de eliminar; badge «inactiva».
- `constants/roles.js`, `Dashboard.jsx` y `App.jsx`: el dentista entra al panel y solo ve Inicio.

## 2. Qué se probó (resultados reales)

| Verificación | Resultado |
|---|---|
| `dotnet build Dentissa.sln` (SDK 10.0.404) | 0 advertencias, 0 errores |
| `dotnet test` contra PostgreSQL 17 real (Testcontainers) | **100 de 100** pasan. Suite completa 6 veces en verde después de la corrección; pruebas de concurrencia 8 veces más en verde. |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios pendientes |
| Migración en el contenedor local (`127.0.0.1:5433`) | Aplicada; una sola migración (`20261120005504_Inicial`) |
| Arranque real de la API en Development contra la base local | Arranca, aplica migraciones, crea la agenda (7 días y 2 descansos), avisa que falta `Seed:*` sin valores; `GET api/citas` sin sesión → 401 |
| `npm run build` (frontend) | Correcto |
| `npx eslint .` (frontend) | 6 problemas (5 errores, 1 aviso), **idénticos a los de `main`**; se dejan como pediste |

Pruebas por área (100):
- **Autorización:**
  - Matriz de 401 y 403 con los cuatro roles, incluido `mi-agenda`.
  - Fichas por rol.
  - Registro.
  - DTOs sin hash ni tokens.
  - Límite de intentos de login.
  - Imágenes del catálogo.
  - Arranque sin clave JWT.
- **Base de datos:**
  - La migración se aplica desde una base vacía.
  - No traslape: citas de dos slots, otro dentista, citas contiguas y una cita sobre otra cancelada.
  - `CHECK` de rol, estado, motivo y bloqueo; FK de rol.
  - Correo único sin distinguir mayúsculas, y liberado al eliminar la cuenta.
  - Vigencia inclusiva de ofertas: aplica el primer y el último día.
  - Bitácora, historial y expediente rechazan `UPDATE`, `DELETE` y `TRUNCATE`.
- **Las tres pruebas nuevas que pediste:**
  - **Concurrencia:** dos inserciones simultáneas con barrera, directo contra la base y por la API. En ambas exactamente una se guarda; por la API la otra recibe `409` con «El dentista ya tiene una cita en ese horario.».
  - **Historial con registros eliminados:** con paciente, dentista y servicio eliminados, la cita sigue en el listado, en el detalle por id y en `mi-agenda`, con sus nombres; las listas para elegir ya no los incluyen.
  - **Bitácora sin datos sensibles:** se crea y modifica un paciente con CURP, dirección, alergias y condiciones, y un usuario con hash y token. Ningún valor aparece en `auditoria`; sí aparece `"[cambió]"`, y los datos no sensibles conservan el antes y el después.
- **Reglas:**
  - Cancelación con motivo obligatorio.
  - Una cuenta desactivada o eliminada no inicia sesión ni renueva su token.
  - Último administrador.
  - Datos iniciales: se crean una sola vez, nunca escriben los valores en los logs y respetan la longitud mínima.
  - Los seis horarios del lunes (8, 9, 10, 12, 13 y 15 h).

## 3. Acciones manuales tuyas

1. **Administrador inicial.** En `backend/` (sustituye los marcadores; no los escribas en ningún archivo del repositorio):
   ```powershell
   cd backend
   dotnet user-secrets set "Seed:AdminEmail" "<correo-del-administrador>"
   dotnet user-secrets set "Seed:AdminPassword" "<contraseña-de-al-menos-12-caracteres>"
   ```
   Arranca la API una vez (`dotnet run`) y busca en la consola «Se creó el administrador inicial.». Después borra la contraseña de user-secrets, porque ya no se usa:
   ```powershell
   dotnet user-secrets remove "Seed:AdminPassword"
   ```
   Al escribirla en la línea de comandos queda en el historial de PowerShell (`(Get-PSReadLineOption).HistorySavePath`); bórrala de ahí, o defínela solo para esa ejecución con `$env:Seed__AdminPassword = Read-Host -AsSecureString | ConvertFrom-SecureString -AsPlainText` en PowerShell 7.
2. **Secretos generados por mí:** la contraseña de PostgreSQL (en `.env` y en `ConnectionStrings:DefaultConnection`) y `JwtSettings:SecretKey` (88 caracteres). No se mostraron en ningún momento. Si prefieres los tuyos, cambia `.env` y user-secrets a la vez y recrea el volumen con `docker compose down -v` (**borra los datos locales**).
3. **Puerto:** `.env` tiene `POSTGRES_PORT=5433` por el PostgreSQL instalado en Windows (servicio `postgres`, puerto 5432). Decide si lo conservas o lo detienes; si lo detienes, puedes volver al 5432 en `.env` y en la cadena de conexión.
4. **Docker:** las pruebas del backend necesitan Docker en marcha (también en CI).
5. **Prueba manual del panel:**
   - Crear una cita y verla en el listado.
   - Cancelarla con motivo.
   - Desactivar y activar un usuario.
   - Entrar como dentista: solo debe ver Inicio.
6. **Antes de producción** (no es de esta fase, pero queda anotado):
   - Conectar la aplicación con un rol de PostgreSQL que **no** sea dueño de las tablas; el dueño o un superusuario puede desactivar los triggers de la bitácora.
   - Aplicar migraciones de forma explícita (`dotnet ef database update` o un *migration bundle*); fuera de Development no se aplican solas.
   - Definir `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey` y `Seed__*` como variables de entorno.
   - En imágenes Linux mínimas (Alpine), instalar `tzdata` para `America/Mexico_City`.
7. **Validación legal** (NOM-004-SSA3-2012 y datos personales) con un especialista. Afecta en particular a:
   - Retención de `auditoria`, que guarda IP y el correo intentado en logins fallidos.
   - El borrado lógico como forma de baja.
8. **Revisión e integración** de la rama a `main`. Desde ese momento la migración `Inicial` queda cerrada.

## 4. Diferencias con el plan

- **Migración `Inicial` regenerada en el commit 6:** las FK hacia tablas con borrado lógico son `ClientNoAction` (`NO ACTION` en la base). Sin eso, un `Remove()` de un paciente con citas cargadas fallaba antes de convertirse en borrado lógico.
- **Concurrencia:** el plan esperaba `23P01` directo contra la base, pero PostgreSQL puede abortar la segunda inserción con `40P01` (interbloqueo) cuando ambas se esperan dentro de la restricción de exclusión. La prueba directa acepta los dos códigos. La API reintenta el `40P01` hasta 3 veces y siempre responde `409`; antes de esta corrección respondía `500`.
- **`mi-agenda`** sigue funcionando aunque la ficha del dentista esté eliminada lógicamente (si su cuenta sigue activa), como pide la prueba de historial.
- **Datos de demostración en Development:** no se crearon; tu especificación del commit 8 no los incluía.
- **Agregados:**
  - `dotnet-ef` como herramienta local (`dotnet-tools.json`).
  - Fábrica de diseño (`DentissaDbContextFactory`) para generar migraciones sin secretos.
  - Un admin no puede desactivar su propia cuenta.
  - Confirmar desde el panel registra el canal `llamada`.
- **Aviso 10622 de EF silenciado:** ignorar el filtro de borrado lógico en citas es intencional.

## 5. Riesgos que quedan

1. **Token tras desactivar:** un token de acceso emitido sigue valiendo hasta 15 minutos después de desactivar la cuenta. La verificación por petición queda para la Fase 3.
2. **Carrera en la regla del último administrador:** la regla se valida en la aplicación. Dos desactivaciones simultáneas de los dos últimos administradores podrían pasar ambas. La probabilidad es baja; se cierra con un trigger o con aislamiento serializable si se considera necesario.
3. **Borrado físico de citas:** ya no existe endpoint para borrarlas, pero la tabla `citas` no tiene trigger contra `DELETE` (el plan no lo pedía). Recomiendo agregarlo en una migración nueva.
4. **Solicitudes pendientes vencidas** siguen bloqueando el horario hasta que se resuelven; la liberación es de la Fase 2 (decisión 2 del modelo).
5. **Disponibilidad simplificada:** `horarios-disponibles` no considera servicios de varios slots que crucen un descanso, la anticipación ni la liberación; eso es de la Fase 2.
6. **Expediente sin auditoría:** las tablas del expediente aún no entran en la auditoría automática. Cuando entren, todas sus columnas deben marcarse `[DatoSensible]`, y sus consultas deben ignorar el filtro de borrado lógico igual que las de citas.
7. **Historial sin consulta:** `cita_historial` se escribe, pero aún no hay endpoint para consultarlo.
8. **ESLint:** los 6 problemas preexistentes del frontend siguen ahí, como acordamos.
