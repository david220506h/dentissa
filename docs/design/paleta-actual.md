# Paleta actual del frontend

Inventario de los colores y tipografías que usa hoy el frontend: el panel y, como referencia, el sitio y las pantallas de sesión, que comparten código. Es un levantamiento de lo que existe, no una propuesta de diseño.

- **Fecha:** 2026-11-18 · **Rama:** `fase-0-seguridad`
- **Método:** búsqueda de literales `#hex`, `rgb()/rgba()`, `hsl()`, colores con nombre y `var(--…)` en `frontend/src/**/*.css`, `*.jsx` y `*.js`. Los hex de 3 dígitos se normalizaron a 6 (`#333` → `#333333`). Cada aparición cuenta una vez, incluida la definición de una variable.

## Hallazgos generales

1. **Hay dos rosas primarios distintos.**
   - El **panel** usa `--pink: #E91E8C` (Dashboard y componentes del panel).
   - El **sitio, el login y el registro** usan `#d75078`, el literal más repetido (42), con hover `#b22f56`.
   - No están unificados.
2. **Variables CSS solo en `Dashboard.css`.** Los componentes del panel (`citas.css`, `usuarios.css`, `ofertas.css`, etc.) usan las variables para fondo, borde y texto, pero escriben a mano los colores de estado. El sitio y las sesiones no usan ninguna variable.
3. **Los colores de estado siguen la escala de Tailwind**, escritos como literales: rojos `#ef4444`/`#fef2f2`, verdes `#16a34a`/`#dcfce7`, azules `#3b82f6`/`#eff6ff`, grises `#6b7280`/`#e5e7eb`. No se usa la librería, solo sus valores.
4. **El modo oscuro existe en CSS pero no se activa.** Están definidas las variables `html.dark` y las reglas `html.dark …` en los componentes, y hay estilos para `.dark-toggle`, pero ningún `.jsx` agrega la clase `dark` a `<html>`.
5. **`index.css` conserva la plantilla de Vite** (`#646cff`, `#535bf2`, `#747bff`, `#242424`, `#213547`, `rgba(255,255,255,0.87)`). No pertenece a la identidad de Dentissa.
6. **El logo es monocromo** (sección 4).

---

## 1. Variables CSS de color

Todas se definen en `frontend/src/Layouts/Dashboar/Dashboard.css`.

| Variable | Modo claro (`:root`) | Modo oscuro (`html.dark`, sin usar) | Usos de `var()` | Función |
|---|---|---|---|---|
| `--pink` | `#E91E8C` | — | **43** | Primario del panel: botones, activos, acentos |
| `--border` | `#eee` | `#2a2d3a` | 38 | Bordes y separadores |
| `--text-label` | `#888` | `#6b7280` | 34 | Etiquetas y texto secundario pequeño |
| `--text-main` | `#333` | `#f0f0f0` | 31 | Texto principal |
| `--bg-page` | `#f5f5f5` | `#0f1117` | 20 | Fondo de página |
| `--bg-card` | `#ffffff` | `#1a1d27` | 16 | Fondo de tarjetas y modales |
| `--text-soft` | `#777` | `#9ca3af` | 7 | Texto atenuado |
| `--pink-light` | `#F8BBD9` | — | 3 | Rosa claro (bordes o fondos de acento) |
| `--bg-nav` | `#ffffff` | `#13151f` | 3 | Fondo de la barra de navegación |
| `--pink-soft` | `#FFF0F7` | — | 2 | Fondo rosa muy suave (hover, resaltado) |
| `--text-stat` | `#222` | `#f0f0f0` | 1 | Cifras de las tarjetas de estadística |
| `--yellow` | `#F5A623` | — | 0 | Acento amarillo; definida pero no se usa con `var()` (el mismo valor aparece literal en `Dashboard.jsx`) |

---

## 2. Colores literales por frecuencia

Se encontraron 110 valores distintos. Abajo están todos los que aparecen 2 o más veces, más los de un solo uso que tienen una función clara. La columna «Archivos» muestra hasta 3, con la ruta relativa a `frontend/src/`.

### 2.1 Más repetidos

