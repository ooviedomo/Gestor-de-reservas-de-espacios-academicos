# Tasks

## 1. Persistencia y permisos

- [ ] 1.1 Crear la migración de `notifications` —con la columna adicional `ultimo_error`— y `audit_logs` con índices por reserva, entidad, actor y fecha, y verificar que la migración se aplica sobre PostgreSQL local (`reservaespacios_test`, según ADR-003).
- [ ] 1.2 Restringir los permisos de `audit_logs` a `INSERT`/`SELECT` para el usuario de aplicación y verificar que un intento de `UPDATE`/`DELETE` falla (`TC-AUD-001-*`).

## 2. Notificaciones

- [ ] 2.1 Generar el registro de notificación dentro de la misma transacción que aprueba, rechaza o cancela una reserva, y verificar `TC-NOT-001-*` con `WebApplicationFactory`, incluido que la creación en `PENDIENTE` no genera notificación.
- [ ] 2.2 Implementar el `IHostedService` que envía correos y actualiza estado/intentos, y verificar `TC-NOT-002-*` con un proveedor SMTP falso en pruebas.
- [ ] 2.3 Verificar `TC-NOT-003-*`: con el SMTP caído, la reserva aprobada permanece `APROBADA` y la notificación sigue `PENDIENTE` con el error en `ultimo_error`; verificar además que el registro outbox se insertó en la misma transacción que el cambio de estado.

## 3. Auditoría

- [ ] 3.1 Cablear el registro de auditoría en los casos de uso ya existentes del change anterior (alta de usuarios, cambio de roles, creación, aprobación, rechazo, cancelación y completado) e insertar en `audit_logs`, y verificar `TC-AUD-001-*` incluidos aprobación y cambio de rol.
- [ ] 3.2 Implementar la consulta filtrable de auditoría con permisos por rol, y verificar `TC-AUD-002-*` con coordinador, administrador y solicitante.
- [ ] 3.3 Crear la vista Blazor de auditoría y verificar con bUnit que el solicitante no ve el enlace de acceso.

## 4. Reportes

- [ ] 4.1 Implementar el reporte de ocupación con `GROUP BY` sobre reservas `APROBADA`/`COMPLETADA`, denominador de días naturales en `America/Bogota` y unión de intervalos, y verificar `TC-REP-001-*` y `TC-REP-003-*` incluidas exclusión de canceladas, filtro por fechas y no duplicación de solapes.
- [ ] 4.2 Aplicar la política de `autorizacion` al endpoint del reporte y verificar `TC-REP-002-*` para coordinador, administrador y solicitante.
- [ ] 4.3 Crear la vista Blazor del reporte y verificar con `WebApplicationFactory` que devuelve horas reservadas y porcentaje de ocupación por espacio.

## 5. Integración y cierre del change

- [ ] 5.1 Ejecutar `dotnet format` y `dotnet test` completos y verificar que no hay errores de analyzers y cobertura ≥ 80%.
- [ ] 5.2 Ejecutar `openspec validate add-notificaciones-auditoria-y-reportes --strict` sin errores y verificar que cada requisito `NOT-*`, `AUD-*` y `REP-*` (incluido `REP-003`) tiene al menos un escenario vinculado a una prueba.
- [ ] 5.3 Verificar que el change aplica `Command`, `Strategy`, `Observer`, `Adapter`, `Decorator`, `Builder` y `Facade` según `design.md` y `docs/adr/ADR-002-patrones-de-diseno.md`, y que cada patrón tiene al menos una prueba que lo exercise.

## Workflow follow-up

- Archivar el change después de la revisión del proyecto.
- Verificar el resultado archivado.
