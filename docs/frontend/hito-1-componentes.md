# Hito 1 de la Fase 3a: sistema de componentes y catálogo

> **Fecha:** 2026-11-23 · **Rama:** `fase-3a-panel-base`
> **Evaluado:** `@dentissa/tokens`, `@dentissa/ui` y su catálogo (`frontend/catalogo.html`, solo desarrollo).

## Evidencia

| Verificación | Resultado |
|---|---|
| Estilos calculados de 22 componentes frente a `Main.dc.html` (Playwright, mismo navegador y Nunito local) | 22 de 22 iguales. Única diferencia aprobada: contador a 12.5 px. |
| axe, WCAG 2.2 AA con contraste real, a 360, 768 y 1440 px | 0 violaciones. |
| Desborde horizontal, texto < 12.5 px, controles < 44 px | Ninguno. |
| Detector de Impeccable sobre la página real (1440×900 y 390×844) | 0 antipatrones; 1 aviso informativo. |
| Pruebas | 71 unitarias (Vitest) y 41 de Playwright, en verde. |
| **Audit** de Impeccable | **17/20 (bueno)**: accesibilidad 3, rendimiento 4, temas 3, responsive 3, integridad 4. |
| **Critique** de Impeccable (revisión de diseño en un subagente aislado) | **28/36** (heurística 10 n/a: biblioteca de componentes). |

## Aplicado (coherente con el diseño)

| Hallazgo | Corrección |
|---|---|
| El botón "cargando" se veía gris, igual que uno deshabilitado (critique, P1). El diseño no define ese estado. | Conserva el color de su variante; solo bloquea la acción (`aria-busy`). |
| GrupoVista (Día/Semana) se estiraba a todo el ancho en un contenedor en columna (critique, P2). | Compacto como en Agenda (`align-self: flex-start; width: max-content`). Restaura el diseño. |
| Contadores pegados al nombre en el lector de pantalla: "Solicitudes3 por revisar" (audit, P2). | Texto oculto separado: "Solicitudes, 3 por revisar". Las pestañas dicen qué cuentan ("Pendientes, 5 solicitudes"). |
| La tabla se anunciaba tres veces con el mismo nombre (audit, P3). | Un solo nombre: el `caption` de la tabla y la región desplazable. |
| "Ver ficha" repetido sin decir de quién (critique, P2). | En el ejemplo del catálogo, `aria-label="Ver ficha de Rosa Aguilar"`. Lo mismo deberán hacer las pantallas. |
| Antes del hito (Playwright): tabla que desbordaba a 360 px y etiquetas de grupo sin contraste sobre el fondo gris. | Commit `4c5e380`. |

## No aplicado, con su motivo

| Hallazgo | Motivo |
|---|---|
| Alergias fuera de la vista en celular por el desplazamiento horizontal de la tabla (critique, P1). | La tabla de Pacientes en celular no está diseñada; el diseño usa desplazamiento horizontal. **Decisión para el arquitecto:** priorizar columnas (Paciente, Alergias, acción) o una lista en celular. |
| El rojo de "Cancelada" y el de la alergia son la misma pareja (critique, P3). | **Contra el diseño.** Lo distingue el ícono de alerta. Decisión del arquitecto si la alergia debe tener un tratamiento propio. |
| Tamaños en px: la preferencia de tamaño de letra del navegador no escala el texto (el zoom sí) (audit, P3). | **Contra el diseño**, que especifica px. Cumple WCAG 1.4.4 por zoom. |
| Palabra sola en la última línea del texto del catálogo a 390 px; `text-wrap: pretty` (detector, aviso). | No está en el diseño; además es solo la página interna. |
| Atajos de teclado para "Confirmar cita" y acciones en lote (critique, heurística 7). | Pertenece a las pantallas de Solicitudes y Agenda, que aún no existen; se propondrá con su diseño. |
| Botón "Cerrar aviso" que Main no muestra (critique, menor). | Es una propiedad opcional del componente; ninguna pantalla la usa todavía. **Confirmar con el arquitecto.** |
| Hoja de "Más" en el celular (critique, menor). | El diseño no la dibuja: "Más" lleva a una página con los mismos grupos del menú lateral. **Confirmar con el arquitecto.** |
| Estado "con foco" de los campos visible de forma estática en el catálogo (critique, menor). | Se verifica con teclado; forzarlo exigiría una clase solo para el catálogo. Pendiente para el próximo hito. |
| Formato de la paginación con total ("de N") (critique, menor). | El diseño muestra "Mostrando 1 a 8" sin total; se conserva. |
