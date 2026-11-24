# Código para migrar al sitio en Astro

Landing, menú, inicio de sesión de pacientes y registro del frontend anterior. **Ya no forman parte del panel**, que es solo para el personal y tiene su propio inicio de sesión (`src/features/auth/`). Se conservan aquí, sin borrar, hasta construir el sitio comercial y el portal del paciente (etapa 2 de `CLAUDE.md`). El endpoint de registro del backend se queda.

| Archivo | Qué era |
|---|---|
| `Layouts/LandPague/principal.jsx` y `.css` | Página principal (landing). |
| `Layouts/LandPague/nuestrosServicios.jsx` y `.css` | Sección de servicios de la landing (datos fijos). |
| `components/nav.jsx` y `.css` | Menú de la landing; abre el inicio de sesión y el registro. |
| `Sesiones/login.jsx` y `.css` | Inicio de sesión de pacientes desde la landing. |
| `Sesiones/register.jsx` y `.css` | Registro de pacientes (`POST /api/auth/register`). |

- **Nada aquí se compila ni se importa desde el panel.** Solo se ajustó la ruta relativa a `services/authService.js`, que sigue en `src/services/` porque el panel también la usa.
- Dependencias que solo usa este código: `react-icons` e `iconoir-react`. Al migrar, se mueven al proyecto del sitio y se quitan del panel.

## Pendiente

- **Imagen externa.** `Layouts/LandPague/principal.jsx` carga una imagen directamente de `decogaleria.mx` (clase `imagen-carita-feliz`). No se descargó ni se copió al repositorio. Al migrar hay que sustituirla por una imagen con derechos de uso para la clínica.
- Los colores de este código son los anteriores (rosa `#d75078`); el sitio usará los tokens de `@dentissa/tokens`.
