# Requerimientos — Sistema Integral Clínica Dental (Dentissa)

Nov 18, 2026 · @David

## 1. Resumen y alcance

El sistema es un ecosistema digital para una clínica dental de una sola sucursal: un sitio web comercial con portal del paciente, un panel administrativo web y una app móvil para el personal, todos conectados a un mismo backend y a una base de datos central.

- **Objetivo:** administrar citas, pacientes, expediente clínico, inventario y pagos, y atraer nuevos pacientes con el sitio comercial.
- **Usuarios iniciales:** 1 dentista, 1 asistente (recepción) y 1 administrador, más los pacientes. El modelo soporta más dentistas sin rediseño.
- **Plataformas:** Android, iOS y navegador de escritorio.
- **Contexto:** proyecto universitario, sin límite de MVP; se construirá completo, por fases entregables.
- **Punto de partida:** existe una base previa en React, .NET y PostgreSQL y un documento de requerimientos (Dentissa). Ambos se auditan antes de construir (sección 11).

| Plataforma | Tecnología | Usuarios | Función principal |
| --- | --- | --- | --- |
| Sitio comercial y portal del paciente | Astro | Público y pacientes | Servicios, promociones, galería, reseñas de Google, solicitud de citas, cuenta del paciente |
| Panel administrativo | React + Vite | Asistente, dentista, administrador | Operación completa de la clínica |
| App móvil | React Native + Expo | Personal de la clínica | Agenda, pacientes, inventario y notificaciones en movimiento |
| Backend | ASP.NET Core Web API | Todos los clientes | Lógica de negocio, seguridad y sincronización |
| Base de datos | PostgreSQL | Backend | Fuente única de información |

## 2. Decisiones de negocio confirmadas

Estas decisiones salieron de la entrevista de requerimientos y priman sobre el documento Dentissa donde hay contradicción.

| Tema | Decisión |
| --- | --- |
| Sucursales | Una sola. Sin multi-sucursal. |
| Personal | 1 dentista, 1 asistente, 1 administrador. Todos inician sesión. |
| Datos previos | No se migra nada; la base arranca limpia. |
| Horario | Lunes a sábado, 8:00 a 16:00, con descansos de 11:00 a 12:00 y de 14:00 a 15:00 (propuesta aceptada; confirmar). |
| Cita estándar | 60 minutos. Un servicio más largo ocupa 2 horarios seguidos. |
| Asignación | La clínica asigna dentista y consultorio. El paciente solo elige servicio, fecha y hora. |
| Capacidad | Una cita por dentista activo y por hora; cada dentista usa su propio consultorio. |
| Confirmación | Correo automático primero; el paciente confirma por enlace o desde su portal. Si no responde, recepción llama y confirma ella misma. |
| Cambios | El paciente solo solicita reagendar o cancelar; recepción aprueba. |
| Solicitudes pendientes | Bloquean el horario hasta resolverse; se liberan manualmente o a las 24 h, o 2 h antes de la cita, lo que ocurra primero (propuesta aceptada; confirmar). |
| Anticipación | Mínimo configurable para solicitudes web; recepción puede agendar sin ese límite. |
| Varias sesiones | Recepción programa las sesiones siguientes de un tratamiento, el mismo día u otro. |
| Paciente | Cuenta opcional en un portal dentro del sitio web; también puede solicitar como invitado dejando correo o teléfono. |
| Notificaciones | Correo como canal principal; botón de WhatsApp para que recepción contacte manualmente. |
| Comentarios | Se eliminan del sistema. El sitio mostrará las reseñas de Google. |
| Alcance | Se incluyen expediente clínico completo, inventario y pagos y adeudos. |

## 3. Arquitectura y tecnologías

Los tres clientes hablan con un único backend por API REST sobre HTTPS, y PostgreSQL es la fuente única de verdad.

&#91;embedded content: arquitectura · 3 clientes, 1 backend, 3 almacenes\]

La lógica de negocio vive solo en el backend, para no duplicarla en los clientes. Los archivos clínicos van a almacenamiento privado y el correo es el canal de aviso al paciente. Las reseñas de Google se muestran en el sitio sin pasar por el backend.

**Modo híbrido y trabajo sin conexión**

- La nube es la fuente principal; los clientes locales guardan caché y una cola de operaciones pendientes.
- La app móvil puede consultar la agenda sin conexión. Una cita creada sin conexión queda pendiente y el servidor la valida al sincronizar; si el horario ya se ocupó, se rechaza y se avisa a recepción.
- El panel web no crea citas sin conexión; conserva los formularios en curso y avisa cuando se pierde la conexión.
- El servidor es la autoridad: cada operación lleva un identificador único para no aplicarse dos veces, y los conflictos se resuelven en el servidor.
- El expediente clínico nunca se guarda en el dispositivo móvil.

