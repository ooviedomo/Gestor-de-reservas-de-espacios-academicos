# Design

## Context

Los changes previos dejaron creados `espacios`, `disponibilidad`, `usuarios`, `autorizacion`, `reservas` y `aprobaciones`. Las transiciones de estado ya existen, por lo que este change no las modifica: solo reacciona a ellas y agrega consultas de auditoría y ocupación. Ver `proposal.md` para la motivación.

## Goals / Non-Goals

**Goals:**

- Desacoplar el envío de correo de la transacción que cambia el estado de la reserva.
- Hacer cumplible la trazabilidad exigida en el plan mediante un registro inmutable.
- Derivar métricas de ocupación de los datos ya existentes, sin duplicar contadores.

**Non-Goals:**

- Recordatorios automáticos antes de la reserva ni notificaciones push.
- Dashboard de BI, exportación a Excel ni cálculo de sanciones.

## Decisions

- **Patrón outbox con `IHostedService`.** El caso de uso inserta en `notifications` dentro de la misma transacción que cambia el estado; un worker independiente lee pendientes y envía. Alternativa descartada: llamar al SMTP desde el caso de uso, que acopla latencia y fallos externos a la reserva.
- **Reintentos con límite de 5 intentos y error final.** El worker incrementa `intentos` y usa retroceso exponencial simple. Alternativa descartada: reintentos infinitos, que ocultan fallos de configuración.
- **`audit_logs` append-only.** Solo se concede `INSERT` y `SELECT` al rol de aplicación; no existe comando de actualización o borrado en el repositorio. Alternativa descartada: borrado lógico con columna `eliminado`, que permite alterar el histórico.
- **Reporte calculado con `GROUP BY` sobre `reservations`.** Se filtran estados `APROBADA` y `COMPLETADA` y se divide entre el horario de atención del espacio. Alternativa descartada: tabla de contadores que se desincronizaría al cancelar.
- **Acceso al reporte mediante la política de `autorizacion`.** No se crea un mecanismo de permisos paralelo. Alternativa descartada: validar el rol dentro del controlador del reporte.

## Risks / Trade-offs

- **[Riesgo] SMTP mal configurado deja notificaciones acumuladas.** → Mitigación: el worker registra el último error y el estado `ERROR` tras 5 intentos; la operación de reserva nunca se revierte.
- **[Riesgo] Crecimiento de `audit_logs`.** → Mitigación: índice por `ocurrido_en` y `entidad`; el particionado se difiere hasta que el volumen lo justifique.
- **[Trade-off] Las reservas `PENDIENTE` bloquean la disponibilidad pero no cuentan en el reporte.** → Mitigación: se documenta en la spec REP-001 para evitar interpretaciones erróneas de la ocupación.

## Migration Plan

1. Crear tablas `notifications` y `audit_logs` con sus índices.
2. Restringir permisos de `audit_logs` a `INSERT`/`SELECT` para el usuario de aplicación.
3. Registrar el `IHostedService` de notificaciones en el arranque de ASP.NET Core.
4. Rollback: eliminar el worker, luego las dos tablas; no afecta a reservas ni espacios.
