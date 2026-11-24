# Despliegue gratuito: Neon + Render + Vercel

Pon la demo en internet con **datos ficticios**. No es el despliegue con datos reales: ese exige primero `docs/requisitos-despliegue.md` (tokens en cookie, revisión legal, correo con dominio, etc.).

| Pieza | Servicio | Qué corre |
|---|---|---|
| Base de datos | **Neon** (PostgreSQL 17, gratis) | Los datos |
| API | **Render** (plan gratis, Docker) | `backend/Dockerfile` en modo **Production** |
| Panel y sitio | **Vercel** (gratis) | `frontend/dist`; `/api/*` se reenvía a Render |

**Despliegue actual (demo):** sitio en https://dentissa-two.vercel.app (panel del personal en https://dentissa-two.vercel.app/panel) y API en https://dentissa-api-22cy.onrender.com (salud: https://dentissa-api-22cy.onrender.com/health).

Orden: **1. Neon → 2. Render → 3. Vercel**. Necesitas una cuenta en cada uno (puedes entrar con GitHub) y el repositorio en GitHub.

## 0. Genera los secretos (PowerShell, en tu equipo)

Abre PowerShell y ejecuta **una vez por valor**. Copia cada resultado a un lugar seguro (un gestor de contraseñas); **nunca** al repositorio.

```powershell
# Clave JWT (64 caracteres al azar)
$b = New-Object byte[] 48; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)

# Contraseña (24 caracteres, sin símbolos). Ejecútalo dos veces: una para administración y otra para el personal
$b = New-Object byte[] 18; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b) -replace '[+/=]', ''
```

Obtienes tres valores: **clave JWT**, **contraseña de administración** y **contraseña del personal**.

## 1. Neon (base de datos)

1. Entra a https://neon.tech y crea un proyecto: nombre `dentissa`, **PostgreSQL 17** y la región más cercana a la de Render (p. ej., *AWS US West (Oregon)*).
2. En el **Dashboard** del proyecto, pulsa **Connect**. En la ventana:
   - Base de datos: `neondb`. Rol: el que aparece (p. ej., `neondb_owner`).
   - **Desactiva "Connection pooling"** (la API necesita la conexión directa: el nombre del host no debe llevar `-pooler`).
   - Copia la cadena que empieza con `postgresql://…?sslmode=require…`. Es la **cadena de conexión**: contiene una contraseña, trátala como secreto.

La API crea todas las tablas sola al arrancar (migraciones); no hay que ejecutar nada en Neon.

## 2. Render (API)

1. Entra a https://render.com → **New +** → **Blueprint** → conecta GitHub y elige el repositorio `dentissa`. Render lee `render.yaml` y propone el servicio **dentissa-api** (Docker, plan Free).
2. Render pide los 4 valores marcados como secretos. Pega:

| Variable | Valor |
|---|---|
| `ConnectionStrings__DefaultConnection` | La cadena de Neon del paso 1 (tal cual, `postgresql://…`) |
| `JwtSettings__SecretKey` | La **clave JWT** del paso 0 |
| `DEMO_PASSWORD_ADMIN` | La **contraseña de administración** |
| `DEMO_PASSWORD_PERSONAL` | La **contraseña del personal** (la comparten recepción y los 3 dentistas) |

   Las demás variables ya vienen en `render.yaml` sin secretos: `ASPNETCORE_ENVIRONMENT=Production`, migraciones al arrancar, `Proxy__Saltos=2`, correo apagado y `DEMO_SEMBRAR=true`.
3. Pulsa **Apply**. La primera compilación tarda unos 5 a 10 minutos.
4. Cuando diga **Live**, anota la URL (la de esta demo es `https://dentissa-api-22cy.onrender.com`) y abre `https://<tu-url>/health`: debe responder `{"estado":"ok"}`.

**Si tu URL no es `https://dentissa-api-22cy.onrender.com`** (Render agrega letras si el nombre está ocupado), edita `vercel.json` en la raíz del repositorio, cambia esa URL en la regla de `/api/:path*` y haz commit y push antes del paso 3.

Al primer arranque con la base vacía, la API siembra los datos ficticios (30 pacientes, citas de dos semanas) y crea las cuentas con las contraseñas que pegaste. **Las contraseñas no aparecen en los logs.** En arranques posteriores no siembra de nuevo.

## 3. Vercel (panel)

1. Entra a https://vercel.com → **Add New… → Project** → importa el repositorio `dentissa`.
2. Configuración:
   - **Root Directory:** déjalo en la raíz del repositorio (`./`).
   - **Framework Preset:** *Other*. No cambies los comandos: los toma de `vercel.json` (`npm ci`, `npm run build -w frontend`, salida `frontend/dist`).
   - **Environment Variables:** ninguna.
3. Pulsa **Deploy**. Anota la URL (p. ej., `https://dentissa.vercel.app`).

## 4. Comprobar

1. Abre `https://<render>/health` para despertar la API (el plan gratis la duerme tras 15 minutos sin uso; despertar tarda cerca de un minuto).
2. Abre la URL de Vercel: `/` es el sitio público de la clínica y `/panel` el panel del personal. Inicia sesión en `/panel`:

| Cuenta | Correo | Contraseña |
|---|---|---|
| Administración | `administracion@clinica-demo.test` | `DEMO_PASSWORD_ADMIN` |
| Recepción | `recepcion@clinica-demo.test` | `DEMO_PASSWORD_PERSONAL` |
| Dentistas | `mariana.soto@`, `andres.lara@`, `elena.rios@clinica-demo.test` | `DEMO_PASSWORD_PERSONAL` |

Cada push a `main` vuelve a desplegar Render y Vercel solos.

## Cómo está protegida la API en producción

- `Production`: sin Swagger, errores 500 sin detalles internos, HSTS.
- CORS cerrado (sin orígenes): el navegador solo habla con Vercel, que reenvía `/api` a Render.
- Cabeceras de seguridad en la API y en Vercel; límite de intentos de inicio de sesión por IP y correo (la IP real llega por `X-Forwarded-For`, con dos proxies de confianza).
- Correo apagado: los avisos quedan en la bandeja de salida sin enviarse. Sin correo no corre ningún trabajo en segundo plano, así que no importa que Render duerma el servicio.

## Empezar de cero

En Neon, **Branches → main → Reset** (o borra y crea el proyecto) y luego, en Render, **Manual Deploy → Restart**. La API vuelve a crear las tablas y a sembrar.

## Problemas comunes

- **El panel dice "Sin conexión con la clínica" o el inicio de sesión tarda mucho:** la API está dormida. Abre `/health`, espera a que responda y vuelve a intentar.
- **`/health` responde 503:** la API no llega a Neon. Revisa la cadena de conexión (sin `-pooler`, con `sslmode=require`).
- **No se puede entrar con las cuentas demo:** revisa en los logs de Render que diga "Se sembraron los datos de la demo". Si dice que faltan contraseñas, deben tener al menos 12 caracteres; corrígelas y empieza de cero.