**Decisiones técnicas propuestas, por validar**

- Monolito modular en .NET con migraciones de EF Core dentro del proyecto, en lugar de una carpeta de migraciones aparte.
- Paquete compartido de tipos y cliente de API, generado desde el OpenAPI del backend, para admin y móvil.
- Docker Compose para el desarrollo local e integración continua.
- Estructura de repositorio: `apps/` (website, admin, mobile), `backend/`, `packages/`, `docs/`.

## 4. Roles y matriz de permisos

Hay cuatro roles y cada uno ve solo lo que le corresponde; el control se aplica en el backend, no solo en las pantallas. La matriz es una propuesta base: las filas marcadas con \* aún requieren validación del cliente.

| Función | Paciente | Asistente | Dentista | Administrador |
| --- | --- | --- | --- | --- |
| Solicitar cita y ver sus citas | Sí (propias) | — | — | — |
| Crear, confirmar, reprogramar y cancelar citas | Solo solicita | Sí | Ver su agenda | Sí |
| Aprobar solicitudes de cambio | — | Sí | — | Sí |
| Programar sesiones de un tratamiento | — | Sí | — | Sí |
| Datos generales y alergias del paciente | Propios | Sí | Sí | Sí |
| Expediente clínico completo | Resumen propio | No | Edita | Solo lectura |
| Pagos y adeudos \* | Ve los propios | Registra | — | Completo |
| Inventario \* | — | Consulta y registra movimientos | Consulta y registra consumo | Completo |
| Servicios, ofertas, galería y preguntas frecuentes | — | — | — | Completo |
| Usuarios, horarios y consultorios | — | — | — | Completo |
| Reportes y estadísticas \* | — | — | Los propios | Completo |
| Bitácora de auditoría | — | — | — | Solo lectura |

La asistente ve solo datos generales y alergias porque el expediente es información sensible. Todo acceso al expediente queda registrado en la auditoría.

## 5. Agenda y reglas de citas

La agenda ofrece seis horarios al día por dentista, de lunes a sábado, y ningún horario puede tener dos citas con el mismo dentista.

**Horarios disponibles por día:** 8:00, 9:00, 10:00, 12:00, 13:00 y 15:00. Los bloques de 11:00 a 12:00 y de 14:00 a 15:00 son descanso. Estos valores se guardan como configuración editable por el administrador, no en el código.

**Reglas:**

1. Cada cita dura 60 minutos. Un servicio que dura más ocupa horarios consecutivos y no puede cruzar un descanso.
2. Un tratamiento de varias sesiones tiene un plan con sesiones, cada una con su propia cita; recepción las programa el mismo día o en otro.
3. La capacidad es una cita por dentista activo y por horario. Con un dentista, una cita por hora; si se agrega otro dentista con su consultorio, se duplica sin cambiar el código.
4. Una solicitud pendiente bloquea su horario. Se libera al rechazarse, manualmente, a las 24 horas o 2 horas antes de la cita, lo que ocurra primero.
5. Las solicitudes web exigen una anticipación mínima configurable. Recepción puede agendar sin ese límite, hasta una hora antes de la cita.
6. Una cita completada no se puede cancelar ni reprogramar. Un servicio inactivo no se asigna a citas nuevas.
7. La base de datos debe impedir por sí misma dos citas activas en el mismo horario y dentista, además de la validación en el backend.

**Estados de la cita:**

| Estado | Significado |
| --- | --- |
| Pendiente | Solicitada o creada, aún sin confirmar. Bloquea el horario. |
| Confirmada | Confirmada por el paciente (correo o portal) o por recepción tras llamada. |
| Completada | La atención se realizó. |
| Cancelada | Cancelada, siempre con motivo. |
| No asistió | El paciente no llegó. Alimenta los reportes (propuesta aceptada). |

Una solicitud de reagendar o cancelar del paciente es un indicador sobre la cita, no un estado nuevo: la cita conserva su horario hasta que recepción la apruebe.

## 6. Flujos de citas

Una solicitud web nace pendiente y bloquea su horario; el correo invita a confirmar y, si no hay respuesta, recepción llama y decide si confirma, reprograma o cancela.

&#91;embedded content: ciclo de la cita · 5 estados, 1 decisión\]

