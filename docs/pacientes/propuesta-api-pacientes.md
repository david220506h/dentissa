# Propuesta: API de pacientes para el panel

- **Fecha:** 2026-11-23 · **Estado:** **aprobada por el dueño el 2026-11-24 solo para la demo con datos ficticios** e implementada en la Fase 2b (rama `fase-2b-pacientes`). La revisión legal sigue siendo requisito antes de usar datos reales (`docs/requisitos-despliegue.md`, punto 5).

> **Decisiones provisionales (2026-11-23).** Las tomó el dueño del proyecto; lo legal lo valida un especialista (protección de datos personales y NOM-004-SSA3-2012):
> 1. **"Asignado"** = el paciente tiene al menos una cita no cancelada con ese dentista. Si no, **404**.
> 2. La asistente **no recibe CURP ni dirección**.
> 3. La bitácora de lecturas se conserva **al menos tanto como el expediente**, **sin purga automática**; el plazo exacto lo fija el especialista.
> 4. El dentista **no recibe el teléfono** por ahora.
- **Motivo:** la pantalla Pacientes (tarjetas en celular, tabla en escritorio) y el selector de paciente de los formularios de cita necesitan datos que la API no entrega. Sin alergias no se muestra la lista: una tarjeta sin alergia podría leerse como "sin alergias".

## 0. Lo que existe hoy

| Pieza | Estado |
|---|---|
| Entidad `Paciente` | Tiene `Nombre`, `Apellido`, `Correo`, `Telefono`, `FechaNacimiento`, `Sexo`, `TipoSangre`, `UsuarioId` (cuenta opcional), `Origen`, `ContactoVerificado` y, marcados `[DatoSensible]`, `Curp`, `Direccion`, `Alergias` y `CondicionesMedicas`. Eliminado lógico (`EliminadoEn`). |
| Endpoint | Solo `GET /api/citas/pacientes` (admin y asistente): `id`, `nombre`, `apellido` de todos, sin paginar ni buscar. |
| Bitácora (`auditoria`) | El interceptor registra **escrituras** (`crear`, `modificar`, `eliminar`) y sesiones, y enmascara los `[DatoSensible]` como `"[cambió]"`. **Las lecturas no se registran.** La tabla es de solo inserción (trigger). |
| Matriz (requisitos, sección 4; `Main.dc.html`) | Datos generales y alergias: asistente **sí**, dentista **sí** ("los asignados" en Main), administración **sí**. Expediente completo: asistente **no**, dentista edita, administración solo lectura. |

## 1. Endpoints propuestos

### 1.1 `GET /api/pacientes` — lista paginada con búsqueda

Parámetros: `buscar` (opcional, mínimo 2 caracteres), `pagina` (desde 1), `tamano` (por defecto 25, máximo 100).

Respuesta:

```json
{
  "elementos": [
    {
      "id": 2,
      "nombre": "Ernesto",
      "apellido": "Vega",
      "telefono": "55 5555 0148",
      "cuenta": "con-cuenta",
      "alergias": "Penicilina",
      "ultimaCita": { "fecha": "2026-10-02", "hora": "09:00", "estado": "completada" },
      "proximaCita": { "fecha": "2026-11-18", "hora": "09:00", "estado": "pendiente" }
    }
  ],
  "total": 148,
  "pagina": 1,
  "tamano": 25
}
```

| Campo | Origen | ¿Sensible? | Nota |
|---|---|---|---|
| `id`, `nombre`, `apellido` | `pacientes` | Dato personal | Identifica a la persona. |
| `telefono` | `pacientes.telefono` | Dato personal (contacto) | Recepción llama para confirmar; `null` si no hay. |
| `cuenta` | `usuario_id` no nulo → `con-cuenta`; si no, `invitado` | No | Solo el tipo, nunca el correo ni datos de la cuenta. |
| `alergias` | `pacientes.alergias` | **Sí: dato de salud** (`[DatoSensible]`) | Texto tal como está registrado; `null` si no hay. **Su lectura se audita (sección 3).** |
| `ultimaCita` | Cita más reciente ya iniciada y no cancelada | Dato personal (relación con la clínica) | Fecha y hora locales de la clínica y estado. |
| `proximaCita` | Cita futura más cercana no cancelada | Ídem | Ídem. |

