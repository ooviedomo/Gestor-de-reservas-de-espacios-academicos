# PLAN_INGENIERIA.md

**Nombre de la solución:** ReservaEspacios  
**Solución y proyectos:** `ReservaEspacios.sln`, `ReservaEspacios.Domain`, `ReservaEspacios.Application`, `ReservaEspacios.Infrastructure`, `ReservaEspacios.Web`  
**Roles del sistema (español):** Solicitante, Coordinador de espacios, Administrador del sistema  
**Códigos normalizados:** `SOLICITANTE`, `COORDINADOR`, `ADMINISTRADOR`  
**Estados de reserva:** `PENDIENTE`, `APROBADA`, `RECHAZADA`, `COMPLETADA`, `CANCELADA`

---

## 1. Análisis del problema

### 1.1 Identificación de la problemática

- Las solicitudes se envían por correo, sin un formato único, lo que genera **duplicidad de peticiones** para el mismo espacio y horario.
- No existe una **fuente única de verdad**: la disponibilidad se consulta en hojas de cálculo que se actualizan tarde o nunca.
- Se presentan **conflictos de horarios** porque no hay validación automática de solapamiento antes de confirmar una reserva.
- No hay claridad sobre **quién aprobó, cuándo y por qué** una reserva fue aceptada o rechazada.
- Las cancelaciones y cambios no quedan registrados, dificultando el histórico y la auditoría.
- La información de capacidad, equipamiento u horarios de atención de los espacios está dispersa.

### 1.2 Necesidades de la institución

**Funcionales**

- Registrar y mantener un catálogo de espacios (laboratorios, auditorios, salas de cómputo).
- Consultar disponibilidad por fecha, franja horaria, tipo de espacio y capacidad.
- Crear, modificar y cancelar reservas con validación automática de solapes.
- Gestionar el flujo de estados: `PENDIENTE → APROBADA / RECHAZADA → COMPLETADA / CANCELADA`.
- Asignar responsables de aprobación (Coordinador de espacios).
- Notificar a los solicitantes sobre el estado de sus reservas.
- Consultar historial y generar reportes básicos de uso.

**No funcionales**

- Integridad y consistencia de datos ante reservas simultáneas.
- Control de acceso basado en los tres roles definidos.
- Trazabilidad de todas las modificaciones.
- Interfaz web usable y adaptable a distintos dispositivos.
- Código mantenible, tipado y cubierto por pruebas automatizadas.

### 1.3 Actores involucrados

| Actor (rol) | Responsabilidad principal |
|---|---|
| **Solicitante** | Consultar disponibilidad y enviar solicitudes de reserva. |
| **Coordinador de espacios** | Aprobar o rechazar solicitudes, resolver conflictos y consultar reportes de su área. |
| **Administrador del sistema** | Gestionar usuarios, roles, espacios y parámetros; consultar la auditoría y los reportes globales. |

> No se definen roles adicionales. Las labores de auditoría y reporte son responsabilidad del **Coordinador de espacios** y del **Administrador del sistema**. El envío de correos se modela como el componente `Notificaciones`, no como un rol.

### 1.4 Alcance y limitaciones

**Dentro del alcance**

- Gestión de espacios académicos y sus atributos.
- Consulta de disponibilidad y prevención de solapes.
- Flujo completo de reserva con aprobación y cancelación.
- Autenticación, tres roles y auditoría básica.
- Notificaciones por correo y reportes de ocupación.

**Fuera del alcance (MVP)**

- Cobros, pagos o facturación por uso de espacios.
- Control de acceso físico, cerraduras o sensores IoT.
- Integración con el sistema de identidad institucional / LDAP / SSO.
- Reservas recurrentes complejas (ej. "todos los martes excepto feriados").
- Aplicación móvil nativa.
- Sanciones automáticas por incumplimiento.
- Analítica predictiva o dashboards avanzados de BI.

---

## 2. Propuesta de solución

### 2.1 Descripción general

