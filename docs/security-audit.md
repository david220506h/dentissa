# Auditoría de seguridad — Fase 0 (seguridad base)

- **Fecha:** 2026-11-18
- **Rama:** `fase-0-seguridad` (a partir del commit `baseline` en `main`)
- **Alcance:** backend ASP.NET Core (.NET 9) y panel React + Vite. Sin cambios de proveedor de base de datos ni de esquema.

Este documento no contiene secretos. Las referencias a archivo y línea corresponden al commit `baseline` (`dc2c855`).

---

## 1. Hallazgos iniciales

| # | Severidad | Hallazgo | Ubicación (baseline) | Estado |
|---|---|---|---|---|
| H1 | Crítica | Token personal de GitHub escrito en el código del frontend; se descargaba en el navegador y la app se publicó por un túnel de Cloudflare. Debe darse por comprometido. | `frontend/src/utils/githubUpload.jsx:9` (valor ya redactado en el baseline) | Corregido: archivo eliminado, subida por el backend. **Revocar el token (acción manual).** |
| H2 | Crítica | `POST api/Usuarios` público: cualquiera podía crear un usuario `admin` sin iniciar sesión. | `backend/Controllers/UsuariosController.cs:40-57` | Corregido |
| H3 | Crítica | Ningún controlador salvo `logout` tenía `[Authorize]`: usuarios y citas (datos de salud) accesibles sin sesión. | `UsuariosController.cs`, `citasController.cs` | Corregido |
| H4 | Alta | `GET api/Citas/{id}` devolvía la entidad `Cita` con `Cliente.Usuario` y `Trabajador.Usuario`: hash de contraseña, refresh token vigente, correo, teléfono, CURP, sexo y fecha de nacimiento. | `citasController.cs:55`, `CitasRepository.cs:35-40` | Corregido |
| H5 | Alta | Cadena de conexión con la cuenta `sa` y su contraseña, y clave de firma JWT, en texto plano en `appsettings.json`. | `backend/appsettings.json:3,6` | Corregido en código (user-secrets / variables de entorno). **Rotar ambos (acción manual).** |
| H6 | Alta | CORS abierto: `.AllowAnyOrigin()` anulaba la lista de `WithOrigins`. | `backend/Program.cs:52-60` | Corregido |
| H7 | Media | Sin límite de peticiones en login y registro (fuerza bruta sin restricción). | `Program.cs` | Corregido |
| H8 | Media | Sin HSTS ni cabeceras de seguridad; la redirección a HTTPS no tenía efecto con el perfil `http`. | `Program.cs:103`, `Properties/launchSettings.json` | Corregido para producción (ver R5 para desarrollo) |
| H9 | Media | El frontend llamaba a citas y usuarios con `fetch` sin token; el panel solo se mostraba al admin. | `frontend/src/components/citas.jsx`, `usuarios.jsx`, `App.jsx:22` | Corregido |
| H10 | Media | `authService.js` apuntaba a una IP de la red local por HTTP, escrita en el código; `vite.config.js` tenía la IP del backend y `.trycloudflare.com`. | `frontend/src/services/authService.js:7`, `frontend/vite.config.js` | Corregido (variables de entorno) |
| H11 | Media | Refresh token guardado en texto plano en `Usuarios.RefreshToken`; se busca por igualdad. Se rota en cada uso, pero no hay detección de reutilización. | `Services/AuthService.cs:80`, `Repositories/AuthRepository.cs:22` | **Pendiente** (Fase posterior) |
| H12 | Media | Access token, refresh token y datos del usuario en `localStorage` (expuestos ante XSS). | `frontend/src/services/authService.js:11-14` | **Pendiente** (Fase posterior) |
| H13 | Media | Borrado físico de usuarios y citas, contrario a la regla de eliminados lógicos de `CLAUDE.md` §5. | `UsuariosController.cs:76-85`, `citasController.cs:185-194` | **Pendiente** (requiere cambio de esquema) |
| H14 | Baja | No existía repositorio Git ni `.gitignore` en la raíz: `bin/` (con copia de `appsettings.json` y sus secretos) y `.vs/` habrían quedado versionados. | raíz | Corregido |
| H15 | Baja | `register` **no** permitía elegir el rol (`RegisterDto` no tiene campo `Rol`; `AuthService` fija `cliente`). El riesgo real era H2. | `DTOs/AuthDtos.cs`, `AuthService.cs:43` | Verificado con prueba |
| H16 | Baja | DTOs vacíos sin uso. | `DTOs/AuthResponseDto.cs`, `DTOs/RefreshTokenDto.cs` | Eliminados |

