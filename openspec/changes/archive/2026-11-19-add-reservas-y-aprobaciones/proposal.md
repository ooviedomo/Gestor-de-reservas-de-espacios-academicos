# Proposal

## Why

Con el catálogo y la disponibilidad ya definidos, la institución aún no tiene un flujo formal para solicitar un espacio: las peticiones siguen llegando por correo, se duplican y nadie puede demostrar quién aprobó o canceló una reserva. Este change cierra ese vacío con usuarios, permisos y el ciclo completo de la solicitud.

## What Changes

- Registrar y administrar usuarios con autenticación basada en ASP.NET Core Identity: autorregistro de `Solicitante` y creación de `Coordinador de espacios` y `Administrador del sistema` solo por parte del administrador.
- Definir la matriz de permisos de los tres roles en español y aplicarla a los casos de uso del sistema.
- Crear solicitudes de reserva con estado inicial `PENDIENTE`, revalidando disponibilidad dentro de la transacción y bloqueando las franjas `PENDIENTE`/`APROBADA`.
- Impedir reservas simultáneas sobre el mismo espacio mediante validación de dominio y restricción de exclusión en PostgreSQL.
- Aprobar o rechazar solicitudes registrando actor, fecha y motivo de rechazo obligatorio.
- Cancelar reservas activas con motivo y consultar el historial con filtros por estado, fecha y espacio, respetando los permisos de cada rol.

Fuera de alcance: notificaciones por correo, auditoría separada, reportes de ocupación y reservas recurrentes.

## Capabilities

### New Capabilities

- `usuarios`: Registro, autenticación y administración de las personas que usan el sistema.
- `autorizacion`: Definición y aplicación de los permisos de los tres roles del sistema.
- `reservas`: Creación, validación anti-solapes, cancelación e historial de solicitudes de reserva.
- `aprobaciones`: Transiciones de estado controladas para aprobación y rechazo de solicitudes.

### Modified Capabilities

- Ninguna. `espacios` y `disponibilidad` se consumen sin cambiar sus requisitos.

## Impact

- Proyectos afectados: los cuatro proyectos de `ReservaEspacios`, con especial impacto en `Domain`, `Application` e `Infrastructure`.
- Datos: nuevas tablas `roles`, `users`, `reservations` y enlace con `spaces`; restricción `EXCLUDE` con `btree_gist`.
- Dependencias: ASP.NET Core Identity, JWT opcional para API, EF Core + Npgsql y transacciones con `SERIALIZABLE` o `SELECT ... FOR UPDATE`.
- Interfaz: formularios de acceso, alta de reservas, panel de aprobación e historial en Blazor.