Se propone **ReservaEspacios**, una aplicación web centralizada desarrollada íntegramente en C#, con una interfaz Blazor y una API ASP.NET Core que concentra las reglas de negocio, más una base de datos relacional que garantiza integridad. El sistema permitirá pasar de solicitudes informales por correo a un flujo formal, trazable y validado: el **Solicitante** consulta disponibilidad, crea una reserva en estado `PENDIENTE`, el **Coordinador de espacios** la aprueba o rechaza, y el sistema bloquea automáticamente cualquier franja que se solape con reservas activas. El **Administrador del sistema** gestiona la configuración, los espacios y los usuarios.

**Regla de negocio base:** una reserva en estado `PENDIENTE` o `APROBADA` **bloquea** su franja horaria; `RECHAZADA`, `CANCELADA` y `COMPLETADA` no bloquean. Los intervalos son semiabiertos `[inicio, fin)`, por lo que `10:00–11:00` y `11:00–12:00` **no** se consideran solapados.

### 2.2 Funcionalidades prioritarias

| Prioridad | Funcionalidad |
|---|---|
| **P0** | Catálogo de espacios (alta, edición, activación/desactivación). |
| **P0** | Consulta de disponibilidad por fecha y rango horario. |
| **P0** | Creación de reservas con validación anti-solape. |
| **P0** | Autenticación y control de acceso para Solicitante, Coordinador de espacios y Administrador del sistema. El Solicitante se autorregistra; el Coordinador y el Administrador son creados por el Administrador. Se incluye un script de *seed* con un usuario por rol. |
| **P0** | Aprobación / rechazo de reservas por el Coordinador de espacios. |
| **P0** | Cancelación de reservas con registro de motivo. |
| **P1** | Notificaciones por correo de cambio de estado. |
| **P1** | Historial de cambios y auditoría. |
| **P1** | Reporte básico de ocupación por espacio y período. |
| **P2** | Reservas recurrentes e integración con calendario externo. |

### 2.2.1 Reglas de transición de estado

| Estado actual | Acción | Nuevo estado | Quién puede hacerlo |
|---|---|---|---|
| `PENDIENTE` | aprobar | `APROBADA` | Coordinador de espacios, Administrador del sistema |
| `PENDIENTE` | rechazar | `RECHAZADA` | Coordinador de espacios, Administrador del sistema |
| `PENDIENTE` | cancelar | `CANCELADA` | Solicitante (solo la propia), Coordinador, Administrador |
| `APROBADA` | completar | `COMPLETADA` | Coordinador/Administrador (o automáticamente al vencer la fecha) |
| `APROBADA` | cancelar | `CANCELADA` | Solicitante (solo la propia), Coordinador, Administrador |
| `RECHAZADA` / `CANCELADA` / `COMPLETADA` | — | terminal | Nadie |

### 2.3 Organización del sistema en módulos

- **Identidad y acceso**: usuarios, los tres roles, autenticación, autorización.
- **Espacios**: catálogo, capacidad, equipamiento, horarios de atención.
- **Reservas**: creación, modificación, cancelación, validación de solapes.
- **Aprobaciones**: flujo de estados y reglas de transición.
- **Notificaciones**: plantillas, *outbox*, envío de correo, reintentos.
- **Auditoría y reportes**: logs de eventos y consultas de uso.
- **Configuración**: parámetros generales (horarios, feriados, plazos, zona horaria).

### 2.4 Estrategia de desarrollo progresivo

Se trabajará con **cortes verticales**: cada iteración entrega una funcionalidad completa desde la interfaz hasta la base de datos y sus pruebas, en lugar de construir primero toda la capa de datos y luego toda la UI.

Orden previsto:

1. Espacios + consulta de disponibilidad.
2. Creación de reservas con regla anti-solape.
3. Usuarios, tres roles y flujo de aprobación.
4. Notificaciones y auditoría.
5. Reportes y ajustes de experiencia de usuario.

---

## 3. Decisiones de arquitectura y tecnología

### 3.1 Arquitectura seleccionada y justificación

**Monolito modular en capas, desarrollado en C# y expuesto como API REST, con interfaz Blazor.**

