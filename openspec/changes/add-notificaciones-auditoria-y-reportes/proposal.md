# Proposal

## Why

Con reservas y aprobaciones ya operativas, los involucrados aún no reciben confirmación de los cambios de estado y la institución no cuenta con un registro inmutable ni con datos agregados de ocupación. Sin esto, la trazabilidad exigida en el plan queda incompleta y las decisiones no pueden auditarse ni medirse.

## What Changes

- Registrar cada cambio de estado de una reserva como un mensaje pendiente en un patrón *outbox* y enviarlo por correo con reintentos, sin revertir la reserva si el SMTP falla.
- Mantener un registro de auditoría de solo inserción para las acciones sensibles (alta de usuarios, cambio de roles, creación/aprobación/rechazo/cancelación de reservas), con actor, entidad, acción y marca de tiempo.
- Publicar un reporte de ocupación por espacio y período, accesible solo para `Coordinador de espacios` y `Administrador del sistema`.

Fuera de alcance: notificaciones push, recordatorios automáticos antes de la reserva, dashboards de BI y exportación a hojas de cálculo.

## Capabilities

### New Capabilities

- `notificaciones`: Generación, almacenamiento y envío con reintentos de mensajes de correo asociados a cambios de reserva.
- `auditoria`: Registro inmutable de acciones sensibles realizadas por los usuarios del sistema.
- `reportes`: Consulta agregada de ocupación de espacios por período.

### Modified Capabilities

- Ninguna. Los cambios de estado existentes no se alteran; este change solo reacciona a ellos.

## Impact

- Proyectos afectados: `Application`, `Infrastructure` y `Web`, con un nuevo servicio de correo y un worker de reintentos.
- Datos: tablas `notifications` y `audit_logs`; índices por reserva, entidad y fecha.
- Dependencias: proveedor SMTP configurable, `IHostedService`/worker en ASP.NET Core y EF Core para consultas agregadas.
- Seguridad: el reporte se restringe mediante la matriz de permisos definida en `autorizacion`.