**Solicitud de cambio del paciente.** El paciente pide reagendar o cancelar desde su portal; la cita conserva su horario y recepción llama para aprobar o rechazar.

**Tratamientos de varias sesiones.** El dentista indica el plan y recepción agenda cada sesión, el mismo día o en otro. Cada sesión puede ocupar uno o dos horarios seguidos. Falta confirmar si cada sesión se confirma por separado.

**Invitado y paciente registrado.** El invitado deja correo o teléfono; recepción lo contacta y, si hay solo teléfono, se usa el botón de WhatsApp. Si después se registra, sus solicitudes se vinculan por contacto verificado.

**Cita creada sin conexión en la app.** Queda pendiente; al sincronizar, el servidor la valida y, si el horario ya se ocupó, la rechaza y avisa a recepción.

Cada transición guarda quién la hizo y por qué canal (correo, portal o llamada) en la auditoría.

## 7. Requerimientos funcionales por módulo

Se conservan los módulos del documento Dentissa, ajustados a las decisiones de la sección 2. Los identificadores RF del documento original se indican entre paréntesis.

### 7.1 Cuentas y acceso

- Registro de pacientes con nombre, correo único, contraseña, fecha de nacimiento, teléfono, dirección, género, tipo de sangre, alergias y condiciones médicas (RF-01), con aviso de privacidad y consentimiento expreso.
- Inicio de sesión de paciente, asistente, dentista y administrador, validando que la cuenta esté activa (RF-02, 08, 14). Recuperación de contraseña por correo.
- Alta, edición y desactivación de asistentes y dentistas por el administrador (RF-07, 15). Nunca se borran cuentas; se desactivan.

### 7.2 Citas

- El paciente o invitado solicita cita eligiendo servicio, fecha y hora disponibles; la clínica asigna dentista y consultorio (RF-03, ajustado).
- El paciente invitado deja correo o teléfono; si después crea cuenta, sus solicitudes se vinculan por contacto verificado.
- El paciente confirma desde el correo o su portal; recepción también puede confirmar tras llamar (RF-04, ajustado).
- El paciente solicita reagendar o cancelar; recepción aprueba o rechaza.
- La asistente y el administrador crean, reagendan, cancelan y confirman cualquier cita con motivo (RF-09, 16).
- Calendario diario, semanal y mensual; el dentista ve su propia agenda.
- Historial de cada acción sobre una cita: acción, fecha y hora, usuario y detalle (RF-13).
- Recordatorios de citas próximas por correo.

### 7.3 Pacientes y tratamientos

- Búsqueda y consulta de pacientes según permisos (RF-11, 23).
- Plan de tratamiento con sesiones programadas por recepción (sección 8).

### 7.4 Expediente clínico

Ver sección 8.

### 7.5 Inventario

- Productos e insumos con categoría y existencias.
- Entradas, salidas y ajustes con historial de movimientos.
- Alerta de existencias bajas.
- Consumos asociados a tratamientos, cuando se requiera.

### 7.6 Pagos y adeudos

- Registro de cobros, abonos y saldo pendiente por paciente y por tratamiento.
- Comprobante interno de pago. La facturación fiscal queda fuera del alcance, salvo que se pida después.

### 7.7 Contenido del sitio web

- Administrador gestiona servicios (nombre, descripción, precio, duración), ofertas con vigencia e imagen, galería y preguntas frecuentes (RF-17 a 20).
- Galería e imágenes del sistema se unifican en un solo módulo de medios con categorías (RF-19 y RF-22).
- Mensajes de contacto: ver, marcar como leídos y responder (RF-21).
- Reseñas de Google en el sitio, en lugar de comentarios en citas.
- Ofertas vencidas se desactivan solas. Imágenes JPG, PNG o WEBP de hasta 5 MB.
- SEO, diseño responsive, integración con WhatsApp y mapa de ubicación.

### 7.8 Notificaciones

- Notificaciones internas para asistente y administrador: nuevas solicitudes, cancelaciones, confirmaciones y solicitudes de cambio.
- Notificaciones al paciente por correo: solicitud recibida, confirmación, recordatorio, cambio y cancelación.
- Historial de notificaciones, marcadas como leídas o no.

### 7.9 Reportes y panel

- Panel con indicadores: citas del día, solicitudes pendientes, existencias bajas y saldos.
- Reportes de citas por periodo con totales, completadas, canceladas, pendientes y no asistidas, exportables (RF-24).
- Reportes de inventario y de pagos.

### 7.10 Consultorios