| # | Color | Apariciones | Archivos (máx. 3) | Función aparente |
|---|---|---|---|---|
| 1 | `#d75078` | 42 | `components/nav.css`, `Sesiones/login.css`, `Layouts/LandPague/principal.css` | **Primario del sitio y las sesiones** (botones, enlaces, títulos) |
| 2 | `white` (nombre) | 37 | `components/nav.css`, `Layouts/Dashboar/Dashboard.css`, `Layouts/Dashboar/modals/agregarServicios.css` | Texto sobre primario y fondos |
| 3 | `#333333` | 10 | `components/nav.css`, `Sesiones/login.css`, `Layouts/LandPague/principal.css` | Texto principal (equivale a `--text-main`) |
| 4 | `#ffffff` | 10 | `index.css`, `Layouts/Dashboar/Dashboard.css`, `components/citas.css` | Fondo de tarjetas y texto sobre color |
| 5 | `#f0f0f0` | 7 | `components/nav.css`, `components/servicios.css`, `Layouts/Dashboar/Dashboard.css` | Fondo gris claro; texto en modo oscuro |
| 6 | `black` (nombre) | 7 | `Sesiones/login.css`, `Sesiones/register.css`, `Layouts/Dashboar/modals/agregarServicios.css` | Texto de formularios |
| 7 | `#ef4444` | 6 | `components/citas.css`, `components/ofertas.css`, `components/comentarios.css` | **Error / eliminar** (texto e ícono) |
| 8 | `#fca5a5` | 6 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | Error: borde del botón eliminar |
| 9 | `#fef2f2` | 6 | `components/citas.css`, `components/comentarios.css`, `components/ofertas.css` | Error: fondo en hover de eliminar |
| 10 | `#3b0f0f` | 6 | `components/citas.css`, `components/comentarios.css`, `components/ofertas.css` | Error: fondo de hover en modo oscuro |
| 11 | `#2a2d3a` | 6 | `Layouts/Dashboar/Dashboard.css`, `components/pacientes.css` | Borde en modo oscuro |
| 12 | `#374151` | 6 | `components/citas.css`, `components/comentarios.css` | Borde y controles en modo oscuro |
| 13 | `#666666` | 6 | `components/nav.css`, `Layouts/LandPague/nuestrosServicios.css` | Texto secundario del sitio |
| 14 | `#93c5fd` | 5 | `components/citas.css`, `components/pacientes.css`, `components/ofertas.css` | **Acción editar**: borde azul |
| 15 | `#1e3a5f` | 5 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | Editar: hover en modo oscuro; badge en modo oscuro |
| 16 | `#2a0f1f` | 5 | `components/ofertas.css`, `components/servicios.css`, `Layouts/Dashboar/Dashboard.css` | Fondo rosa oscuro en modo oscuro |
| 17 | `#e5e7eb` | 5 | `components/citas.css`, `Layouts/Dashboar/Dashboard.css` | Bordes de modales y selects |
| 18 | `#fce7f3` | 5 | `components/servicios.css`, `components/usuarios.css`, `Layouts/LandPague/principal.css` | Fondo rosa suave (badge admin, placeholder de imagen) |
| 19 | `#3b82f6` | 4 | `components/citas.css`, `components/ofertas.css`, `components/pacientes.css` | **Acción editar**: texto e ícono azul |
| 20 | `#eff6ff` | 4 | `components/citas.css`, `components/ofertas.css`, `components/pacientes.css` | Editar: fondo en hover |
| 21 | `#6b7280` | 4 | `components/citas.css`, `components/ofertas.css`, `Layouts/Dashboar/Dashboard.css` | Texto atenuado (gris) |
| 22 | `#9ca3af` | 4 | `components/citas.css`, `components/usuarios.css`, `Layouts/Dashboar/Dashboard.css` | Texto deshabilitado o placeholder |
| 23 | `#f3f4f6` | 4 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | Fondo gris de controles |
| 24 | `#cccccc` | 4 | `components/nav.css`, `Sesiones/login.css`, `Sesiones/register.css` | Bordes de inputs (sesiones) |
| 25 | `#13151f` | 4 | `Layouts/Dashboar/Dashboard.css`, `components/pacientes.css` | Fondo de navegación en modo oscuro |
| 26 | `#1a1d27` | 4 | `Layouts/Dashboar/Dashboard.css`, `components/pacientes.css`, `components/servicios.css` | Tarjeta en modo oscuro |
| 27 | `rgba(0,0,0,0.2)` | 4 | `components/ofertas.css`, `components/servicios.css`, `Layouts/LandPague/nuestrosServicios.css` | Sombras |
| 28 | `rgba(215,80,120,0.4)` | 4 | `components/nav.css`, `Sesiones/login.css`, `Sesiones/register.css` | Sombra o halo del primario del sitio (`#d75078`) |
| 29 | `#16a34a` | 3 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | **Éxito**: texto de badge (confirmada, activo) |
| 30 | `#dcfce7` | 3 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | Éxito: fondo de badge |
| 31 | `#14532d` | 3 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | Éxito: fondo de badge en modo oscuro |
| 32 | `#4ade80` | 3 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | Éxito: texto de badge en modo oscuro |
| 33 | `#1f2937` | 3 | `components/citas.css`, `components/ofertas.css`, `components/usuarios.css` | Superficie en modo oscuro |
| 34 | `#b22f56` | 3 | `Sesiones/login.css`, `Sesiones/register.css`, `Layouts/Dashboar/modals/agregarServicios.css` | **Hover del primario del sitio** |
| 35 | `rgba(0,0,0,0.3)` | 3 | `components/nav.css`, `components/pacientes.css`, `Layouts/Dashboar/Dashboard.css` | Sombra o fondo de overlay |
| 36 | `rgba(0,0,0,0.45)` | 3 | `components/ofertas.css`, `components/servicios.css`, `components/usuarios.css` | Fondo de overlay de modales |
| 37 | `#e91e8c` | 2 | `Layouts/Dashboar/Dashboard.css`, `Layouts/Dashboar/Dashboard.jsx` | Primario del panel: definición de `--pink` y un literal en JSX |
| 38 | `#f5a623` | 2 | `Layouts/Dashboar/Dashboard.css`, `Layouts/Dashboar/Dashboard.jsx` | Acento amarillo: definición de `--yellow` y un literal en JSX |
| 39 | `#f59e0b` | 2 | `components/comentarios.css`, `Layouts/Dashboar/Dashboard.css` | **Calificación** (estrellas) y advertencia |
| 40 | `#d1d5db` | 2 | `components/comentarios.css`, `Layouts/Dashboar/Dashboard.css` | Estrella apagada o borde gris |
| 41 | `#eeeeee` | 2 | `components/nav.css`, `Layouts/Dashboar/Dashboard.css` | Bordes (equivale a `--border`) |
| 42 | `#555555` | 2 | `Layouts/Dashboar/modals/agregarServicios.css`, `Layouts/LandPague/nuestrosServicios.css` | Texto secundario |
| 43 | `#22253a` | 2 | `Layouts/Dashboar/Dashboard.css`, `components/pacientes.css` | Hover en modo oscuro |
| 44 | `#111827` | 2 | `components/citas.css` | Texto oscuro de modales |
| 45 | `#f9fafb` | 2 | `components/citas.css` | Fondo muy claro de modales |
| 46 | `rgba(233,30,140,0.08)` / `0.1` / `0.3` | 2 c/u | `components/comentarios.css`, `components/pacientes.css`, `components/citas.css` | Transparencias del primario del panel (`#E91E8C`) para fondos, focos y sombras |
| 47 | `rgba(0,0,0,0.05)` / `0.1` / `0.55` | 2 c/u | `Layouts/Dashboar/Dashboard.css`, `components/nav.css`, `components/pacientes.css` | Sombras y overlays |
| 48 | `transparent` (nombre) | 15 | `Layouts/Dashboar/Dashboard.css`, `components/pacientes.css`, `components/citas.css` | Fondos de botones secundarios |
| 49 | `#646cff`, `#535bf2`, `#747bff`, `#242424`, `#213547` | 1–2 | `index.css` | Plantilla de Vite, no es de Dentissa |

