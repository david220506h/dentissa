# Demo de Dentissa (versión v0.1-demo)

Panel de la clínica funcionando en tu equipo, con **datos inventados**. No es un despliegue: no se usa con datos reales hasta cumplir `docs/requisitos-despliegue.md`.

## Levantar todo con un comando

Requisito: Docker Desktop abierto. Desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File demo\demo.ps1
```

En macOS o Linux: `sh demo/demo.sh`.

El script:

1. Crea `.env.demo` a partir de `.env.demo.example`, con la contraseña de PostgreSQL y la clave JWT generadas al azar. `.env.demo` no se versiona.
2. Construye y levanta `docker-compose.demo.yml`: PostgreSQL 17, la API (aplica las migraciones al arrancar), el panel compilado detrás de nginx y Mailpit.
3. Siembra la base y **muestra en la consola las cuentas del personal con sus contraseñas, generadas en ese momento**. Solo se muestran esa vez: no quedan en el repositorio ni en ningún archivo. Si las pierdes, empieza de cero (abajo).

| Qué | Dónde |
|---|---|
| Panel | http://127.0.0.1:8080 |
| Correos que envía el sistema (Mailpit) | http://127.0.0.1:8026 |

Todo se publica solo en `127.0.0.1`. Los puertos se cambian en `.env.demo` (`PANEL_PUERTO`, `MAILPIT_PUERTO`).

**Empezar de cero** (borra la base de la demo y vuelve a generar las contraseñas):

```powershell
docker compose -f docker-compose.demo.yml --env-file .env.demo down -v
powershell -ExecutionPolicy Bypass -File demo\demo.ps1
```

**Apagar sin borrar:** `docker compose -f docker-compose.demo.yml --env-file .env.demo down`.

El sembrado se niega a correr sobre una base que ya tenga cuentas, pacientes o citas, y solo corre con `Demo:Habilitada=true` (lo define el compose de la demo).

## Datos de la demo

Todo es ficticio; cualquier parecido con personas reales es coincidencia.

| Cuenta | Correo | Rol |
|---|---|---|
| Laura Mendoza | `administracion@clinica-demo.test` | Administración |
| Karla Reyes | `recepcion@clinica-demo.test` | Asistente (recepción) |
| Dra. Mariana Soto | `mariana.soto@clinica-demo.test` | Dentista |
| Dr. Andrés Lara | `andres.lara@clinica-demo.test` | Dentista |
| Dra. Elena Ríos | `elena.rios@clinica-demo.test` | Dentista |

- **30 pacientes** inventados, con correos `@correo.test` y teléfonos `55 5550 …`; 7 con alergias y 9 con cuenta (esas cuentas de paciente tienen contraseña al azar que nadie conoce: los pacientes no entran al panel).
- **6 servicios** (uno de ellos, Tratamiento de conductos, ocupa 2 horarios).
- **Citas de dos semanas** (7 días antes y 7 después del día en que se siembra, de lunes a sábado) en los cinco estados: completadas, no asistió y canceladas en el pasado; pendientes, confirmadas y canceladas en adelante.
- Horario: lunes a sábado, de 8:00 a 16:00, con descansos de 11:00 a 12:00 y de 14:00 a 15:00.

## Guion de 5 minutos

1. **Entrada (30 s).** Abre el panel e inicia sesión como **recepción**. Se ve el Inicio con las citas de hoy. Menciona que la sesión se cierra sola a los 15 minutos sin uso, con aviso al minuto 14.
2. **Agenda (1 min).** Entra a Agenda: la semana con las citas por color de estado y los descansos. Cambia a la vista **Día**. Toca una cita: el diálogo «Cita» muestra los datos y solo las acciones que corresponden a su estado y su hora (confirmar, reagendar, cambiar de dentista, cancelar, completar, no asistió).
3. **Nueva cita (1 min 30 s).** «Nueva cita»: busca a "Ernesto" y aparece su **alergia en rojo**. Elige servicio, fecha y un horario libre (ya sin descansos ni cierres) y deja que la clínica asigne dentista. Muestra el **alta rápida** de una persona nueva sin salir del formulario. Opcional: marca "La persona está en la clínica" para una cita de hoy.
   - **Conflicto (409):** abre «Nueva cita» en dos pestañas con el mismo horario y guarda en ambas. La segunda avisa que el horario ya no está libre, propone horarios libres y conserva lo capturado.
4. **Cancelar y completar (45 s).** Cancela una cita con un motivo neutro ("La persona avisó que no puede asistir"). Abre **Mailpit** (http://127.0.0.1:8026): llegó el correo de cancelación, sin el motivo ni datos de salud. Marca otra cita de hoy como completada.
5. **Pacientes (45 s).** Lista con búsqueda por nombre o teléfono, cuenta, alergias y próxima cita; en el celular (o con la ventana angosta) se ve en tarjetas. Abre una ficha: datos generales y próximas citas, sin expediente clínico.
6. **Permisos (30 s).** Cierra sesión y entra como **dentista**: solo ve Inicio y **sus** pacientes (los que tienen o tuvieron cita con esa persona), sin teléfono ni alta. Entra como **administración**: además ve Equipo y usuarios, donde no se puede desactivar a la última administración activa.

Cierra con: cada acción queda en la bitácora con los datos sensibles enmascarados, y nada clínico ni de citas se borra físicamente.

## Qué queda fuera de la demo

Ver `docs/pendientes.md` para el orden y el detalle.

- Portal del paciente y solicitudes en línea (formulario público, Turnstile, enlaces de los correos).
- Sitio web comercial (Astro).
- Expediente clínico (odontograma, notas, fotos, radiografías, consentimientos, recetas), inventario, pagos y reportes.
- Pantallas de Solicitudes, Configuración (los endpoints de horario y cierres ya existen) y Auditoría; la agenda propia del dentista.
- Todo lo que bloquea un despliegue real: tokens en cookie `HttpOnly`, hosting, correo con dominio, revisión legal y cuenta de administración nueva.

## Notas técnicas

- La API corre en modo `Development` dentro del contenedor (migraciones al arrancar y sin redirección a HTTPS, porque todo es local detrás de nginx). No se publica su puerto: el panel la alcanza en `/api`.
- El correo sale solo a Mailpit (`Correo__Smtp__Host=mailpit`); nada sale a internet.
- El panel guarda los tokens en `localStorage` (válido solo para la demo; bloquea el despliegue real).
