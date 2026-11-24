# Plan de la Fase 3a: base del panel (frontend)

> **Estado:** **aprobado** (2026-11-23), con las decisiones de la sección 14, que prevalecen sobre el resto del documento donde lo contradigan.
> **Rama:** `fase-3a-panel-base`. Creada desde `main` tras integrar la Fase 1 por avance rápido (2026-11-23) y rebasada sobre `main` después de integrar `fase-2-agenda` (merge `a3107a4`).
> **Fuentes, en orden de prioridad:**
> 1. `CLAUDE.md` y las reglas de seguridad.
> 2. El diseño aprobado en `docs/design/panel/*.dc.html` (`Main` = sistema visual y permisos; `Movil` = celular).
> 3. Accesibilidad AA.
> 4. Las skills instaladas.
> **Alcance:** solo frontend; el backend no cambia.

---

## 0. Skills y hooks activos (revisión previa)

### 0.1 Hooks

| Dónde | Qué hay | Efecto |
|---|---|---|
| `Dentissa/.claude/settings.json` y `settings.local.json` | **No existen** (no hay carpeta `.claude` en el proyecto). | Ninguno. |
| `~/.claude/settings.json` | Solo `theme` y `autoUpdatesChannel`. | Ninguno. |
| Plugins | `installed_plugins.json` vacío. Los `hooks.json` que existen están en la caché del marketplace oficial, de plugins **no instalados**. | No se ejecutan. |
| **`design-taste-frontend` (taste-skill)** | Su `SKILL.md` declara un hook **`Stop`** propio, activo mientras el skill está en uso: ejecuta `node scripts/gate.mjs --hook claude`. | Si existe `.taste-skill/build.json` en el proyecto (lo crea su comando `--start`), **impide terminar el turno** hasta que una página pase sus revisiones de **sitio de marketing**: un hero que entra al cargar, un "momento" por página, una sección por pantalla, en Chrome sin interfaz. Sin ese archivo sale de inmediato y no hace nada. **No lo vamos a armar nunca**: sus reglas no aplican a un panel. |
| `impeccable` | Ofrece un hook opcional (`/impeccable hooks on`) que corre el detector tras cada edición de UI. | **No está activo y no lo activo.** El detector se corre a mano antes de cada commit, como pediste. |