**No se devuelven** en la lista: `correo`, `curp`, `direccion`, `condiciones_medicas`, `fecha_nacimiento`, `sexo`, `tipo_sangre`, nada del expediente, ni hashes o tokens de la cuenta. Siempre DTO, nunca la entidad.

### 1.2 `GET /api/pacientes/{id}` — datos generales

Los mismos campos que la lista más `correo`, `fechaNacimiento` y `sexo` (los "datos generales" de la matriz). Sin `curp`, `direccion` ni `condicionesMedicas`: esos van con el expediente, en su fase, con la autorización del expediente (asistente **no**).

> **Decisión provisional 2:** la asistente no recibe `curp` ni `direccion`. Ningún rol los recibe por estos endpoints; van con el expediente, en su fase.

### 1.3 Reemplazo de `GET /api/citas/pacientes`

El selector de paciente del formulario de cita (bloque f) usa `GET /api/pacientes?buscar=…&tamano=10`. `GET /api/citas/pacientes` se conserva hasta que nada lo use y luego se retira (no se elimina funcionalidad sin aviso).

## 2. Qué rol ve qué (lo aplica el backend)

| Rol | Pacientes visibles | Campos | Expediente |
|---|---|---|---|
| Asistente | Todos (activos) | Los de 1.1 y 1.2, **con alergias** | **Nunca** (403 en cualquier endpoint del expediente). |
| Administración | Todos (activos) | Los de 1.1 y 1.2 | Solo lectura, en su fase. |
| Dentista | **Los asignados** | Los de 1.1 y 1.2 **sin `telefono`** (decisión provisional 4: llega `null`) | Edita, en su fase. |
| Paciente | — | Solo los propios, por el portal (Fase 2, commit 13), no por estos endpoints. | Resumen propio. |

- **"Asignado" no existe hoy en la base.** **Decisión provisional 1:** un paciente está asignado a un dentista si tiene **al menos una cita no cancelada con ese dentista** (pasada o futura). No requiere tabla nueva y coincide con "la clínica asigna dentista" (sección 3 del diseño de la agenda).
- Un dentista que pide `GET /api/pacientes/{id}` de un paciente no asignado recibe **404** (no 403), para no confirmar que existe.
- Los pacientes con eliminado lógico no aparecen.
- Autorización con `[Authorize(Roles = …)]` más el filtro por dentista en la consulta, no después.

## 3. Auditoría de lecturas sensibles y enmascaramiento

**Qué se registra.** Cada respuesta que incluye `alergias` deja una fila en `auditoria`:

| Columna | Lista (`GET /api/pacientes`) | Detalle (`GET /api/pacientes/{id}`) |
|---|---|---|
| `accion` | `lectura_sensible` (nueva constante en `AccionesAuditoria`) | `lectura_sensible` |
| `entidad` | `pacientes` | `pacientes` |
| `entidad_id` | `null` | id del paciente |
| `detalle` | `{"campos":["alergias"],"ids":[2,7,13,…],"pagina":1,"busqueda":true}` | `{"campos":["alergias"]}` |
| `usuario_id`, `usuario_correo`, `ip`, `ocurrido_en` | Como en las escrituras | Ídem |

**Enmascaramiento.**
- **Nunca** se guarda el valor de las alergias ni de ningún `[DatoSensible]`: solo el **nombre del campo** leído y los ids.
- **El texto de búsqueda no se guarda** (puede ser un nombre o un teléfono): solo `"busqueda": true`.
- Las escrituras siguen como hoy: `"[cambió]"` para las columnas sensibles.

**Cómo.** Un filtro de acción (`[AuditarLecturaSensible]`) que, tras una respuesta 200, llama a `IAuditoriaService.RegistrarAsync` con los ids devueltos. Si la bitácora falla, la respuesta falla (500): no se entregan datos sensibles sin registro.

