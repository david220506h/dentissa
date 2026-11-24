---
name: "Dentissa — Panel de la clínica"
description: "Panel administrativo de una clínica dental. DERIVADO de packages/tokens/src/tokens.css; la autoridad es docs/design/panel/. No editar a mano."
colors:
  rosa-500: "#d75078"
  rosa-700: "#b22f56"
  rosa-800: "#9b294b"
  rosa-50: "#fdecef"
  rosa-100: "#fce7f3"
  rosa-25: "#fff5f8"
  texto: "#333333"
  texto-secundario: "#555555"
  texto-terciario: "#6b7280"
  borde: "#eeeeee"
  borde-campo: "#6b7280"
  fondo: "#f5f5f5"
  superficie: "#ffffff"
  borde-control: "#d1d5db"
  fondo-encabezado-tabla: "#f9fafb"
  deshabilitado-fondo: "#f3f4f6"
  deshabilitado-borde: "#e5e7eb"
  borde-libre: "#9ca3af"
  borde-peligro: "#fca5a5"
  estado-confirmada-fondo: "#dcfce7"
  estado-confirmada-texto: "#14532d"
  estado-pendiente-fondo: "#fef9c3"
  estado-pendiente-texto: "#854d0e"
  estado-completada-fondo: "#dbeafe"
  estado-completada-texto: "#1e40af"
  estado-cancelada-fondo: "#fee2e2"
  estado-cancelada-texto: "#991b1b"
  estado-no-asistio-fondo: "#f3f4f6"
  estado-no-asistio-texto: "#374151"
  odontograma-borde-azul: "#93c5fd"
  odontograma-borde-amarillo: "#fde047"
  odontograma-borde-rosa: "#f9a8d4"
  velo: "rgba(51, 51, 51, 0.45)"
typography:
  titulo-pagina:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "26px"
    fontWeight: 800
  titulo-tarjeta:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "18px"
    fontWeight: 700
  cuerpo:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "15px"
    fontWeight: 400
  etiqueta:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "14px"
    fontWeight: 700
  tabla:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "14.5px"
    fontWeight: 400
  detalle:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "13px"
    fontWeight: 600
  insignia:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "12.5px"
    fontWeight: 700
  boton:
    fontFamily: "Nunito, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "14.5px"
    fontWeight: 700
rounded:
  4: "4px"
  8: "8px"
  10: "10px"
  12: "12px"
  14: "14px"
  16: "16px"
  20: "20px"
  pildora: "999px"
  circulo: "50%"
spacing:
  2: "2px"
  4: "4px"
  6: "6px"
  8: "8px"
  10: "10px"
  12: "12px"
  14: "14px"
  16: "16px"
  18: "18px"
  20: "20px"
  24: "24px"
  28: "28px"
  32: "32px"
  40: "40px"
  48: "48px"
  56: "56px"
  64: "64px"
components:
  button-primary:
    backgroundColor: "{colors.rosa-700}"
    textColor: "{colors.superficie}"
    typography: "{typography.boton}"
    rounded: "{rounded.10}"
    height: "44px"
    padding: "0 18px"
  button-secondary:
    backgroundColor: "{colors.superficie}"
    textColor: "{colors.texto}"
    typography: "{typography.boton}"
    rounded: "{rounded.10}"
    height: "44px"
  button-danger:
    backgroundColor: "{colors.superficie}"
    textColor: "{colors.estado-cancelada-texto}"
    typography: "{typography.boton}"
    rounded: "{rounded.10}"
    height: "44px"
  button-text:
    textColor: "{colors.rosa-800}"
    typography: "{typography.boton}"
    height: "44px"
  input:
    backgroundColor: "{colors.superficie}"
    textColor: "{colors.texto}"
    rounded: "{rounded.10}"
    height: "44px"
    padding: "0 14px"
  badge:
    typography: "{typography.insignia}"
    rounded: "{rounded.pildora}"
    height: "26px"
    padding: "0 10px"
  nav-item-active:
    backgroundColor: "{colors.rosa-50}"
    textColor: "{colors.rosa-800}"
    rounded: "{rounded.10}"
    height: "44px"
  card:
    backgroundColor: "{colors.superficie}"
    rounded: "{rounded.14}"
    padding: "20px"
---

# Design System: Dentissa — Panel de la clínica

> **Archivo derivado.** Lo genera `packages/tokens/scripts/generar-design-md.ts` a partir de `packages/tokens/src/tokens.css`. La **autoridad visual** es el diseño aprobado en `docs/design/panel/*.dc.html`: `Main.dc.html` es el sistema visual y la matriz de permisos; `Movil.dc.html`, la versión celular. Si algo aquí difiere del diseño, gana el diseño.

## Overview

Panel de trabajo diario (modo *Operate*) para el personal de una clínica dental: administración, asistente y dentista. Claridad y rapidez antes que efecto. Modo claro; una sola acción principal por pantalla, en Rosa 700.

## Colors

Paleta de la clínica: un solo rosa de marca (Rosa 500 para acentos; Rosa 700 en botones para que el texto blanco se lea), neutros y cinco parejas de estado de la cita. Cada valor y su contraste están documentados en `Main.dc.html` (sección Color).

### Named Rules

- El estado nunca depende solo del color: toda insignia lleva texto y los descansos de la agenda van rayados.
- Texto terciario (`texto-terciario`) solo sobre blanco.

## Typography

Nunito 400, 600, 700 y 800, autoalojada. Nada por debajo de 12.5 px. Escala y usos en `Main.dc.html` (sección Tipografía).

## Layout

Menú lateral desde 1024 px; menú inferior de cinco entradas por debajo (`Movil.dc.html`). Desde 360 px, sin desplazamiento horizontal de la página.

## Elevation & Depth

Una sola sombra suave en tarjetas (`0 1px 2px rgba(0,0,0,0.04)`) más bordes de 1 px.

## Shapes

Radios de 10 px (controles), 12 px y 14 px (tarjetas); insignias en píldora.

## Components

Botones, insignias de estado, campos, avisos y elementos del menú según `Main.dc.html` (sección Componentes). Alto mínimo de 44 px en todo lo que se toca.

## Do's and Don'ts

- Usar solo los colores y tamaños de este sistema; nunca valores sueltos.
- No mostrar controles sin backend ni cifras inventadas.
- Movimiento sobrio: lo frecuente es instantáneo (especificación en `docs/frontend/plan-fase-3a.md`, sección 4).