### 2.2 Estados de cita (`components/citas.css`)

| Estado | Claro: fondo / texto | Oscuro: fondo / texto |
|---|---|---|
| Confirmada | `#dcfce7` / `#16a34a` | `#14532d` / `#4ade80` |
| Pendiente | `#fef9c3` / `#854d0e` | `#422006` / `#fde047` |
| Cancelada | `#fee2e2` / `#dc2626` | `#450a0a` / `#f87171` |
| Botón confirmar | borde `#86efac`, texto `#22c55e`, hover `#f0fdf4` | hover `#0f2e1a` |

### 2.3 Otros de un solo uso con función clara

| Color | Archivo | Función |
|---|---|---|
| `#9b294b` | `Layouts/LandPague/principal.css` | Variante oscura del rosa del sitio |
| `#a3264b` / `#fdecef` | `components/nav.css` | Aviso de sesión expirada: texto y fondo (agregado en la Fase 0) |
| `#ffe6ee`, `#fff5f8`, `#ffe0f0`, `#ffe4f0`, `#f5c6e0`, `#f9a8d4` | `agregarServicios.css`, `Dashboard.css`, `servicios.css`, `usuarios.css` | Rosas pastel para fondos y badges |
| `#dbeafe` / `#1e40af`, `#d1fae5` / `#065f46` | `components/usuarios.css` | Badges de rol (azul y verde) |
| `#1e1b4b`, `#312e81`, `#4c1d95`, `#a5b4fc` | `Layouts/Dashboar/Dashboard.css` | Interruptor de modo oscuro (índigo y violeta) |
| `rgb(0,28,52)` | `Sesiones/login.css` | Azul marino puntual |
| `'red'` (estilo en línea) | `Sesiones/login.jsx`, `Sesiones/register.jsx` | Mensaje de error del formulario |