- Registro de consultorios con nombre, descripción y disponibilidad. Cada cita se asigna a un consultorio y se valida su disponibilidad (RF-26).

### 7.11 Aplicación móvil

- Inicio de sesión, agenda, próximas citas, pacientes autorizados, inventario y notificaciones.
- Modo sin conexión solo para consulta de la agenda; una cita creada sin conexión queda pendiente y el servidor la valida al sincronizar. El expediente clínico no se guarda en el dispositivo.

## 8. Expediente clínico

El expediente es el módulo más grande y sensible: reúne el odontograma, las notas por consulta, fotos y radiografías, el consentimiento informado, el plan de tratamiento y las recetas, y solo el dentista lo edita.

| Componente | Qué incluye | Notas de diseño |
| --- | --- | --- |
| Datos generales | Alergias, tipo de sangre y condiciones médicas del registro | Visible para la asistente |
| Odontograma | Estado de cada pieza dental y su historial de cambios | Componente gráfico interactivo en el panel web; versiones por fecha |
| Notas por consulta | Nota clínica ligada a una cita, con fecha y autor | No se pueden borrar; las correcciones se registran como nueva versión |
| Fotografías | Fotos antes, durante y después del tratamiento | Almacenamiento privado, acceso por enlace temporal |
| Radiografías | Archivos de imagen, ligados al paciente y la consulta | Archivos grandes; límite de tamaño y formato por definir |
| Consentimiento informado | Documento con firma del paciente | Firma digital o escaneada; registro de fecha |
| Plan de tratamiento | Sesiones, presupuesto y avance | Se conecta con la agenda de varias sesiones y con pagos |
| Recetas | Medicamentos e indicaciones | Formato imprimible; requisitos formales por validar |

**Acceso:** el dentista edita; el administrador solo lee; la asistente ve solo datos generales y alergias; el paciente ve un resumen. Cada lectura y cambio queda en la bitácora.

**Conservación:** el expediente debe guardarse al menos 5 años desde la última atención, según la NOM-004-SSA3-2012; hay que validarlo con un especialista. El expediente no se guarda sin cifrar en dispositivos móviles.

## 9. Requerimientos no funcionales y reglas de negocio

Los requerimientos no funcionales de Dentissa se mantienen, con objetivos medibles donde el original era vago.

### 9.1 No funcionales

| Área | Requerimiento |
| --- | --- |
| Seguridad | Contraseñas con hash seguro, autenticación por tokens, control de acceso por roles, protección contra inyección SQL y XSS, HTTPS en todo. |
| Usabilidad | Interfaz intuitiva y responsive; validaciones en tiempo real, tanto en pantalla como en backend. |
| Rendimiento | Consultas en menos de 2 segundos; carga de imágenes optimizada automáticamente. |
| Disponibilidad | Objetivo de 99 % de disponibilidad y respaldos automáticos diarios de la base de datos, con prueba periódica de restauración. |
| Mantenibilidad | Código documentado, patrones estándar, base de datos normalizada, configuración sensible en variables de entorno. |
| Escalabilidad | Soportar más dentistas, consultorios y módulos sin reescribir; monolito modular en .NET. |
| Notificaciones | Entre usuarios internos en tiempo casi real; al paciente por correo, con historial de envíos y enlaces directos. |
| Auditoría | Toda acción crítica registra quién, qué, cuándo y detalle; los registros no se pueden modificar ni borrar. |
| Recuperación | Con un solo administrador, la recuperación de contraseña por correo y un procedimiento de emergencia son obligatorios. |

### 9.2 Reglas de negocio

| Regla | Descripción |
| --- | --- |
| RN-01 | Una cita tiene estado Pendiente, Confirmada, Completada, Cancelada o No asistió. |
| RN-02 | Solo se cancelan o reagendan citas pendientes o confirmadas; toda cancelación lleva motivo. |
| RN-03 | Reagendar exige verificar disponibilidad en el nuevo horario. |
| RN-04 | Los comentarios en citas se eliminan; las reseñas son las de Google. |
| RN-05 | Cada rol accede solo a lo que indica la matriz de la sección 4. |
| RN-06 | El correo es único en todo el sistema. |
| RN-07 | Un dentista no puede tener dos citas activas en el mismo horario. La duración base es 60 minutos. |
| RN-08 | Los servicios inactivos no se asignan a citas nuevas; deben tener precio y duración. |
| RN-09 | Las ofertas tienen vigencia; solo las activas y vigentes se muestran, y las vencidas se desactivan solas. |
| RN-10 | Imágenes JPG, PNG o WEBP de máximo 5 MB, siempre categorizadas. |
| RN-11 | Las eliminaciones son lógicas; nada clínico ni financiero se borra físicamente. |

