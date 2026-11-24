# Propuesta: editar usuarios del personal (sin código)

- **Fecha:** 2026-11-23 · **Estado:** propuesta. No se implementa hasta aprobarla.
- **Para qué:** que "Acceso" en Equipo vuelva a ser "Editar" (`docs/frontend/plan-fase-3a.md`, 14.8) con nombre, correo y rol.

## 1. Endpoint

`PUT /api/usuarios/{id}` · solo administración (`[Authorize(Roles = Roles.Admin)]`).

```json
{ "nombre": "Karla", "apellido": "Reyes", "email": "karla@ejemplo.test", "rol": "asistente", "version": 7 }
```

- **Validación (el backend es la autoridad):** nombre y apellido obligatorios (1–100); correo válido y único entre cuentas no eliminadas (409 si existe); rol en `admin | asistente | dentista` (el rol `paciente` no se asigna ni se quita desde aquí: 400).
- **Concurrencia:** `version` (`xmin` de PostgreSQL); si otra persona cambió la cuenta, 409 con mensaje claro y la versión actual.
- **Respuesta:** `UsuarioListaDto` actualizado. Nunca hash ni tokens.
- **No incluye contraseña.** El cambio de contraseña va por "Enviar enlace para nueva contraseña" (requiere correo, Fase 2).

## 2. Reglas

| Regla | Comportamiento |
|---|---|
| **Último administrador** | No se puede **cambiar de rol** a la última cuenta de administración activa (además de no desactivarla, como hoy). 409: "Es la única cuenta de administración activa. Para cambiar su rol, primero debe haber otra cuenta de administración activa." Se valida en la misma transacción que el cambio, con el mismo mecanismo de `UltimoAdministradorException`. |
| **Propio rol** | Nadie cambia su propio rol (400), para no perder acceso por error. |
| **Cambio a dentista** | Crea su ficha de `dentistas` si no existe (como el alta). Pide cédula y especialidad en un segundo paso, no las inventa. |
| **Cambio desde dentista** | Su ficha se **desactiva** (no se borra) si no tiene citas futuras; si las tiene, 409 con cuántas, para reasignarlas antes (sección 3 del diseño de la agenda). |
| **Cambio de correo** | Queda sin verificar hasta que la persona confirme por enlace (cuando exista el correo); mientras tanto el inicio de sesión sigue con el correo anterior. |

## 3. Sesiones

- **Al cambiar de rol o de correo se revocan sus sesiones:** se borra el refresh token (como al desactivar) y el token de acceso vigente deja de servir en su siguiente renovación (máximo 15 min). Para que el cambio de rol tenga efecto inmediato, el backend compara el rol del token con el de la base en las acciones sensibles (o se agrega un "sello de seguridad" a la cuenta que invalida tokens anteriores).
- Cambiar solo el nombre no cierra sesiones.

## 4. Auditoría

- El interceptor ya registra `modificar` en `usuarios` con quién y cuándo; los campos sensibles siguen enmascarados (`[DatoSensible]`).
- Se agrega en `detalle` un resumen explícito para lo que más importa revisar: `{"rol":{"antes":"asistente","despues":"admin"}}` y `{"sesionesRevocadas":true}`. El correo anterior y el nuevo se registran (no son `[DatoSensible]`); si el especialista los considera sensibles, se enmascaran.

## 5. Pruebas previstas (Testcontainers)

Cada regla de la sección 2; 403 para asistente y dentista; 409 por correo duplicado y por versión vieja; revocación de sesión tras cambio de rol (el refresh token deja de funcionar); fila de auditoría sin valores sensibles.

## 6. Panel

"Acceso" vuelve a "Editar" y "Editar usuario" con los campos del diseño (`Equipo.dc.html`): nombre completo, correo y rol (deshabilitado con aviso para la última administración). Commit propio, después del backend.
