# Correo en desarrollo (Mailpit)

La Fase 2 (commit 10) envía los correos al paciente desde una **bandeja de salida** sobre la tabla `notificaciones` (diseño de la agenda, sección 7). En desarrollo, todo se envía a **Mailpit**: atrapa los correos y nada sale a internet.

**Proveedor real, credenciales y dominio:** no se configuran aquí. Se deciden antes del primer despliegue (`docs/requisitos-despliegue.md`, punto 4).

## 1. Levantar Mailpit

```bash
docker compose up -d mailpit
```

- SMTP: `127.0.0.1:1025`.
- Bandeja web: <http://127.0.0.1:8025>.

## 2. Configurar el backend (user-secrets)

El repositorio no trae valores de correo (`appsettings.json` solo tiene `Correo:Habilitado: false`). En `backend/`:

```bash
dotnet user-secrets set "Correo:Habilitado" "true"
dotnet user-secrets set "Correo:Remitente" "citas@dentissa.local"
dotnet user-secrets set "Correo:NombreRemitente" "Dentissa (desarrollo)"
dotnet user-secrets set "Correo:Smtp:Host" "127.0.0.1"
dotnet user-secrets set "Correo:Smtp:Puerto" "1025"
dotnet user-secrets set "Correo:Smtp:Seguridad" "Ninguna"
```

- **Opcional, datos de la clínica** para el pie de los correos: `Clinica:Nombre`, `Clinica:Direccion`, `Clinica:Telefono` y `Clinica:WhatsApp`.
- **`Clinica:UrlPublica`:** sin ella, los correos van sin enlaces ("Te llamaremos para confirmarla"). Se llena cuando exista el sitio (decisión 13).
- **Variables de entorno:** también sirven, con doble guion bajo (por ejemplo, `Correo__Smtp__Host`).

**Mailpit no necesita usuario ni contraseña.** Con un proveedor real, `Correo:Smtp:Usuario` y `Correo:Smtp:Password` van **solo** en user-secrets o variables de entorno, nunca en el repositorio.

Si `Correo:Habilitado` es `true` y falta `Correo:Smtp:Host` o `Correo:Remitente`, el backend no arranca.

## 3. Cómo funciona

- **Cuándo se encola:** al confirmar, cancelar o reagendar una cita, al crearla confirmada, al vencer una solicitud en línea y al recibir una solicitud en línea. La fila se escribe en la misma transacción que el cambio, con la plantilla y referencias. Nunca guarda el cuerpo ni tokens.
- **Trabajador:** cada 30 s, o en cuanto se encola algo, reclama lotes (`FOR UPDATE SKIP LOCKED`, arrendamiento de 10 min). Lee el correo vigente del paciente, genera los enlaces si hay `UrlPublica` (solo guarda su hash), renderiza y envía.
- **Reintentos:** 1 min, 5 min, 15 min, 1 h y 6 h. A la 6.ª falla la fila queda `fallida` y llega un aviso interno a recepción.
- **Paciente sin correo:** la fila se descarta y se crea un aviso interno.
- **Filas de más de 48 h:** se descartan, para no enviar correos viejos tras tener el correo apagado.
- **Entrega "al menos una vez":** si el proceso cae entre el envío y el registro, la fila se reintenta y el paciente podría recibir el correo dos veces.
