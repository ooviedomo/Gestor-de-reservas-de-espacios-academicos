# Tasks

## 1. Identidad y roles

- [x] 1.1 Configurar ASP.NET Core Identity con email como nombre de usuario y semilla de los roles `SOLICITANTE`, `COORDINADOR` y `ADMINISTRADOR` con sus etiquetas `Solicitante`, `Coordinador de espacios` y `Administrador del sistema`, y verificar que la migración inserta exactamente tres roles.
- [x] 1.2 Implementar autorregistro de `Solicitante`, administración de usuarios y autenticación, y verificar `TC-USR-001-*`, `TC-USR-002-*` y `TC-USR-003-*`.
- [x] 1.3 Implementar los guards de autorización según la matriz completa de `autorizacion/spec.md` y verificar `TC-AUTH-001-*` con `WebApplicationFactory`, incluyendo el rechazo de acceso anónimo, que el coordinador no administre espacios ni cree reservas y que el administrador no cree reservas.

## 2. Persistencia de reservas y anti-solape

- [x] 2.1 Crear la entidad `Reservation` y la migración de `reservations` con `tstzrange`, índice `btree_gist`, restricción `no_overlap` y las columnas `asistentes`, `motivo_rechazo` y `completado_en`, y verificar que una inserción solapada falla en PostgreSQL local (`reservaespacios_test`, según ADR-003).
- [x] 2.2 Implementar la máquina de estados en `Domain` con la matriz de transiciones —incluido `APROBADA → COMPLETADA`— y verificar `TC-APR-001-*` y `TC-COM-001-*` de transición inválida.
- [x] 2.3 Mapear la violación de la restricción a `SOLAPE_DETECTADO` en `Application` y verificar que el cliente recibe ese código y no un error SQL crudo; unificar toda transición no permitida bajo `TRANSICION_INVALIDA`.

## 3. Creación y cancelación de reservas

- [ ] 3.1 Sustituir el stub `IDisponibilidadRepository` del change anterior por la implementación real sobre `reservations`, eliminando el stub, y verificar que `ConsultarDisponibilidad` refleja las reservas `PENDIENTE`/`APROBADA` persistidas.
- [ ] 3.2 Implementar `CrearReserva` con revalidación de disponibilidad dentro de una transacción `SERIALIZABLE`, y verificar `TC-RES-001-*` incluido el escenario de disponibilidad cambiada.
- [ ] 3.3 Implementar la prueba de concurrencia con dos peticiones simultáneas sobre la misma franja y verificar `TC-RES-002-*` (una aceptada, una rechazada).
- [ ] 3.4 Implementar `CancelarReserva` con motivo obligatorio y permisos por rol, y verificar `TC-RES-003-*`.
- [ ] 3.5 Crear los componentes Razor de alta y cancelación y verificar con bUnit que muestran `RANGO_INVALIDO`, `CAPACIDAD_EXCEDIDA`, `MOTIVO_OBLIGATORIO` y `NO_AUTORIZADO`.

## 4. Aprobación e historial

- [ ] 4.1 Implementar `AprobarReserva` y `RechazarReserva` registrando actor, fecha y motivo, y verificar `TC-APR-001-*` con pruebas unitarias e integración.
- [ ] 4.2 Implementar `CompletarReserva` manual y el proceso periódico de completado automático al vencer `fin`, y verificar `TC-COM-001-*` con pruebas unitarias e integración.
- [ ] 4.3 Implementar la consulta de historial con filtros y visibilidad por rol, y verificar `TC-RES-004-*` incluido el motivo de rechazo y el actor con nombre y rol.
- [ ] 4.4 Crear el panel de aprobación y la vista de historial en Blazor y verificar con `WebApplicationFactory` que coordinador y solicitante ven conjuntos distintos de reservas.

## 5. Integración y cierre del change

- [ ] 5.1 Ejecutar `dotnet format` y `dotnet test` completos y verificar que no hay errores de analyzers y cobertura ≥ 80%.
- [ ] 5.2 Ejecutar `openspec validate add-reservas-y-aprobaciones --strict` sin errores y verificar que cada requisito `USR-*`, `AUTH-*`, `RES-*`, `APR-*` y `COM-*` tiene al menos un escenario vinculado a una prueba.
- [ ] 5.3 Verificar que el change aplica `State`, `Template Method`, `Chain of Responsibility`, `Command`, `Strategy`, `Observer`, `Adapter`, `Builder` y `Facade` según `design.md` y `docs/adr/ADR-002-patrones-de-diseno.md`, y que cada patrón tiene al menos una prueba que lo exercise.

## Workflow follow-up

- Archivar el change después de la revisión del proyecto.
- Verificar el resultado archivado.
