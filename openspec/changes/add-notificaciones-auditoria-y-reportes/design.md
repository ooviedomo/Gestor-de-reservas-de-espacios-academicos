# Design

## Context

Los changes previos dejaron creados `espacios`, `disponibilidad`, `usuarios`, `autorizacion`, `reservas`, `aprobaciones` y `completado`. Las transiciones de estado ya existen, por lo que este change no las modifica: solo reacciona a ellas y agrega consultas de auditoría y ocupación. Ver `proposal.md` para la motivación.

Nota de alcance: el plan §1.2 pide "notificar sobre el estado", pero se notifican solo las transiciones a `APROBADA`, `RECHAZADA` y `CANCELADA` (`NOT-001`). La creación en `PENDIENTE` **no** genera correo; el propio spec lo hace explícito para evitar supuestos.

## Goals / Non-Goals

**Goals:**

- Desacoplar el envío de correo de la transacción que cambia el estado de la reserva.
- Hacer cumplible la trazabilidad exigida en el plan mediante un registro inmutable.
- Derivar métricas de ocupación de los datos ya existentes, sin duplicar contadores.

**Non-Goals:**

- Recordatorios automáticos antes de la reserva ni notificaciones push.
- Dashboard de BI, exportación a Excel ni cálculo de sanciones.

## Decisions

- **Patrón outbox con `IHostedService`.** El caso de uso inserta en `notifications` dentro de la misma transacción que cambia el estado, de modo que reserva y notificación se confirman o se rechazan juntas; el envío y sus reintentos los hace un worker independiente, y su fallo nunca revierte la reserva. Alternativa descartada: llamar al SMTP desde el caso de uso, que acopla latencia y fallos externos a la reserva.
- **Columna `ultimo_error` en `notifications`.** El esquema del plan §3.3 no contempla dónde guardar el motivo de un envío fallido; se añade `ultimo_error` para cumplir `NOT-002`. Alternativa descartada: registrar el error solo en logs de aplicación, que no permite inspeccionar la notificación.
- **Reintentos con límite de 5 intentos y error final.** El worker incrementa `intentos` y usa retroceso exponencial simple. Alternativa descartada: reintentos infinitos, que ocultan fallos de configuración.
- **`audit_logs` append-only.** Solo se concede `INSERT` y `SELECT` al rol de aplicación; no existe comando de actualización o borrado en el repositorio. Alternativa descartada: borrado lógico con columna `eliminado`, que permite alterar el histórico.
- **Reporte calculado con `GROUP BY` sobre `reservas`.** Se filtran estados `APROBADA` y `COMPLETADA`; el denominador es `(cierre − apertura) × días naturales del rango` en `America/Bogota` y las horas se suman como unión de intervalos para no duplicar solapes entre una reserva `COMPLETADA` y una `APROBADA`. Alternativa descartada: tabla de contadores que se desincronizaría al cancelar.
- **Supuesto: feriados y días no hábiles fuera del alcance.** El plan §2.3 menciona parámetros de feriados, pero no existe un capability de configuración en estos changes; el reporte usa días naturales y el supuesto queda registrado aquí conforme a §5.2 del plan. Alternativa descartada: inventar una tabla de feriados sin especificación revisada.
- **Acceso al reporte mediante la política de `autorizacion`.** No se crea un mecanismo de permisos paralelo. Alternativa descartada: validar el rol dentro del controlador del reporte.

## Patrones de diseño

Los patrones adoptados en este change (ver `docs/adr/ADR-002-patrones-de-diseno.md`) son:

- **Command**: `EnviarCorreoCommand` se persiste en `notifications` como outbox y se ejecuta diferidamente desde el `IHostedService`.
- **Strategy**: `IRetryBackoff` implementa el retroceso exponencial con límite de 5 intentos (`NOT-002`) e `ICalculoOcupacion` el denominador y la unión de intervalos (`REP-003`).
- **Observer**: los suscriptores `AuditoriaHandler` y `NotificacionOutboxHandler` reaccionan a los eventos de dominio publicados en el change anterior (`AUD-001`, `NOT-001`).
- **Adapter**: `SmtpEmailSender` implementa `IEmailSender`, de modo que el proveedor externo se sustituye sin tocar los casos de uso.
- **Decorator**: `LoggingCommandHandler<T>` registra el resultado de cada envío sin modificar el worker.
- **Builder**: `ReporteBuilder` construye el rango de fechas y el filtro por espacio de `REP-001`.
- **Facade**: `IReservaEspaciosFacade` añade `ConsultarAuditoria` y `GenerarReporte` a la interfaz existente.

## Risks / Trade-offs

- **[Riesgo] SMTP mal configurado deja notificaciones acumuladas.** → Mitigación: el worker registra el último error y el estado `ERROR` tras 5 intentos; la operación de reserva nunca se revierte.
- **[Riesgo] Crecimiento de `audit_logs`.** → Mitigación: índice por `ocurrido_en` y `entidad`; el particionado se difiere hasta que el volumen lo justifique.
- **[Trade-off] Las reservas `PENDIENTE` bloquean la disponibilidad pero no cuentan en el reporte.** → Mitigación: se documenta en la spec REP-001 para evitar interpretaciones erróneas de la ocupación.

## Migration Plan

1. Crear tablas `notifications` y `audit_logs` con sus índices.
2. Restringir permisos de `audit_logs` a `INSERT`/`SELECT` para el usuario de aplicación.
3. Registrar el `IHostedService` de notificaciones en el arranque de ASP.NET Core.
4. Rollback: eliminar el worker, luego las dos tablas; no afecta a reservas ni espacios.