Otros puntos de impeccable que conviene que conozcas:
- **`impeccable.exe` v0.1.27:** binario nativo que instaló en `C:\Users\David Dz\.impeccable\`.
- **Telemetría:** tiene telemetría anónima de uso (`npx impeccable telemetry status|off`). **[DECIDE]** si la apago; propongo apagarla.
- **`npx impeccable login`:** sube críticas y auditorías a su cuenta en línea. No se usa.
- **`live`:** inyecta scripts y propone editar la CSP. No se usa.

### 0.2 Qué aplico de cada skill y qué no

**Impeccable** (modo **Operate**: panel de trabajo, no sitio de marketing).

| Se aplica | No se aplica (choca con el diseño o con lo que pediste) |
|---|---|
| `init`: `PRODUCT.md` con la verdad del producto (usuarios, tareas, roles, restricciones). | `bolder`, `quieter`, `colorize`, `delight`, `overdrive`, `typeset`, `layout`, `craft`, `generate`, `live`, `world` (cambian la estética o crean otra fuente de verdad). |
| `critique`, `audit`, `polish` y `harden` en cada hito, corrigiendo solo lo coherente con el diseño. | El flujo `new-work` ni el "mundo visual": el mundo ya existe y es el diseño aprobado. |
| `npx impeccable@4.10.0 detect src/` antes de cada commit. | De su `craft-floor`, la lista "Refuse": las **tarjetas de indicadores de Inicio** (número grande y etiqueta) que el diseño sí pide; el diseño gana. |
| Reglas de Operate compatibles: transiciones de 150–250 ms, nada de animaciones de carga orquestadas, densidad permitida, vocabulario de componentes consistente, estados (hover, deshabilitado, carga, error, vacío). | "Tematizar las superficies del navegador" (selección de texto, cursor, barras de desplazamiento) con colores que el diseño no define. Se reporta, no se aplica. |
| `DESIGN.md` como **índice** que apunta a `docs/design/panel/` y al paquete de tokens, sin valores propios (sección 2.6). | `document` y `extract` en modo de generación libre: escribirían otro `DESIGN.md` con valores deducidos del código. |

**Emil Kowalski** (`emil-design-eng`, `animate`, `review-animations`): solo microinteracciones y movimiento.
- **Se aplica:**
  - el filtro "¿debe animarse?" (nada en acciones de teclado ni en lo que se usa cientos de veces al día);
  - solo `transform` y `opacity`;
  - nunca `ease-in` ni `scale(0)`;
  - curvas `--ease-out: cubic-bezier(0.23, 1, 0.32, 1)` y `--ease-drawer: cubic-bezier(0.32, 0.72, 0, 1)`;
  - menos de 300 ms, salvo hojas y diálogos;
  - `prefers-reduced-motion` y `hover: hover`;
  - transiciones CSS en lugar de keyframes.
- **No se aplica:** resortes, gestos de arrastre, `clip-path` decorativo, desenfoque como transición, efectos de "delight". Propuesta concreta en la sección 4.

**taste-skill** (`design-taste-frontend`): solo como **crítica de criterio visual** (jerarquía, ritmo, consistencia).
- **No se aplica nada de su método:** está pensado para sitios de empresa, con hero, fotografías, "momentos", una sección por pantalla y shaders, y nada de eso corresponde a un panel.
- **No se arma su compuerta** (`gate.mjs --start`).
- Sus hallazgos que contradigan el diseño se te reportan y no se aplican.

---

## 1. Estado actual del frontend

React 19 + Vite 7, JavaScript sin tipos, `react-router-dom` 7 instalado pero **sin usar** (la navegación es por estado en `App.jsx`), CSS global por componente, `react-icons` e `iconoir-react`, React Compiler activado.

| Pieza | Estado | Qué pasa en esta fase |
|---|---|---|
| `services/authService.js` (login, refresh, logout, `authFetch` con renovación y avisos 401/403) | Funciona y está probado contra la API de la Fase 1. | **Se conserva la lógica.** Se pasa a TypeScript con el mismo comportamiento; los tokens siguen en `localStorage` (riesgo P3 de la Fase 0; moverlos a cookie requiere cambiar el backend, fuera de alcance). |
| Inicio de sesión (`Sesiones/login.jsx`, modal sobre la landing) | Funciona. | Se **rediseña** como página según `Login.dc.html`. Solo para el personal: una cuenta `paciente` ve "¿Eres paciente? Entra desde el sitio web de la clínica." y no entra al panel. |
| Registro (`Sesiones/register.jsx`) | Funciona; crea pacientes con ficha. | **No es del panel** (es del sitio y portal). Se conserva sin cambios, ver la landing. |
| Landing (`Layouts/LandPague/*`, `components/nav.jsx`) | Sitio comercial provisional. | **[DECIDE]** Propuesta: se conserva sin cambios en la ruta `/sitio`, con su login y registro, hasta que exista el sitio en Astro. El panel ocupa `/`. |
| Citas (`components/citas.jsx`): listar, crear, editar, confirmar, cancelar con motivo | Funciona contra la API de la Fase 1. | Se **rediseña** como Agenda (sección 10). Ninguna acción se pierde. |
| Usuarios (`components/usuarios.jsx`): listar, crear (con ficha por rol), desactivar y activar | Funciona. | Se **rediseña** como "Equipo y usuarios". Ninguna acción se pierde. |
| `services/catalogoService.js` (subida de imágenes) y `modals/agregarServicios.jsx` | El modal no está montado (P12). | Se conserva el servicio; la pantalla de Servicios llega con su fase. |
| Pacientes, Servicios, Ofertas, Comentarios (`components/*.jsx`) | **Datos fijos en el código**, sin API. | Se sustituyen por "Próximamente". No es funcionalidad real, pero te lo aviso. Comentarios se eliminó por decisión. |
| ESLint | 5 errores y 1 aviso preexistentes. | Desaparecen con los archivos que se reemplazan; el código nuevo no agrega ninguno. |

**El viejo `Dashboard.jsx` y sus pantallas se retiran en un commit propio al final**, solo con tu confirmación y después de que el panel nuevo cubra todo lo que funcionaba.

---

## 2. Decisiones técnicas

Las versiones exactas se fijan al instalar, con al menos dos semanas de publicadas, y quedan en el `package-lock.json`.

| Tema | Propuesta | Por qué | Alternativas descartadas |
|---|---|---|---|
| **Lenguaje** | **TypeScript estricto** en todo el código nuevo; el código viejo se queda en JS hasta retirarse. | Los DTO de la API (citas, usuarios, roles, estados) son el punto de falla más probable; el compilador los vigila. Comparte tipos con el futuro sitio. | JSDoc: menos garantías. Migrar todo de golpe: tocaría código que se va a retirar. |
| **Enrutador** | **React Router 7** (ya instalado, modo declarativo), con rutas protegidas por rol. | Ya es dependencia; maduro; cubre rutas anidadas, guardas y carga diferida por pantalla. | TanStack Router: tipado de rutas superior, pero es otra dependencia y migración. |
| **Datos del servidor** | **TanStack Query** con caché **solo en memoria** (sin persistencia), `gcTime` corto, `retry` limitado, y `queryClient.clear()` al cerrar sesión, al expirar la sesión y al perder la conexión. | Caché, reintentos, invalidación tras crear o editar, estados de carga y error uniformes. Que no persista es requisito: ningún dato de paciente toca el disco. | Hooks propios con `fetch`: más código y más errores de estados. SWR: equivalente, menos herramientas de mutación. |
| **Estilos** | **Variables CSS** generadas del sistema visual (`packages/tokens`) + **CSS Modules** por componente. | Sin dependencias; los valores salen de una sola fuente; el sitio en Astro puede usar los mismos tokens. | Tailwind: otra capa de configuración y tendencia a valores sueltos. CSS-in-JS: costo en tiempo de ejecución. |
| **Tipografía** | **Nunito 400, 600, 700 y 800 autoalojada** con `@fontsource/nunito` (subconjunto latino), servida desde el mismo origen. | El diseño pide Nunito; sin CDN externo (privacidad y PWA sin conexión). | Descargar los `.woff2` a mano: igual de válido, pero sin actualizaciones. Google Fonts (lo usa el diseño): descartado por la regla. |
| **Íconos** | Componente `Icono` con los **SVG del propio diseño** (trazo 1.8, redondeado), extraídos de los `.dc.html`. | Coinciden exactamente con el diseño, sin dependencia. `react-icons` e `iconoir-react` se retiran con el código viejo. | Lucide o Iconoir: parecidos pero no idénticos. |
| **Primitivas accesibles** | **Componentes propios** para lo simple (botón, insignia, tarjeta, campo, aviso, tabla, avatar). **React Aria Components** (sin estilos) para lo que necesita gestión de foco y teclado: diálogo, panel lateral, menú "Más", pestañas, grupo de botones (Día/Semana/Mes), combobox para buscar paciente y selector. | Diálogos y combobox accesibles son difíciles de hacer bien; React Aria es la referencia de accesibilidad y no impone estilos, así que los estilos siguen siendo del diseño. | Radix: excelente, pero su combobox y tabla son más limitados. Todo propio: alto riesgo de fallas de foco. `<dialog>` nativo: se puede, pero sin combobox ni menús. |
| **Pruebas** | **Vitest + Testing Library + jsdom** para componentes y lógica; **axe-core** en las pruebas de componentes; **Playwright + @axe-core/playwright** para recorridos completos, accesibilidad de página y verificación visual (sección 9). | Vitest usa la misma configuración de Vite; Testing Library prueba como usa la persona; axe detecta fallas AA automáticas. | Jest: duplicaría la configuración. Cypress: más pesado. |
| **Lint** | ESLint con `typescript-eslint` y `eslint-plugin-jsx-a11y`. | Atrapa errores de accesibilidad en el código. | — |

### 2.6 Una sola fuente de verdad visual

- **`packages/tokens/tokens.css`** transcribe del diseño los colores (Marca, Neutros, Estados de la cita), la tipografía (tamaños y pesos de `Main`), los radios (10, 12 y 14 px), los altos (44 px de toque, 48 px en login) y las sombras (`0 1px 2px rgba(0,0,0,0.04)`).
- Cada variable lleva el nombre del diseño (`--rosa-700`, `--texto-secundario`, `--estado-confirmada-fondo`…) y un comentario con su origen.
- **Prueba automática contra la deriva:** una prueba lee `docs/design/panel/Main.dc.html`, extrae los colores de la paleta y comprueba que `tokens.css` tiene exactamente esos valores. Si el diseño cambia, la prueba falla y obliga a actualizar.
- **Ningún componente usa un color o tamaño literal:** una regla de lint (`stylelint` con `declaration-property-value-disallowed-list`) prohíbe hex sueltos fuera de `tokens.css`.
- **`DESIGN.md`** (para impeccable) no repite valores: dice que la autoridad es `docs/design/panel/` y que su implementación es `packages/tokens/tokens.css`.

---

## 3. Sistema de componentes

Todos en `packages/ui`, con su prueba (render, estados, teclado y axe) y su entrada en el catálogo. Entre paréntesis, de dónde sale cada uno.

| Grupo | Componentes |
|---|---|
| Base | `Boton` (principal Rosa 700, secundario, peligro, texto, deshabilitado "Sin teléfono"; con ícono y estado "cargando") (Main); `Icono` (diseño); `Avatar` con iniciales (Rosa 100 y Rosa 800) (Inicio, Solicitudes) |
| Estado | `InsigniaEstadoCita` (5 estados, siempre con texto y punto) (Main); `Insignia` genérica (Con cuenta, Invitado, alergia con ícono) (Main, Pacientes); `Contador` del menú (Main) |
| Contenedores | `Tarjeta` (14 px de radio, sombra del diseño); `TarjetaIndicador` (Inicio); `Seccion` con título 18/700 |
| Formularios | `Campo` (etiqueta 14/700, ayuda, error con ícono y `aria-describedby`); `Entrada`, `Seleccion`, `AreaTexto`, `Busqueda` (44 px, borde #6b7280, foco 2 px Rosa 700 con halo Rosa 100); `Contrasena` con botón "Mostrar contraseña" (Login); `Combobox` de pacientes (React Aria) |
| Avisos | `Aviso` informativo, éxito, advertencia y error, con `role="status"` o `role="alert"` según el caso (Main); `NotaBloqueo` (Movil, Solicitudes) |
| Datos | `Tabla` (encabezado #f9fafb, 14.5 px, filas separadas) con modo **lista apilada** en celular; `Paginacion` ("Mostrando 1 a 8", Anterior/Siguiente) (Pacientes); `ListaHorario` (agenda del día con descansos rayados) (Inicio, Movil); `CuadriculaSemana` (Agenda) |
| Navegación | `MenuLateral` con grupos Principal, Clínica y Administración, activo en Rosa 50/Rosa 800, contador y pie con usuario y "Cerrar sesión" (Inicio); `MenuInferior` de 5 entradas con "Más" (Movil); `HojaMas` (React Aria); `EncabezadoPagina` (título 26/800, subtítulo, acciones); `GrupoVista` Día/Semana/Mes (`aria-pressed`) (Agenda); `Pestanas` (Expediente, Solicitudes) |
| Superposiciones | `Dialogo` (confirmaciones: cancelar cita, desactivar usuario); `PanelLateral` (detalle y formularios en escritorio; pantalla completa en celular, como "Detalle de solicitud" en Movil) |
| Pantallas de estado | `Proximamente`, `SinPermiso` (403), `NoEncontrado` (404), `SinConexion` (Movil), `Vacio` (lista vacía), `ErrorCarga` |
| Clínicos | `Odontograma` con numeración internacional, leyenda de códigos (–, C, R, E, Co, Au), código además del color y diente seleccionable con teclado (Expediente). **Solo presentacional y solo en el catálogo**: el expediente no tiene backend. |

**Catálogo interno:** ruta `/catalogo` con cada componente en todos sus estados, a 360 / 768 / 1440 px. **Solo existe en desarrollo** (`import.meta.env.DEV`): no se incluye en la compilación de producción. Es también la base de la verificación visual (sección 9).

---

## 4. Especificación de movimiento (para tu aprobación)

**Criterio:** panel de uso diario. Lo frecuente es instantáneo; solo se anima lo que explica un cambio de estado o de lugar. Sin biblioteca de animación: transiciones CSS y `@starting-style`.

**Tokens** (en `packages/tokens`):
- `--ease-out: cubic-bezier(0.23, 1, 0.32, 1)`
- `--ease-drawer: cubic-bezier(0.32, 0.72, 0, 1)`
- `--dur-presion: 120ms`
- `--dur-rapida: 150ms`
- `--dur-panel: 220ms`
- `--dur-hoja: 280ms`

| Interacción | ¿Se anima? | Cómo |
|---|---|---|
| Presión de botón (puntero) | Sí, retroalimentación | `transform: scale(0.97)` en `:active`, 120 ms `--ease-out`. Solo con `@media (hover: hover) and (pointer: fine)` y puntero táctil; no con teclado. |
| Hover de botones, filas y elementos del menú | Sí, casi imperceptible | Color y fondo, 150 ms `ease`, solo con `hover: hover`. |
| Cambio de elemento activo del menú, cambio de pantalla, cambio de semana o día en la Agenda, cambio de pestaña, Día/Semana/Mes | **No** | Instantáneo: se usan decenas de veces al día. |
| Foco de teclado (anillo de 3 px Rosa 500) | **No** | Aparece al instante. |
| Diálogo de confirmación | Sí, evita el salto | Entrada: `opacity 0→1` + `scale(0.96→1)` en 200 ms `--ease-out`. Salida: 150 ms. Fondo: opacidad 200 ms. Centrado (sin `transform-origin` en el disparador). |
| Panel lateral (escritorio) | Sí, ubicación | `translateX(16px)→0` + opacidad, 220 ms `--ease-out`. Salida simétrica, 180 ms. |
| Pantalla completa de detalle (celular) | Sí, ubicación | `translateX(100%)→0`, 280 ms `--ease-drawer`; salida por la derecha. |
| Hoja "Más" (celular) | Sí, ubicación | `translateY(100%)→0`, 280 ms `--ease-drawer`; salida hacia abajo. |
| Avisos tras una acción ("Cita confirmada") | Sí, estado | Entrada con opacidad + `translateY(4px)`, 200 ms. Sin salida automática animada: el aviso se queda hasta que se cierra o se navega. |
| Contador del menú al cambiar | **No** | El número cambia; `aria-live` lo anuncia. |
| Botón "cargando" al enviar | Sí, estado | Indicador giratorio lineal de 700 ms por vuelta, solo si la espera pasa de 300 ms. Sin esqueletos con brillo. |
| Carga de pantallas | **No** | Sin secuencias de entrada ni escalonados; el contenido aparece cuando llega. |
| Invitación a instalar (celular) | Sí, una vez | Entrada con opacidad, 200 ms. |

**Con `prefers-reduced-motion: reduce`:**
- Se eliminan las transformaciones (escala y desplazamientos).
- Diálogos, paneles y hojas solo cambian de opacidad en 150 ms.
- El indicador giratorio se sustituye por el texto "Guardando…".

**Prohibido:** `transition: all`, `ease-in`, `scale(0)`, animar ancho, alto o márgenes, y cualquier animación de más de 300 ms fuera de hojas y paneles.

---

## 5. Estructura de carpetas y paquete común

Se usan **npm workspaces** en la raíz del repositorio (un `package.json` raíz que solo declara los espacios de trabajo). `frontend/` conserva su nombre y ruta para no romper documentación ni scripts.

```
Dentissa/
├─ package.json                 # workspaces: ["frontend", "packages/*"]
├─ packages/
│  ├─ tokens/                   # @dentissa/tokens: tokens.css, tokens.ts, fuente Nunito, movimiento
│  │  └─ test/                  # prueba contra docs/design/panel/Main.dc.html
│  └─ ui/                       # @dentissa/ui: componentes React + CSS Modules + pruebas
├─ frontend/                    # panel (React + Vite)
│  ├─ public/                   # manifest, íconos de la PWA
│  ├─ src/
│  │  ├─ app/                   # enrutador, proveedores (Query), guardas por rol, layout
│  │  ├─ api/                   # cliente (authFetch en TS), tipos de los DTO, consultas por recurso
│  │  ├─ features/
│  │  │  ├─ auth/               # inicio de sesión, sesión
│  │  │  ├─ inicio/
│  │  │  ├─ agenda/             # citas
│  │  │  ├─ equipo/             # usuarios
│  │  │  └─ proximamente/
│  │  ├─ catalogo/              # solo en desarrollo
│  │  ├─ pwa/                   # registro del service worker, sin conexión, invitación a instalar
│  │  └─ legacy/                # landing, registro y pantallas viejas hasta retirarlas
│  ├─ sw.ts                     # service worker
│  └─ e2e/                      # Playwright
└─ docs/frontend/               # este plan, especificación de movimiento, informe
```

**Con el sitio en Astro:**
- `@dentissa/tokens` se usa tal cual: CSS puro más la fuente.
- `@dentissa/ui` se puede usar en islas de React (`@astrojs/react`) si el sitio necesita algún componente interactivo.
- Los componentes no dependen del enrutador ni de la API: reciben datos y funciones por propiedades.

---

## 6. Menú por rol

**El backend es la autoridad.** El menú solo oculta; cada ruta tiene además una guarda que muestra "Sin permiso" (403) si el rol no corresponde, y la API rechaza igual.

| Sección (grupo) | Admin | Asistente | Dentista (por ahora) | En esta fase |
|---|---|---|---|---|
| Inicio (Principal) | Sí | Sí | **Sí** | Con datos (sección 10) |
| Agenda (Principal) | Sí | Sí | — *(la matriz dice "solo la propia"; pendiente)* | **Rediseñada** |
| Solicitudes (Principal) | Sí | Sí | — | Próximamente (Fase 2) |
| Pacientes (Principal) | Sí | Sí | — *("los asignados")* | Próximamente |
| Expediente clínico (Clínica) | Sí (lectura) | — | — *("edita")* | Próximamente |
| Servicios (Clínica) | Sí | — | — | Próximamente |
| Ofertas y contenido (Clínica) | Sí | — | — | Próximamente |
| Inventario (Clínica) | Sí | Sí | — | Próximamente |
| Pagos (Clínica) | Sí | Sí | — | Próximamente |
| Reportes (Administración) | Sí | — | — *("los propios")* | Próximamente |
| Equipo y usuarios (Administración) | Sí | — | — | **Rediseñada** |
| Configuración (Administración) | Sí | — | — | Próximamente (Fase 2) |
| Auditoría (Administración) | Sí (lectura) | — | — | Próximamente |

- **Paciente:** no entra al panel (mensaje del diseño de Login).
- **Fuente única de la matriz:** `frontend/src/app/permisos.ts` contiene la matriz y deriva tanto el menú como las guardas. Una prueba compara la tabla con la matriz de `Main.dc.html`, salvo la restricción temporal del dentista, que queda marcada.

---

## 7. Responsive (desde 360 px)

| Ancho | Navegación | Contenido |
|---|---|---|
| ≥ 1024 px | Menú lateral (240–264 px), como en Inicio | Encabezado con búsqueda y acciones; Agenda en semana. |
| 768–1023 px | **[DECIDE]** El diseño no define este ancho. Propuesta: menú inferior como en celular. | Agenda en día por defecto, semana disponible con desplazamiento horizontal (el diseño ya prevé `min-width: 900px` con `overflow-x: auto`). |
| 360–767 px | Menú inferior de 5 (Inicio, Agenda, Solicitudes, Pacientes, Más), 60 px de alto, con `env(safe-area-inset-bottom)` | Encabezado compacto (logo y notificaciones); tablas como listas apiladas; detalle y formularios a pantalla completa; Agenda del día con selector de días (Movil). |

- **"Más"** abre una hoja con el resto de las secciones permitidas y "Cerrar sesión".
- **Sin scroll horizontal de la página** en ningún ancho; solo la cuadrícula semanal se desplaza dentro de su tarjeta.
- **Toques:** todo lo tocable mide al menos 44 × 44 px (diseño y AA).

---

## 8. PWA

**Herramienta:** `vite-plugin-pwa` en modo **`injectManifest`**. El plugin solo genera la lista de archivos con hash de la compilación y el manifiesto; el service worker lo escribimos nosotros, unas 40 líneas en `frontend/sw.ts`, para controlar exactamente qué se guarda.
- **Alternativa:** el modo `generateSW` (Workbox automático) es más cómodo, pero es más fácil que una regla de caché alcance a `/api`.
- **Alternativa:** sin plugin habría que reproducir a mano la lista de archivos con hash.

| Regla | Detalle |
|---|---|
| **Qué se guarda** | Solo los archivos estáticos de la compilación: HTML de la aplicación, JS, CSS, la fuente Nunito, los íconos y el manifiesto. |
| **Qué nunca se guarda** | Nada bajo `/api/`. El service worker ni siquiera intercepta esas peticiones (`if (url.pathname.startsWith('/api/')) return;`). Tampoco respuestas de otros orígenes. |
| **Datos** | TanStack Query solo en memoria. Al perder la conexión se vacía la caché y la aplicación muestra **"Sin conexión con la clínica"** (Movil) en lugar de cualquier dato. Al volver ("Reintentar"), se vuelve a pedir todo. |
| **Navegación sin red** | Las rutas del panel cargan el HTML guardado, que muestra la pantalla sin conexión. |
| **Actualización** | La versión nueva se activa al recargar; aviso discreto "Hay una versión nueva" con botón "Actualizar". |
| **Manifiesto** | `name: "Dentissa — Panel de la clínica"`, `short_name: "Dentissa"`, `lang: "es-MX"`, `display: "standalone"`, `theme_color: #b22f56`, `background_color: #f5f5f5`, `start_url: "/"`, `scope: "/"`. |
| **Íconos** | 192, 512 y 512 *maskable*, más `apple-touch-icon`, generados del ícono del diseño (diente blanco sobre Rosa 700). **El diseño indica que el logo es provisional**: hay que sustituirlos cuando llegue el archivo final (acción manual). |
| **Invitación a instalar** | La tarjeta "Instala Dentissa en tu teléfono" de Movil, en Inicio en celular, con `beforeinstallprompt`. "Ahora no" la oculta (preferencia en `localStorage`, que no es dato de paciente). |
| **Cierre de sesión** | Además de limpiar los tokens: `queryClient.clear()`. No hay datos de pacientes en Cache Storage que borrar, por diseño. |
| **Prueba automática** | Playwright revisa que, tras usar Agenda y Equipo, ninguna entrada de Cache Storage tenga una URL `/api/` ni contenido JSON. |

---

## 9. Verificación visual

**Propuesta:** Playwright (`@playwright/test`), que **no está instalado en el proyecto**. Hay navegadores de Playwright en la caché de tu usuario (`chromium-1255`), pero el paquete se instala en `frontend/` con su versión fijada. **[DECIDE]** apruebas instalarlo.

1. **Capturas del diseño:**
   - Un script abre cada `docs/design/panel/*.dc.html` en Chromium a 360, 768 y 1440 px.
   - **Bloquea `fonts.googleapis.com`** e inyecta la Nunito local, para no depender de la red y usar la misma fuente.
   - Las pantallas del diseño son tableros de 1440 px; a 360 y 768 se comparan contra los tableros de `Movil`.
2. **Capturas del panel** en los mismos anchos, con la API simulada por Playwright (`page.route('/api/**')`) usando **los mismos datos de ejemplo del diseño** (Rosa Aguilar, Ernesto Vega…), para que la comparación sea directa.
3. **Informe:** una página HTML local en `frontend/e2e/informe-visual/`, con el diseño y la implementación lado a lado por pantalla y ancho, más una **diferencia de píxeles** para los componentes del catálogo frente a la sección Componentes de `Main` (botones, insignias, campos, avisos, menú). En pantallas completas, la diferencia de píxeles solo es orientativa (los datos y fechas cambian); la revisión es visual.
4. **Instantáneas de regresión** (`toHaveScreenshot`) del catálogo, para detectar cambios no intencionales en commits posteriores.
5. **Accesibilidad** con `@axe-core/playwright` en cada pantalla y ancho: cero violaciones AA.

---

## 10. Alcance de la fase

### 10.1 Incluido

1. **Paquetes:** `@dentissa/tokens` y `@dentissa/ui` con su catálogo y pruebas.
2. **Estructura general:** layout con menú lateral e inferior, guardas por rol, Próximamente, 403, 404, sin conexión y PWA.
3. **Inicio de sesión** según `Login.dc.html`.
4. **Inicio**, con lo que la API ya permite **[DECIDE]**:
   - "Citas de hoy" y "Agenda de hoy" salen de `GET /api/citas`.
   - Para dentista, de `GET /api/citas/mi-agenda`.
   - "Citas sin confirmar" son las pendientes de hoy y mañana, del mismo listado.
   - "Solicitudes por revisar", "Existencias bajas", "Solicitudes nuevas" y "Por llamar" no tienen API (Fase 2 e inventario): se muestran como tarjetas "Próximamente", sin números inventados.
5. **Agenda (citas)**, rediseñada según `Agenda.dc.html` y `Movil.dc.html`:
   - Vista **Semana** (escritorio) y **Día** (celular).
   - **Horarios y descansos sin escribirlos en el código.** Se deducen de la API: horarios libres de `GET /api/citas/horarios-disponibles` + citas existentes = horarios del día; los huecos entre horarios consecutivos son descansos. Si un día no tiene horarios, está cerrado. Cuando la Fase 2 publique `/api/agenda/configuracion`, se reemplaza por esa fuente.
   - La vista **Mes** queda deshabilitada con "Próximamente" **[DECIDE]**.
   - **Acciones que se conservan:** nueva cita, editar (dentista, fecha, hora, estado, costo extra), confirmar, cancelar con motivo obligatorio.
   - **Formularios sin diseño propio (hueco del diseño, [DECIDE]):** se arman con los componentes aprobados y el patrón de **panel lateral** de "Editar servicio" (Servicios) y "Detalle de solicitud" (Movil). Te muestro capturas antes de cerrarlos.
6. **Equipo y usuarios.** **Hueco del diseño: no hay pantalla diseñada [DECIDE].** Propuesta:
   - Se arma con el patrón de **tabla de Pacientes**: avatar, nombre y correo; teléfono; rol (insignia); estado (Activa o Inactiva); alta; acciones.
   - **Nuevo usuario** en panel lateral (rol, y cédula y especialidad si es dentista).
   - Desactivar y activar con diálogo de confirmación.
   - Mensajes del backend tal cual (p. ej., "No se puede desactivar, eliminar ni cambiar de rol al último administrador activo.").
   - Te muestro capturas antes de cerrarla, o espero un diseño si prefieres.

### 10.2 Fuera de esta fase (Próximamente, con guarda de rol)

Solicitudes, Pacientes, Expediente, Servicios, Ofertas y contenido, Inventario, Pagos, Reportes, Configuración y Auditoría. Llegan cuando exista su backend. El buscador del encabezado y las notificaciones tampoco tienen API; ver preguntas.

---

## 11. Plan de commits

Antes de cada commit:
- `tsc` sin errores;
- ESLint sin errores en código nuevo;
- Vitest en verde;
- `npx impeccable@4.10.0 detect` sobre lo cambiado;
- comparación visual con el diseño (desde el commit 8);
- `npm run build` del frontend.

Además, con cada hito, `critique` y `audit` de impeccable, con la lista de lo que no se aplica y su motivo.

| # | Commit | Contenido |
|---|---|---|
| 1 | `docs: diseño aprobado del panel` | Versiona `docs/design/panel/` (hoy sin seguimiento en git). |
| 2 | `docs: plan de la Fase 3a` | Este documento, con tus ajustes. |
| 3 | `chore(frontend): workspaces, TypeScript, Vitest y lint` | `package.json` raíz con workspaces, TS estricto, Vitest + Testing Library + axe, `typescript-eslint`, `jsx-a11y`, stylelint. Sin cambios visibles. |
| 4 | `docs: PRODUCT.md y DESIGN.md (impeccable)` | `/impeccable init` en modo producto (te hará 1–3 preguntas sobre usuarios y tareas). `DESIGN.md` como índice hacia el diseño, sin valores propios. Telemetría según tu decisión. |
| 5 | `feat(tokens): sistema visual` | `@dentissa/tokens` (colores, tipografía, radios, alturas, sombra, movimiento) + Nunito autoalojada + prueba contra `Main.dc.html`. |
| 6 | `feat(ui): componentes base` | Botón, Icono, Avatar, insignias, contador, tarjetas, campos, avisos. |
| 7 | `feat(ui): navegación, tablas y superposiciones` | Menú lateral e inferior, hoja "Más", encabezado, tabla y lista apilada, paginación, pestañas, grupo de vista, diálogo, panel lateral, combobox, pantallas de estado. |
| 8 | `test(frontend): Playwright y verificación visual` | Tras tu aprobación: capturas del diseño y del catálogo, informe, axe por página. |
| 9 | `feat(ui): catálogo` | Ruta `/catalogo` (solo en desarrollo). **Hito 1: critique y audit del sistema de componentes.** |
| 10 | `feat(ui): odontograma` | Presentacional, en el catálogo. |
| 11 | `feat(panel): estructura, rutas y permisos` | Enrutador, layout, `permisos.ts`, guardas, Próximamente, 403, 404; TanStack Query; `authFetch` en TS. La landing se mueve a `/sitio` sin cambios (si lo apruebas). |
| 12 | `feat(panel): inicio de sesión` | Login rediseñado; rechazo del rol paciente. |
| 13 | `feat(panel): inicio` | Inicio con datos reales y Próximamente. |
| 14 | `feat(panel): agenda` | Semana, día y formularios. **Hito 2: critique y audit de Agenda.** |
| 15 | `feat(panel): equipo y usuarios` | **Hito 3.** |
| 16 | `feat(pwa): instalación y sin conexión` | Manifiesto, íconos, `sw.ts`, pantalla sin conexión, invitación, prueba de Cache Storage. |
| 17 | `chore(frontend): retirar el panel anterior` | Solo con tu confirmación: `Dashboard.jsx` y las pantallas con datos fijos; dependencias `react-icons` e `iconoir-react` si ya no se usan. |
| 18 | `docs: informe de la Fase 3a` | Resultados reales, acciones manuales y riesgos. |

---

## 12. Riesgos

| Riesgo | Mitigación |
|---|---|
| **Huecos del diseño:** Equipo y usuarios, formularios de cita, 768 px, vista Mes | Se arman solo con componentes y patrones aprobados; capturas para tu aprobación antes de cerrar cada uno. |
| **Inconsistencias del diseño:** "nada por debajo de 12.5 px" (Main) frente a 12 px en etiquetas de grupo del menú, contador, menú inferior y estado dentro de la cuadrícula; "Servicios" y "Ofertas y contenido" apuntan a la misma pantalla; Nunito desde Google Fonts | Se implementa lo dibujado (12 px donde está dibujado) y se te reporta para que decidas; Nunito local. |
| **Horarios deducidos de la API** en la Agenda hasta la Fase 2 | Prueba de la deducción con la configuración sembrada; reemplazo directo cuando exista el endpoint. |
| **Tokens en `localStorage`** (P3) | Se conserva el comportamiento actual en esta fase. **BLOQUEANTE del primer despliegue:** token de renovación en cookie `HttpOnly` y token de acceso solo en memoria (requiere backend; decisión 14.4.5). |
| **Skill de taste con hook `Stop`** | No se arma nunca (`.taste-skill/build.json` no se crea); se revisa antes de cada uso. |
| **Telemetría y cuenta de impeccable** | Propuesta: telemetría apagada y sin `login`. |
| **El `CLAUDE.md` de esta rama está desactualizado** (las actualizaciones viven en `fase-2-agenda`) | Ver pregunta 1. |
| **Logo provisional** (según el diseño) en la PWA | Íconos regenerables en cuanto llegue el archivo final. |
| **Dependencias nuevas** (TS, Query, React Aria, Vitest, Playwright, vite-plugin-pwa, fontsource) | Versiones fijadas con al menos dos semanas de antigüedad; `npm audit` en cada commit que instala. |
| **Datos en memoria visibles en un equipo compartido** | Cierre de sesión limpia todo. Cierre por inactividad a los 15 min en el frontend (decisión 14.4.3, con `Estados.dc.html`); mientras solo exista en el navegador, una pestaña manipulada podría evitarlo hasta que el servidor también lo aplique (fase de Configuración). |

---

## 13. Preguntas abiertas

1. **`CLAUDE.md` de esta rama.** Está desactualizado: dice .NET 9, SQL Server y roles viejos; las correcciones están en `fase-2-agenda` (`19dc59d`, `3c399b4`). ¿Traigo aquí esos cambios de `CLAUDE.md` (cherry-pick solo de ese archivo) o esperas a integrar la Fase 2?
2. **Landing y registro actuales:** ¿se conservan sin cambios en `/sitio` hasta que exista el sitio en Astro (propuesta), o se retiran?
3. **Equipo y usuarios** no tiene pantalla diseñada: ¿la armo con los patrones aprobados (propuesta, con capturas para tu visto bueno) o esperas un diseño?
4. **Formularios de cita** (nueva, editar, cancelar) no tienen diseño propio: ¿apruebas el patrón de panel lateral, o pantalla completa en celular?
5. **Ancho de 768–1023 px:** ¿menú inferior (propuesta) o menú lateral compacto?
6. **Inicio:** ¿tarjetas "Próximamente" donde no hay API (propuesta) u ocultarlas?
7. **Encabezado:** el buscador "Buscar paciente o cita" y las notificaciones no tienen API. ¿Se ocultan hasta que existan (propuesta) o se muestran deshabilitados?
8. **"¿Olvidaste tu contraseña?"** no tiene backend. ¿Se oculta (propuesta) o lleva a un texto "Pide a la administración que restablezca tu contraseña"?
9. **Vista Mes** de la Agenda: ¿deshabilitada con Próximamente (propuesta) o se construye?
10. **Tamaños de 12 px** dibujados frente a la regla "nada por debajo de 12.5 px": ¿se respeta el dibujo o la regla?
11. **Playwright:** ¿apruebas instalarlo (sección 9)?
12. **Telemetría de impeccable:** ¿la apago (propuesta)?
13. **Cierre de sesión por inactividad** en equipos compartidos de recepción: ¿se agrega (p. ej., 30 minutos)?
14. **Dentista en la Agenda:** la matriz dice "solo la propia" y la API ya tiene `mi-agenda`. ¿Se queda solo con Inicio, como indicaste, o se le habilita su agenda en esta fase?

---

## 14. Decisiones tomadas (2026-11-23)

### 14.1 Del arquitecto

| # | Decisión |
|---|---|
| 1 | **taste-skill** desinstalada (estaba solo a nivel de usuario, en `~/.claude/skills/design-taste-frontend`); se reinstala con el sitio en Astro. **Telemetría de impeccable apagada** (`~/.impeccable/config.json`). Sin cuenta en línea. |
| 2 | Impeccable en modo Operate, Emil solo movimiento, exclusiones de la sección 0.2, especificación de movimiento (sección 4), tecnologías (sección 2) y **Playwright** (solo desarrollo, solo datos de ejemplo): aprobados. |
| 3 | **No se inventan pantallas.** El arquitecto diseñará Equipo y usuarios, los formularios de cita, los estados vacío, carga y error, y corregirá los tamaños a un mínimo de 12.5 px; llegarán en un zip para `docs/design/panel/`. **Primero:** tokens, componentes, estructura, inicio de sesión y catálogo. Las pantallas faltantes esperan el diseño. **12.5 px es el mínimo desde ahora** (sustituye lo dibujado en 12 px). |
| 4 | Agenda: los descansos se deducen de la API actual en **una sola función marcada como temporal**, con prueba, y **sin confundir con un descanso un hueco que tenga otra causa** (p. ej., un horario ocupado por todos los dentistas). Se reemplaza con la configuración de agenda de la Fase 2. |
| 5 | **Controles sin backend no se muestran** hasta que existan: buscador, notificaciones, "¿Olvidaste tu contraseña?" y tarjetas de Inicio sin datos. Nada de cifras inventadas. |
| 6 | **Servicios** y **Ofertas y contenido** son dos rutas distintas, cada una "Próximamente" por ahora. |
| 7 | Landing y registro actuales **salen de las rutas del panel**, que es solo para el personal y tiene el inicio de sesión como única puerta. Su código se mueve a una carpeta marcada como migrable al sitio en Astro (`frontend/src/migrar-a-sitio/`). No se borra nada; el endpoint de registro del backend se queda. |
| 8 | `CLAUDE.md`: `fase-2-agenda` solo cambiaba documentos (`CLAUDE.md` y `docs/agenda/`); se integró a `main` con merge (`a3107a4`) y esta rama se rebasó. |

### 14.2 Tomadas por mí (preguntas de la sección 13 sin conflicto con `CLAUDE.md`, el diseño ni la seguridad)

| Pregunta | Decisión tomada |
|---|---|
| 5. Ancho de 768–1023 px | **Menú inferior** (como en celular). El menú lateral aparece desde 1024 px. |
| 9. Vista Mes de la Agenda | El diseño solo dibuja el botón, no la vista. **El botón "Mes" no se muestra** hasta que haya diseño (regla 3: no se inventan pantallas; regla 5: no se muestran controles sin función). Día y Semana sí. |
| 14. Dentista en la Agenda | **Solo ve Inicio** (instrucción del arquitecto); Inicio del dentista usa `GET /api/citas/mi-agenda`. |

Respondidas por las decisiones del arquitecto: 1 (`CLAUDE.md`), 2 (landing y registro), 3 y 4 (esperan diseño), 6, 7 y 8 (no se muestran), 10 (mínimo de 12.5 px), 11 (Playwright aprobado) y 12 (telemetría apagada).

**Decisión de negocio 13 (inactividad):** resuelta en la sección 14.4.

### 14.4 Decisiones del arquitecto, segunda ronda (2026-11-23)

| # | Decisión |
|---|---|
| 1 | **Inicio de sesión en celular:** primero el formulario; la presentación se reduce a marca y lema, sin ilustración. Se aplica cuando las dos columnas se apilan (menos de 960 px). El diseño móvil formal llega en el próximo zip. |
| 2 | **Palabra sola al final del párrafo** a 360 px: `text-wrap: pretty` en los párrafos del inicio de sesión, sin cambiar el texto. |
| 3 | **Cierre por inactividad: en esta fase solo el frontend**, y se implementa cuando llegue `Estados.dc.html` (trae el diálogo). 15 minutos fijos para todos los roles en una constante; a los 14 minutos, diálogo de 60 s con "Mantener sesión"; al vencer, cierre de sesión con revocación del token de renovación (`POST /auth/logout`). **Fase de Configuración:** el tiempo lo configura la administración, y el servidor también hace expirar la sesión por inactividad. |
| 4 | **Leyenda del odontograma: 30 px reales** con `border-box`. El diseño la declaraba de 30 px sin `border-box` y se dibujaba de 32; el próximo zip lo corrige. (El Aviso nunca tuvo esa diferencia: su altura coincide con el diseño.) |
| 5 | **BLOQUEANTE del primer despliegue** (no de esta fase): el token de renovación pasa a cookie `HttpOnly` y el token de acceso vive solo en memoria. Requiere cambios de backend. También anotado en `CLAUDE.md`. |

### 14.5 Retiro del panel anterior (decisión del dueño del proyecto, 2026-11-23)

- El panel nuevo es el predeterminado: `index.html` y la raíz `/` abren el inicio de sesión (o el panel si hay sesión). Se quitaron `panel.html`, el desvío de `/panel` en `vite.config.js` y el prefijo `/panel`.
- Se retiraron con `git rm` (recuperables en el historial) las pantallas anteriores: citas, usuarios, comentarios, ofertas, pacientes, servicios, `Dashboard`, `App` y el `main.jsx` anterior, junto con el lint del JavaScript anterior (`frontend/eslint.config.js`, `lint:legacy`, `@eslint/js` y `eslint-plugin-react-refresh`).
- Landing, menú, inicio de sesión de pacientes y registro se movieron con `git mv` a `frontend/src/migrar-a-sitio/` (ver su `LEEME.md`, con la imagen externa de `decogaleria.mx` pendiente).
- **Mientras no existan Agenda y Equipo y usuarios en el panel nuevo, las citas y los usuarios solo se manejan por la API** (`/api/citas`, `/api/usuarios`). El panel no tiene, por ahora, forma de crear, confirmar o cancelar citas, ni de crear o desactivar usuarios.

### 14.7 Agenda con la API actual: filtrado en el cliente y propuesta de backend

**Vista Día: cambio aprobado en `GET /api/citas/horarios-disponibles` (Fase 2, commit 5, 2026-11-24).** El endpoint de compatibilidad ahora delega en el motor de disponibilidad con las **reglas de recepción**: sin horas pasadas y con **60 minutos de anticipación** (minutos de reloj, configurable). Efecto en el panel: en el día de hoy, la vista Día ya no muestra como "Libre" los horarios que ya pasaron ni los que empiezan en menos de 60 minutos; los días futuros no cambian. La deducción temporal de descansos no se afecta (consulta desde mañana). Cuando el panel pase a `GET /api/agenda/disponibilidad` (con servicio y dentistas libres), este endpoint se retira con autorización.

**Hoy.** Agenda (bloque d) e Inicio piden `GET /api/citas`, que devuelve **todas** las citas sin paginar, y filtran por fecha y dentista en el navegador. Con el volumen esperado de una sucursal (un dentista, 6 horarios al día: unas 1 800 citas al año) la respuesta tarda poco al inicio, pero **crece sin límite** y además entrega citas pasadas que la pantalla no muestra.

**Cuándo actuar.** Si `GET /api/citas` pasa de ~2 000 citas o tarda más de 1 s en la red de la clínica, o en cuanto se integre la Fase 2 (lo que ocurra primero).

**Propuesta (sin implementar): un commit chico de backend**, compatible con lo que existe:

- `GET /api/citas?desde=YYYY-MM-DD&hasta=YYYY-MM-DD&dentistaId=N`, todos opcionales. Sin parámetros responde como hoy (compatibilidad).
- Fechas **locales de la clínica**, inclusivas, convertidas a UTC con `IRelojClinica` (igual que `mi-agenda`). Rango máximo de 92 días (el mismo límite de `mi-agenda`); `hasta < desde` → 400.
- Filtro en la consulta (`WHERE inicio >= @desde AND inicio < @hasta+1`), apoyado en el índice de `citas (inicio)` o `(dentista_id, inicio)`; se verifica si existe y, si no, va en una migración nueva.
- Autorización igual que hoy (`Roles.Panel`). DTO sin cambios.
- Pruebas (Testcontainers): rango inclusivo en la zona de la clínica, cita a las 23:30 local, filtro por dentista, 400 por rango inválido, y sin parámetros igual que antes.
- El panel pasa a pedir la semana visible (Agenda) y el día de hoy más las pendientes futuras (Inicio). Commit sugerido: `feat(citas): filtro por fechas y dentista en GET /api/citas`.

### 14.8 Textos que dependen de lo que existe hoy

| Dónde | Texto hoy | Cuándo cambiar | Texto del diseño |
|---|---|---|---|
| Equipo · "Qué puede hacer cada rol" · Dentista (`features/equipo/Equipo.tsx`, constante `ROL`) | "Su inicio, con sus citas del día." | Cuando el dentista tenga su agenda (`mi-agenda`) y el expediente en el panel | "Su agenda y el expediente clínico completo." |
| Equipo · botón de cada fila y título del panel | "Acceso" | Cuando exista el endpoint para editar nombre, correo y rol | "Editar" y "Editar usuario" |
| Inicio · indicador | "Sin confirmar" | Cuando la API tenga el estado del correo de confirmación | "Por llamar" (celular) y "Citas sin confirmar · Pendientes de llamada" |

**Lenguaje sin género (decisión 2026-11-23):** el panel usa formas neutras: "La persona avisó que no puede asistir", "derecha de la persona" (odontograma), "Administración" como nombre del rol, y los mensajes de Equipo hablan de la cuenta ("La cuenta de … quedó inactiva"). Lo que el diseño escribe con género se reporta al arquitecto.

### 14.6 Tareas con fecha

| Fecha | Tarea | Motivo |
|---|---|---|
| **2026-12-07** | Subir `typescript-eslint` de 8.72.1 a **8.74.0** (publicada el 2026-11-23; ese día cumple dos semanas) y cerrar los **7 hallazgos moderados** que quedan en `npm audit`: `minimatch` (DoS por patrones glob, GHSA-g3hv-75wg-m249) y los 6 paquetes de `typescript-eslint` que lo traen. Comprobar antes que su `peerDependencies.typescript` siga aceptando la versión instalada (hoy `>=4.8.4 <6.1.0` con TypeScript 6.0.3). Commit propio, con tipos, lint, compilación, unitarias, Playwright y `npm audit` final. | Solo de desarrollo (lint). No se fuerza con `overrides`: la corrección está en `minimatch` 11 y `typescript-eslint` 8.72 declara `^10.2.2`. |

### 14.3 Versiones elegidas (estables, publicadas antes del 9-nov-2026 y compatibles entre sí)

| Paquete | Versión | Nota |
|---|---|---|
| `typescript` | 6.0.3 | `typescript-eslint` 8.72 exige `< 6.1`; TypeScript 7 aún no es compatible. |
| `typescript-eslint` | 8.72.1 | |
| `vitest` | 5.1.0 | Compatible con Vite 7.3.1 (instalado). |
| `jsdom` | 31.0.0 | |
| `@testing-library/react` / `dom` / `jest-dom` / `user-event` | 16.3.3 / 10.4.2 / 7.0.1 / 14.6.8 | |
| `axe-core` | 4.14.0 | Para las pruebas de componentes. |
| `@tanstack/react-query` | 5.107.1 | |
| `react-aria-components` | 1.22.1 | |
| `@fontsource/nunito` | 5.3.0 | |
| `@playwright/test` / `@axe-core/playwright` | 1.65.0 / 4.14.0 | Solo desarrollo. |
| `eslint-plugin-jsx-a11y` | 6.10.2 | |
| `vite-plugin-pwa` / `workbox-precaching` | 2.0.0 / 7.4.1 | Se instalan en el commit de la PWA. |