Se descarta una arquitectura de microservicios porque el dominio es acotado, el equipo es pequeño y el costo operativo (despliegues, comunicación entre servicios, consistencia distribuida) no se justifica. El monolito mantiene las reglas de reserva en un solo lugar, facilita las transacciones y permite extraer módulos en el futuro si el sistema crece.

Capas:

- **Presentación**: Blazor Web App (C# + Razor components).
- **API / controladores**: endpoints de ASP.NET Core y validación de entrada.
- **Aplicación**: casos de uso (crear reserva, aprobar, cancelar).
- **Dominio**: entidades y reglas de negocio (solapes, estados, permisos).
- **Infraestructura**: Entity Framework Core, PostgreSQL, Identity, envío de correo.

### 3.2 Stack tecnológico propuesto

| Capa | Tecnología |
|---|---|
| Lenguaje / runtime | **C# con .NET 10 (LTS vigente en noviembre de 2026)** |
| Frontend | **Blazor Web App** (interactividad Server/Auto), C# + Razor, con MudBlazor o Bootstrap para UI |
| Backend | **ASP.NET Core 10** (Web API / Minimal APIs) |
| Dominio / aplicación | Bibliotecas de clases en C# sin dependencias de UI ni de datos |
| Base de datos | **PostgreSQL** |
| ORM / migraciones | **Entity Framework Core 10 + Npgsql** |
| Autenticación | **ASP.NET Core Identity + cookies**, RBAC con 3 roles; JWT opcional si se expone API externa |
| Fechas / localización | `DateTimeOffset` en UTC, visualización en zona institucional (ej. `America/Bogota`), cultura `es-ES` |
| Pruebas | **xUnit, Moq/NSubstitute, FluentAssertions, WebApplicationFactory, bUnit, Testcontainers.NET, Playwright** |
| Calidad / CI | Roslyn analyzers, `dotnet format`, GitHub Actions |
| Especificación | OpenSpec (SDD) |

> Se elige .NET 10 por ser la versión de soporte largo plazo (LTS) vigente; .NET 8 ya no recibe soporte en esta fecha.

### 3.3 Estrategia de persistencia de datos

Modelo relacional normalizado con tablas principales:

```
users(id, nombre, email UNIQUE, password_hash, role_id)
roles(id, nombre)                 -- SOLICITANTE, COORDINADOR, ADMINISTRADOR
spaces(id, nombre, tipo, capacidad, ubicacion, hora_apertura, hora_cierre, activo)
reservations(id, space_id, user_id, inicio, fin, estado, proposito,
             aprobado_por, aprobado_en, creado_en)
audit_logs(id, entidad, entidad_id, accion, actor_id, ocurrido_en, metadata)
notifications(id, reservation_id, canal, destinatario, estado, intentos, enviado_en)
```

**Protección contra solapes (doble barrera):**

1. Validación en la capa de dominio/aplicación antes de insertar.
2. Restricción de exclusión en PostgreSQL sobre reservas con estado `PENDIENTE` o `APROBADA`, mapeada desde EF Core con `NpgsqlRange<DateTimeOffset>`:

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE reservations
ADD CONSTRAINT no_overlap
EXCLUDE USING gist (
  space_id WITH =,
  tstzrange(inicio, fin, '[)') WITH &&
) WHERE (estado IN ('PENDIENTE', 'APROBADA'));
```

Además, la creación se ejecuta dentro de una transacción con bloqueo de la fila del espacio (`SELECT ... FOR UPDATE`) o nivel `SERIALIZABLE`, para evitar condiciones de carrera entre dos solicitudes simultáneas.

**Consideraciones adicionales de persistencia:**

- `audit_logs` es **append-only**: solo se inserta; no se actualiza ni se elimina. Esto materializa el requisito de trazabilidad.
- `notifications` funciona como **outbox**: la reserva se confirma primero y un proceso aparte intenta el envío, registrando `intentos`. Un fallo de SMTP nunca deja una reserva a medias.
- Los timestamps se almacenan en UTC con `DateTimeOffset` y se convierten a la zona institucional solo en la presentación.

### 3.4 Organización de componentes

```
ReservaEspacios.sln
  src/
    ReservaEspacios.Domain/            # entidades, value objects, reglas
    ReservaEspacios.Application/       # casos de uso, interfaces, DTOs
    ReservaEspacios.Infrastructure/    # EF Core, Npgsql, Identity, Email
    ReservaEspacios.Web/               # Blazor Web App + endpoints ASP.NET Core
  tests/
    ReservaEspacios.UnitTests/         # xUnit + Moq
    ReservaEspacios.IntegrationTests/  # xUnit + WebApplicationFactory + Testcontainers.NET
    ReservaEspacios.E2ETests/          # Playwright (+ bUnit para componentes)
```

### 3.5 Comparación de decisiones técnicas

| # | Decisión | Opción elegida | Alternativas descartadas | Justificación |
|---|---|---|---|---|
| 1 | Arquitectura general | Monolito modular en capas + Blazor + API REST | Microservicios; serverless | Menor complejidad operativa; transacciones locales; suficiente para el alcance. |
| 2 | Motor de datos | PostgreSQL | MongoDB; SQLite/Excel | Integridad referencial, ACID y soporte nativo de rangos para solapes. |
| 3 | Lenguaje y framework | **C# / .NET 10 + ASP.NET Core + Blazor** | Java Spring Boot; Python Django; Node.js/NestJS | Cumple el requisito del curso, un solo lenguaje para toda la solución, tipado fuerte y ecosistema maduro. |
| 4 | Control de concurrencia | Regla en dominio + restricción de exclusión en BD | Solo validación en UI; solo bloqueo pesimista | La UI nunca es confiable; la BD es la última línea de defensa y evita sobre-reservas. |
| 5 | Autenticación | **ASP.NET Core Identity + cookies + RBAC con Solicitante, Coordinador de espacios y Administrador del sistema** | JWT puro; OAuth institucional; Basic | Integrado en ASP.NET Core, hashing seguro y sin dependencias externas; JWT solo si se abre una API pública. |
| 6 | Acceso a datos | **EF Core 10 + Npgsql** | Dapper; SQL crudo; microlenguajes de otros ecosistemas | Migraciones, change tracking, mapeo de `tstzrange` y reducción de código repetitivo. |

---

## 4. Estrategia de desarrollo

### 4.1 Metodología de trabajo

Metodología **iterativa e incremental**, combinada con **SDD (Spec-Driven Development) usando OpenSpec**. Se recomienda un tablero Kanban o sprints cortos de una semana. Todo cambio relevante pasa por: propuesta → especificación → tareas → implementación → verificación → archivado del *change*.

### 4.2 Etapas de implementación

| Etapa | Entregable principal | Criterio para considerarla terminada |
|---|---|---|
| 0. Planificación | `PLAN_INGENIERIA.md` | Commit independiente realizado y revisado. |
| 1. Especificación | Tres *changes* de OpenSpec validados | `add-espacios-y-disponibilidad`, `add-reservas-y-aprobaciones` y `add-notificaciones-auditoria-y-reportes` con `proposal`, `specs`, `design` y `tasks` sin errores de validación. |
| 2. Núcleo de reservas | Espacios, disponibilidad y creación anti-solape | Pruebas unitarias e integración en verde; prueba de concurrencia sin solapes. |
| 3. Acceso y aprobación | Login, 3 roles y flujo de estados | Alta restringida de roles y *seed* funcionando; pruebas de autorización y de transiciones de estado en verde. |
| 4. Notificaciones y auditoría | Correo vía *outbox* e historial append-only | Envío simulado y reintentos verificados; logs completos por acción sensible. |
| 5. Reportes y pulimiento | Dashboard de ocupación, UX y accesibilidad | E2E con Playwright en verde; cobertura ≥ 80%; revisión de UI en Blazor. |
| 6. Entrega | Documentación, CI y release | CI verde, los tres *changes* archivados y demo funcional. |

### 4.3 Orden de desarrollo y dependencias

```
Usuarios / 3 roles  ──┐
                      ├──▶ Reservas ──▶ Aprobaciones ──▶ Notificaciones ──▶ Reportes
Espacios ─────────────┘         │
                                └──▶ Auditoría (transversal desde la etapa 2)
```

- Los **espacios** deben existir antes de poder reservar.
- Los **usuarios y los tres roles** deben existir antes de saber quién solicita y quién aprueba.
- Las **aprobaciones** dependen de las reservas.
- Las **notificaciones y los reportes** dependen de que el flujo de reservas esté consolidado.

### 4.4 Criterios generales de "terminado"

- Código revisado y sin `TODO` pendientes de la etapa.
- Pruebas automatizadas correspondientes ejecutadas y en verde.
- Especificación OpenSpec actualizada si el comportamiento cambió.
- Commit atómico con mensaje descriptivo que incluya el ID de requisito, por ejemplo `feat(RES-002): validar solapamiento de reservas`.

---

## 5. Estrategia de calidad

### 5.1 Tipos de pruebas

| Tipo | Herramienta | Qué valida |
|---|---|---|
| Unitarias | xUnit + Moq/NSubstitute | Reglas de solapamiento, transiciones de estado, cálculo de disponibilidad. |
| Componentes Blazor | bUnit | Render de componentes y validaciones de formularios. |
| Integración | xUnit + WebApplicationFactory + Testcontainers.NET | Endpoints reales, PostgreSQL, restricción de exclusión, migraciones EF Core. |
| Contrato de API | xUnit + WebApplicationFactory | Formato de request/response y códigos de estado. |
| E2E | Playwright | Flujo completo: consultar → reservar → aprobar → cancelar. Se introduce en la etapa 5. |
| Concurrencia | xUnit + Testcontainers.NET | Dos solicitudes simultáneas sobre la misma franja horaria. |
| Seguridad | xUnit + Identity | Endpoints protegidos y permisos correctos para Solicitante, Coordinador de espacios y Administrador del sistema. |
| Regresión | GitHub Actions | Ejecución automática de `dotnet test` ante cada cambio. |

> `WebApplicationFactory` se usa desde la etapa 2 por su rapidez; Playwright se reserva para cuando la UI Blazor ya esté estable.

### 5.2 Riesgos identificados y mitigación

| # | Riesgo | Impacto | Mitigación |
|---|---|---|---|
| 1 | Reservas simultáneas generen solapes | Alto | Validación en dominio + restricción `EXCLUDE` en PostgreSQL + prueba de concurrencia. |
| 2 | Reglas de negocio ambiguas (recurrencias, feriados, mantenciones) | Alto | Levantamiento con la coordinación, registro de supuestos en `design.md` y versionado de specs. |
| 3 | Datos sucios o incompletos migrados desde correos/Excel | Medio | Script de importación con validación, revisión manual y modo solo lectura inicial (*dry-run*). |
| 4 | Fallo del servicio de correo | Medio | Patrón *outbox* con reintentos y logging; el fallo no bloquea la creación de la reserva. |
| 5 | Curva de aprendizaje de OpenSpec/SDD y de Blazor | Medio | Plantillas, primer *change* pequeño, validación temprana y pair review. |
| 6 | Crecimiento descontrolado del alcance | Medio | Priorización P0/P1/P2 y todo cambio pasa por un nuevo *proposal*. |
| 7 | Asignación incorrecta de permisos entre los tres roles (**Solicitante**, **Coordinador de espacios**, **Administrador del sistema**) | Alto | Identity + RBAC centralizado, guards en endpoints y pruebas de autorización por rol. |
| 8 | Entorno sin Git inicializado | Bajo | `git init` y commit base del plan antes de cualquier otra herramienta. |

### 5.3 Verificación del cumplimiento de requisitos

Se construirá una **matriz de trazabilidad**:

| ID | Requisito | Spec (OpenSpec) | Prueba |
|---|---|---|---|
| REQ-01 | Consultar disponibilidad | `specs/spaces/spec.md` | `TC-01`, `E2E-01` |
| REQ-02 | Evitar solapes | `specs/reservations/spec.md` | `TC-02`, `TI-02`, `CC-01` |
| REQ-03 | Aprobar/rechazar según la matriz de estados | `specs/approvals/spec.md` | `TC-03`, `E2E-02` |
| REQ-04 | Restringir acceso según los 3 roles | `specs/auth/spec.md` | `SEC-01`, `SEC-02` |
| REQ-05 | Registrar auditoría y notificar cambios | `specs/audit/spec.md`, `specs/notifications/spec.md` | `TI-03`, `TI-04` |

Los IDs `REQ-xx` se referenciarán en comentarios de código, en los nombres de las pruebas y en los mensajes de commit. La CI ejecutará `dotnet format`, validación de specs, `dotnet test` y Playwright; el merge se permite solo con todo en verde y revisión de otro compañero.

---

## 6. Uso de SDD e inteligencia artificial

### 6.1 Cómo se utilizará OpenSpec

Una vez realizado el commit del plan:

1. Inicializar OpenSpec en la solución (`openspec init`).
2. Crear tres *changes* alineados con las etapas:
   - `add-espacios-y-disponibilidad`
   - `add-reservas-y-aprobaciones`
   - `add-notificaciones-auditoria-y-reportes`
3. Para cada uno producir:
   - `proposal.md`: motivación, alcance y fuera de alcance (traído de la sección 1).
   - `specs/*.md` con requisitos y escenarios `Given / When / Then`, incluyendo la matriz de estados y la regla de `PENDIENTE` que bloquea.
   - `design.md`: decisiones de arquitectura y tecnología de la sección 3.
   - `tasks.md`: checklist derivado de la sección 4.
4. Validar cada *change* con el comando de validación de OpenSpec.
5. Implementar tarea por tarea, ejecutando `dotnet test` tras cada una.
6. Archivar cada *change* al cerrar su etapa correspondiente.

### 6.2 Transformación del plan de ingeniería en especificaciones

- Cada **necesidad funcional** del plan se convierte en un `Requirement` con ID propio.
- Cada **regla de negocio** (no solapes, transiciones de estado, permisos por rol) se convierte en un `Scenario` con formato Given/When/Then.
- Cada **decisión técnica** de la sección 3 se copia al `design.md`.
- Cada **tarea** del `tasks.md` se vincula a un requisito y a una prueba.
- Los IDs de requisito se usan como hilo conductor entre spec, código, prueba y commit.

### 6.3 Actividades delegadas a la IA

- Generar borradores de `specs` a partir del `PLAN_INGENIERIA.md`.
- Proponer escenarios de prueba faltantes leyendo los requisitos y la matriz de estados.
- Crear código boilerplate en C#: entidades, DTOs, casos de uso, controladores, repositorios EF Core y componentes Razor.
- Sugerir implementaciones de reglas de negocio y refactorizaciones.
- Redactar documentación y mensajes de commit.
- Detectar inconsistencias entre specs y código.

### 6.4 Verificación del código generado

- Revisión humana del `diff` antes de aceptar cualquier cambio.
- Ejecución de `dotnet build`, `dotnet format`, pruebas unitarias, integración, concurrencia y E2E.
- Validación de OpenSpec y comparación con la matriz de trazabilidad.
- Revisión de migraciones de EF Core y de que no se hayan introducido dependencias innecesarias.
- Verificación manual del flujo completo en entorno local.
- Registrar en la memoria final un anexo breve con los prompts representativos y la verificación aplicada a cada salida de IA.

### 6.5 Decisiones que siguen siendo responsabilidad del estudiante

- Definir y validar el alcance y las limitaciones con la institución.
- Aprobar o rechazar reglas de negocio, incluida la matriz de estados y la regla de `PENDIENTE` que bloquea.
- Mantener los **tres roles en español — Solicitante, Coordinador de espacios y Administrador del sistema —** y sus permisos bajo su responsabilidad.
- Elegir y aceptar la arquitectura y el stack tecnológico.
- Priorizar funcionalidades y decidir qué entra en cada entrega.
- Aprobar las especificaciones generadas por IA antes de implementar.
- Interpretar fallos en pruebas y decidir correcciones.
- Realizar el commit independiente del plan y decidir cuándo archivar cada *change*.
- **No editar `PLAN_INGENIERIA.md` después del commit**: las decisiones nuevas se registran en `docs/adr/` o en el `design.md` del *change* correspondiente.
