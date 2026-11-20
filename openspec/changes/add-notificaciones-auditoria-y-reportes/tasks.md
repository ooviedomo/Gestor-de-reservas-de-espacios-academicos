# Tasks

## 1. Persistencia y permisos

- [ ] 1.1 Crear la migración de `notifications` y `audit_logs` con índices por reserva, entidad, actor y fecha, y verificar que la migración se aplica sobre PostgreSQL con Testcontainers.
- [ ] 1.2 Restringir los permisos de `audit_logs` a `INSERT`/`SELECT` para el usuario de aplicación y verificar que un intento de `UPDATE`/`DELETE` falla (`TC-AUD-001-*`).

## 2. Notificaciones

- [ ] 2.1 Generar el registro de notificación dentro de la misma transacción que aprueba, rechaza o cancela una reserva, y verificar `TC-NOT-001-*` con `WebApplicationFactory`.
- [ ] 2.2 Implementar el `IHostedService` que envía correos y actualiza estado/intentos, y verificar `TC-NOT-002-*` con un proveedor SMTP falso en pruebas.
- [ ] 2.3 Verificar `TC-NOT-003-*`: con el SMTP caído, la reserva aprobada permanece `APROBADA` y la notificación sigue `PENDIENTE`.

## 3. Auditoría

- [ ] 3.1 Registrar acciones sensibles de usuarios, roles y reservas en `audit_logs`, y verificar `TC-AUD-001-*` incluidos aprobación y cambio de rol.
- [ ] 3.2 Implementar la consulta filtrable de auditoría con permisos por rol, y verificar `TC-AUD-002-*` con coordinador, administrador y solicitante.
- [ ] 3.3 Crear la vista Blazor de auditoría y verificar con bUnit que el solicitante no ve el enlace de acceso.

## 4. Reportes

- [ ] 4.1 Implementar el reporte de ocupación con `GROUP BY` sobre reservas `APROBADA`/`COMPLETADA` y horarios de atención, y verificar `TC-REP-001-*` incluidas exclusión de canceladas y filtro por fechas.
- [ ] 4.2 Aplicar la política de `autorizacion` al endpoint del reporte y verificar `TC-REP-002-*` para coordinador, administrador y solicitante.
- [ ] 4.3 Crear la vista Blazor del reporte y verificar con `WebApplicationFactory` que devuelve horas reservadas y porcentaje de ocupación por espacio.

## 5. Integración y cierre del change

- [ ] 5.1 Ejecutar `dotnet format` y `dotnet test` completos y verificar que no hay errores de analyzers y cobertura ≥ 80%.
- [ ] 5.2 Ejecutar `openspec validate add-notificaciones-auditoria-y-reportes --strict` sin errores y verificar que cada requisito `NOT-*`, `AUD-*` y `REP-*` tiene al menos un escenario vinculado a una prueba.

## Workflow follow-up

- Archivar el change después de la revisión del proyecto.
- Verificar el resultado archivado.