**Historial de Git:** no existía repositorio antes de esta fase, así que no hay historial con secretos. El commit `baseline` se creó con los valores secretos ya vaciados. Se revisaron todas las revisiones con patrones de token de GitHub, contraseñas en cadenas de conexión, claves JWT con valor y llaves privadas: **sin coincidencias**. Lo único que queda en el historial es el nombre de host del túnel de Cloudflare (no es un secreto), en el baseline y hasta `fase 0.5`.

---

## 2. Qué se corrigió (por commit)

| Commit | Cambio |
|---|---|
| `dc2c855` baseline | Código original con solo los secretos vaciados y `.gitignore` en la raíz. |
| `4537cd0` fase 0.1 | `UserSecretsId`; `appsettings.Example.json`; la API **no arranca** si falta la cadena de conexión o si la clave JWT falta o mide menos de 32 bytes. |
| `04ec762` fase 0.2 | Se elimina la subida a GitHub desde el navegador. `POST api/catalogo/imagenes` (solo admin): JPG, PNG o WEBP verificados por la firma de los bytes, máximo 5 MB, nombre GUID generado por el servidor, guardado en `storage/catalogo` (configurable, fuera de la raíz pública, ignorado por Git). `GET api/catalogo/imagenes/{nombre}` público, solo para nombres generados. **Exclusivo del catálogo**: no es un almacén genérico y no debe usarse para fotos clínicas. |
| `caed7fd` fase 0.3 | `Constants/Roles.cs`. `UsuariosController` solo admin (con chequeo explícito para crear admin); `CitasController` admin y secretaria. Política de respaldo: todo endpoint sin atributo exige sesión. |
| `d573eee` fase 0.4 | El registro usa `Roles.Cliente`; un `rol` enviado se ignora. |
| `3cdd2a7` fase 0.5 | DTOs con nombre en todas las respuestas; `GET api/Citas/{id}` ya no expone datos de cuenta ni personales. |
| `85cc826` fase 0.6 | CORS solo con `Cors:AllowedOrigins`. |
| `6c9644f` fase 0.7 | `authFetch` en citas y usuarios; manejo de 401 (renueva una vez o lleva al inicio de sesión con aviso) y 403 (mensaje claro); panel para admin y secretaria, secciones de administración solo admin; proxy y hosts de Vite desde `.env.local`. |
| `36a38fd` fase 0.8 | Rate limiter: login 5/min por IP + correo y 30/min por IP; registro 3 cada 10 min por IP (todo en `RateLimiting:*`). HSTS y redirección HTTPS fuera de Development. Cabeceras `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` y CSP. |
| `4d49542` fase 0.9 | 43 pruebas de integración (`tests/backend.Tests`). |

### Matriz de acceso vigente

| Endpoint | Público | cliente | secretaria | admin |
|---|---|---|---|---|
| `POST api/auth/register`, `login`, `refresh` | ✅ (con límite) | ✅ | ✅ | ✅ |
| `POST api/auth/logout` | — | ✅ | ✅ | ✅ |
| `api/Citas/**` | — | ❌ 403 | ✅ | ✅ |
| `api/Usuarios/**` | — | ❌ 403 | ❌ 403 | ✅ |
| `POST api/catalogo/imagenes` | — | ❌ 403 | ❌ 403 | ✅ |
| `GET api/catalogo/imagenes/{nombre}` | ✅ | ✅ | ✅ | ✅ |

En el panel, la secretaria ve Dashboard, Citas y Pacientes; Servicios, Ofertas, Comentarios y Usuarios son solo para admin. Ocultar secciones en el panel es solo una comodidad: la autoridad es el backend.

---

## 3. Configuración

### Desarrollo (user-secrets, fuera del repositorio)

```powershell
cd backend
# Generar una clave aleatoria (PowerShell 5.1)
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)

dotnet user-secrets set "JwtSettings:SecretKey" "<clave generada>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena de conexión>"
```

Frontend: copiar `frontend/.env.example` como `frontend/.env.local` (no se versiona) y ajustar `API_PROXY_TARGET` (por ejemplo, la IP del equipo del backend) y, si se usa un túnel, `DEV_ALLOWED_HOSTS`.

### Producción (variables de entorno)

| Variable | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión (cuenta con permisos mínimos, no `sa`) |
| `JwtSettings__SecretKey` | Clave de firma JWT, mínimo 32 bytes aleatorios |
| `Cors__AllowedOrigins__0`, `__1`… | Orígenes permitidos del frontend |
| `CatalogoImagenes__Ruta` | Carpeta de imágenes del catálogo (persistente y con respaldo) |
| `RateLimiting__...` | Ajuste de límites, si hace falta |

### Ejecutar las pruebas

```powershell
dotnet test Dentissa.sln
```