**Volumen.** Con 3 personas y la lista refrescándose al volver a la pestaña, se esperan decenas a cientos de filas por día. Es aceptable; si crece, se agrupan lecturas idénticas del mismo usuario en ventanas de 5 minutos. **Retención (decisión provisional 3):** las filas de lectura se conservan **al menos tanto como el expediente**, **sin purga automática**; el plazo exacto lo fija el especialista. La tabla ya es de solo inserción.

**Consulta.** La pantalla Auditoría (solo administración) podrá filtrar por `lectura_sensible`.

## 4. Paginación y búsqueda

- **Paginación por desplazamiento** (`pagina`, `tamano`): una clínica de una sucursal tendrá cientos o pocos miles de pacientes; es simple y permite "página X de Y". `total` con `COUNT(*)` sobre el mismo filtro. Orden fijo `apellido, nombre, id` para resultados estables.
- **Búsqueda** en nombre, apellido y teléfono:
  - Sin distinguir mayúsculas ni acentos ("nunez" encuentra "Núñez"): extensión `unaccent` de PostgreSQL más `ILIKE`, en una **migración nueva**.
  - Índice de trigramas (`pg_trgm`) sobre `unaccent(nombre || ' ' || apellido)` cuando el volumen lo pida; con miles de filas no hace falta al inicio.
  - Teléfono: se comparan solo los dígitos del término contra el teléfono normalizado (la Fase 2, commit 11, ya normaliza teléfonos).
  - Mínimo 2 caracteres; con menos, 400 con mensaje claro.
- **Última y próxima cita** con subconsultas laterales por paciente de la página (no para toda la tabla), apoyadas en un índice `citas (paciente_id, inicio)`. Se verifica si ya existe; si no, va en la misma migración.
- Límite de tasa en la búsqueda (como el resto de la API) para frenar la enumeración.

## 5. ¿En qué fase encaja?

| Opción | Pros | Contras |
|---|---|---|
| **A. Fase 2b corta, después del commit 8 de la Fase 2** (asignación de dentista) | La definición de "asignado" ya depende de la asignación de la Fase 2. El selector de paciente del formulario de cita (bloque f) la necesita, y el bloque f va después de Agenda (bloque d), que a su vez espera la Fase 2. Commits pequeños, sin mezclar con la agenda. | Pacientes sigue en "Próximamente" hasta entonces. |
| B. Dentro de la Fase 2 | Un solo frente de backend. | La Fase 2 ya tiene 20 commits; mezcla agenda con pacientes. |
| C. Ahora, antes de la Fase 2 | Pacientes disponible antes. | "Asignado" quedaría sin la regla de asignación; habría que rehacer el filtro del dentista. |

**Recomendación: A.** Orden sugerido del backend: Fase 2 (commits 1–8) → **Fase 2b: pacientes** → Fase 2 (9–20) en paralelo con los bloques (d) Agenda y (f) formularios del panel.

### Commits de la Fase 2b (para cuando la apruebes)

1. `feat(db): búsqueda de pacientes` — migración nueva: `unaccent`, índice `citas (paciente_id, inicio)` si falta.
2. `feat(auditoria): lectura de datos sensibles` — acción `lectura_sensible`, filtro de acción y pruebas (no guarda valores ni el texto de búsqueda; si la bitácora falla, la respuesta falla).
3. `feat(pacientes): lista y detalle` — DTOs, servicio, repositorio, autorización por rol y por asignación; pruebas con Testcontainers: cada rol, paciente no asignado (404), eliminado lógico, paginación, búsqueda sin acentos, última y próxima cita.
4. `feat(panel): pantalla Pacientes` — tarjetas en celular y tabla en escritorio, con la alergia siempre visible.

## 6. Para validar contigo o con el especialista

Las cuatro preguntas originales tienen **decisión provisional** (arriba). Quedan para la **revisión legal**:

1. Confirmar "asignado" y el 404 para no asignados.
2. Confirmar que la asistente no necesita CURP ni dirección para su trabajo.
3. Fijar el plazo de conservación de la bitácora de lecturas, y si la lectura de la lista debe registrarse por paciente o por página.
4. Confirmar que el dentista no necesita el teléfono (hoy no llama a pacientes; lo hace recepción).