### 2.4 Resumen por función

| Función | Panel | Sitio y sesiones |
|---|---|---|
| Primario | `#E91E8C` (`--pink`) | `#d75078`; hover `#b22f56` |
| Primario suave | `#F8BBD9`, `#FFF0F7`, `#fce7f3` | `#fdecef`, `#ffe6ee`, `#fff5f8` |
| Acento | `#F5A623` (`--yellow`), `#f59e0b` (estrellas) | — |
| Fondo de página / tarjeta | `#f5f5f5` / `#ffffff` | `white`, `#f8f8f8`, `#f8f9fa` |
| Texto principal / secundario | `#333` / `#777`, `#888`, `#6b7280` | `#333`, `black` / `#666`, `#555` |
| Bordes | `#eee`, `#e5e7eb` | `#cccccc` |
| Éxito | `#16a34a` sobre `#dcfce7` | — |
| Error | `#ef4444`, `#dc2626` sobre `#fef2f2`/`#fee2e2` | `'red'` |
| Advertencia / pendiente | `#854d0e` sobre `#fef9c3` | — |
| Información / editar | `#3b82f6` sobre `#eff6ff`, borde `#93c5fd` | — |

---

## 3. Colores del logo (`frontend/public/logo.svg`)

| Elemento | Color | Nota |
|---|---|---|
| Fondo (`<rect>` en el grupo `Background`) | `#FFFFFF` | Rectángulo blanco de 2200 × 2200 |
| Figura (2 `<path>` en el grupo `Objects`) | negro (`#000000`) | Los trazados no declaran `fill`, así que SVG los pinta de negro por defecto |

El logo es **monocromo, negro sobre blanco**, exportado con Adobe Illustrator 27.5. No tiene ninguno de los rosas de la interfaz. Como el fondo blanco es un rectángulo y no transparencia, sobre fondos de color o en modo oscuro se verá como un cuadro blanco.

---

## 4. Tipografías (referencia)

| Fuente declarada | Dónde | Observación |
|---|---|---|
| `'Nunito', sans-serif` | `Layouts/Dashboar/Dashboard.css` (3 declaraciones, incluido `body`) | **No se carga en ningún lugar**: no hay `<link>` de Google Fonts en `index.html` ni `@import`/`@font-face` en el CSS. Solo se ve si el equipo la tiene instalada; si no, cae en la sans-serif del sistema. |
| `system-ui, Avenir, Helvetica, Arial, sans-serif` | `index.css` (`:root`) | Pila de la plantilla de Vite |
| `arial` | `components/nav.css` (2) | Barra de navegación |
| `inherit` | `citas.css`, `usuarios.css`, `ofertas.css`, `pacientes.css`, `servicios.css` (19 en total) | Controles de formulario que heredan la fuente del contenedor |