En este equipo solo está instalado el runtime de .NET 11; para ejecutar la API o las pruebas `net9.0` hay que instalar el runtime de .NET 9 o definir `DOTNET_ROLL_FORWARD=Major` en la sesión.

---

## 4. Pendientes para fases posteriores

| # | Pendiente | Notas |
|---|---|---|
| P1 | **Borrado físico de usuarios y citas** (H13) | Pasar a eliminado lógico (`Activo`/`EliminadoEn`). Requiere cambio de esquema: Fase 1, junto con la migración a PostgreSQL. |
| P2 | **Refresh token en texto plano sin detección de reutilización** (H11) | Guardar solo el hash (SHA-256), tabla de tokens por sesión o dispositivo y familia de rotación: si se reutiliza un token ya rotado, revocar toda la familia. |
| P3 | **Tokens en `localStorage`** (H12) | Refresh token en cookie `HttpOnly; Secure; SameSite=Strict`, con protección CSRF en `refresh`; access token solo en memoria. |
| P4 | Registro de auditoría de acciones críticas | Exigido por `CLAUDE.md` §5; no existe aún. |
| P5 | Revocación inmediata | Un access token sigue válido hasta 60 min después de eliminar al usuario o cambiar su rol. Valorar una vida más corta o la validación del estado del usuario. |
| P6 | Rate limiting detrás de un proxy | Hoy se usa la IP de la conexión. Detrás de un proxy inverso o túnel, todos comparten IP. Configurar `ForwardedHeaders` solo con proxies conocidos. El límite vive en memoria: se reinicia con la app y no se comparte entre instancias. |
| P7 | `horarios-disponibles` no distingue trabajador | Marca la hora como ocupada para toda la clínica. Se rediseña con el horario configurable de la Fase 1. |
| P8 | Validación de `CrearUsuarioDto` | No tiene `[EmailAddress]`, `[MinLength]` ni `[MaxLength]` como `RegisterDto`. Política de contraseñas mínima (6 caracteres). |
| P9 | Enumeración de correos | `register` y `POST api/Usuarios` responden "El correo ya está registrado". |
| P10 | Imágenes del catálogo | Se valida la firma, pero no se re-codifica la imagen (no elimina metadatos EXIF ni contenido adicional). Mitigado con `nosniff`, `Content-Type` fijo y CSP. |
| P11 | HTTPS en desarrollo | En la red local las credenciales viajan por HTTP (Vite → API en el puerto 5058). |
| P12 | Calidad del frontend | `lint` tiene 5 errores y 1 advertencia que ya existían. `citas.jsx`/`usuarios.jsx` importan `./Citas.css`/`./Usuarios.css` con mayúscula y los archivos son en minúscula: compila en Windows, pero fallará en Linux. El modal `agregarServicios.jsx` no está montado en ninguna pantalla y no guarda el servicio en la base. |

---

## 5. Acciones manuales del responsable

1. **Revocar el token personal de GitHub** que estaba en `githubUpload.jsx` (GitHub → Settings → Developer settings → Personal access tokens). Revisar el historial del repositorio `imagenes-dentista` y del resto de repositorios a los que tenía acceso, por si hay commits no reconocidos.
2. **Cambiar la contraseña de `sa`** en SQL Server y crear una cuenta de aplicación con permisos mínimos sobre `DentissaDB`. Configurarla con user-secrets.
3. **Rotar la clave JWT**: generar una nueva (sección 3) y configurarla. Esto invalida todos los tokens emitidos con la anterior.
4. Invalidar los refresh tokens existentes (vaciar `Usuarios.RefreshToken` y `RefreshTokenExpiration`), porque se guardan en texto plano y pudieron exponerse por H4.
5. Crear `frontend/.env.local` a partir de `.env.example`.
6. Borrar las copias locales con secretos cuando ya no hagan falta: `backend/bin/` y `backend/obj/` (se regeneran al compilar), `Documents/Dentissa.rar` y la instantánea temporal de esta sesión.
7. Cuando exista el remoto privado: revisar el historial antes del primer `push`. No hay secretos en ninguna revisión, pero el baseline conserva el host del túnel de Cloudflare. No se reescribió el historial.

---

## 6. Riesgos abiertos

- **XSS → robo de sesión** mientras los tokens vivan en `localStorage` (P3).
- **Refresh token en texto plano**: quien lea la tabla `Usuarios` puede suplantar sesiones (P2).
- **Token de GitHub comprometido** hasta que se revoque (acción manual 1).
- **Contraseña de `sa` y clave JWT anteriores** expuestas hasta que se roten (acciones 2 y 3).
- **Eliminación física** de datos que, por regulación (NOM-004-SSA3-2012), podrían requerir conservación (P1). La interpretación legal debe validarla un especialista.
- **Sin auditoría** de quién crea, edita o borra usuarios y citas (P4).