## 10. Seguridad, protección de datos y marco legal

El sistema maneja datos de salud, que son datos personales sensibles, por lo que la seguridad y el cumplimiento legal son requisitos de diseño y no un extra.

**Medidas técnicas**

- Autenticación con tokens de vida corta y renovación segura; contraseñas con hash adecuado.
- Autorización por rol y por recurso: un paciente solo ve lo suyo y un dentista solo lo que le corresponde.
- HTTPS en todos los clientes; validación de datos en frontend y en backend.
- Protección del formulario público de citas con captcha y límite de peticiones.
- Fotos, radiografías y documentos en almacenamiento privado, con enlaces temporales.
- Cifrado de datos sensibles en reposo; el expediente no se guarda en el dispositivo móvil.
- Bitácora de auditoría inmodificable para accesos y cambios importantes, incluida la lectura del expediente.
- Respaldos diarios de la base de datos, con pruebas de restauración y política de conservación.
- Secretos y claves solo en variables de entorno, nunca en el repositorio.
- Recuperación de acceso para el único administrador.

**Marco legal mexicano a validar con un especialista**

- Ley Federal de Protección de Datos Personales en Posesión de los Particulares (versión vigente desde 2025): aviso de privacidad, consentimiento expreso para datos sensibles y derechos de acceso, rectificación, cancelación y oposición.
- NOM-004-SSA3-2012, del expediente clínico: contenido mínimo y conservación.
- NOM-024-SSA3-2012, de sistemas de información de registro electrónico para la salud.

Este documento no es asesoría legal; las normas y su vigencia deben verificarse antes de la implementación.

## 11. Riesgos, cambios respecto a Dentissa y pendientes

La auditoría del código existente sigue pendiente y decidirá cuánto de este alcance ya está construido.

### 11.1 Cambios respecto al documento Dentissa

| Cambio | Motivo |
| --- | --- |
| Se elimina que el paciente elija consultorio | La clínica asigna dentista y consultorio. |
| Se elimina la gestión de comentarios (RF-05, RF-10, RN-04) | Se usarán las reseñas de Google en el sitio. |
| El paciente solicita cambios en lugar de aplicarlos | Recepción aprueba por llamada. |
| La confirmación la hace el paciente o recepción | Correo primero; llamada si no responde. |
| Se agrega el rol Dentista | Todos los usuarios inician sesión. |
| Duración de 60 minutos en lugar de 30 | Decisión de la clínica. |
| Se agrega el estado No asistió | Para medir inasistencias. |
| Se agregan expediente clínico, inventario y pagos | Estaban en la especificación original o se pidieron después. |
| Se unen galería e imágenes del sistema | Eran módulos duplicados. |

### 11.2 Riesgos principales

| Riesgo | Mitigación |
| --- | --- |
| Citas duplicadas por trabajo sin conexión | Offline solo de lectura; lo creado sin conexión queda pendiente y el servidor valida; restricción en la base de datos. |
| Datos clínicos en dispositivos | El expediente no se almacena en el móvil; cifrado en reposo. |
| Cumplimiento legal | Revisión con especialista; aviso de privacidad y consentimiento. |
| Spam en el formulario público | Captcha, límite de peticiones y validación estricta. |
| Un solo administrador | Recuperación de contraseña y procedimiento de emergencia. |
| Alcance grande del expediente | Entrega por fases; odontograma y radiografías al final. |
| Publicación en tiendas móviles | Cuentas de Apple y Google y revisión; empezar los trámites pronto. |
| Reseñas de Google | Verificar el método (API o widget), su costo y sus límites. |

### 11.3 Pendientes por confirmar

- [ ] Descansos de 11:00 a 12:00 y de 14:00 a 15:00; mismo horario el sábado.
- [ ] Tiempo de bloqueo de solicitudes pendientes (propuesta: 24 horas o 2 horas antes de la cita).
- [ ] Anticipación mínima para solicitudes web (propuesta: 2 horas).
- [ ] Matriz de permisos de pagos, inventario y reportes.
- [ ] Alcance exacto de radiografías, recetas, consentimiento y plan de tratamiento.
- [ ] Método para mostrar reseñas de Google.
- [ ] Auditoría del código existente: árbol de carpetas, backend .NET y esquema de PostgreSQL.
- [ ] Revisión legal del expediente y del aviso de privacidad.
