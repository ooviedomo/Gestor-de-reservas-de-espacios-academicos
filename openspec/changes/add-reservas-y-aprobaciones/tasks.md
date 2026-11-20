# Tasks

## 1. Identidad y roles

- [ ] 1.1 Configurar ASP.NET Core Identity con email como nombre de usuario y semilla de los roles `Solicitante`, `Coordinador de espacios` y `Administrador del sistema`, y verificar que la migración inserta exactamente tres roles.
- [ ] 1.2 Implementar autorregistro de `Solicitante`, administración de usuarios y autenticación, y verificar `TC-USR-001-*`, `TC-USR-002-*` y `TC-USR-003-*`.
- [ ] 1.3 Implementar los guards de autorización por rol y verificar `TC-AUTH-001-*` con `WebApplicationFactory`, incluyendo el rechazo de acceso anónimo.

## 2. Persistencia de reservas y anti-solape

- [ ] 2.1 Crear la entidad `Reservation` y la migración de `reservations` con `tstzrange`, índice `btree_gist` y restricción `no_overlap`, y verificar que una inserción solapada falla en PostgreSQL con Testcontainers.
- [ ] 2.2 Implementar la máquina de estados en `Domain` con la matriz de transiciones y verificar `TC-APR-001-*` de transición inválida.
- [ ] 2.3 Mapear la violación de la restricción a `SOLAPE_DETECTADO` en `Application` y verificar que el cliente recibe ese código y no un error SQL crudo.

## 3. Creación y cancelación de reservas

- [ ] 3.1 Implementar `CrearReserva` con revalidación de disponibilidad dentro de una transacción `SERIALIZABLE`, y verificar `TC-RES-001-*` incluido el escenario de disponibilidad cambiada.
- [ ] 3.2 Implementar la prueba de concurrencia con dos peticiones simultáneas sobre la misma franja y verificar `TC-RES-002-*` (una aceptada, una rechazada).
- [ ] 3.3 Implementar `CancelarReserva` con motivo obligatorio y permisos por rol, y verificar `TC-RES-003-*`.
- [ ] 3.4 Crear los componentes Razor de alta y cancelación y verificar con bUnit que muestran `RANGO_INVALIDO`, `CAPACIDAD_EXCEDIDA`, `MOTIVO_OBLIGATORIO` y `NO_AUTORIZADO`.

## 4. Aprobación e historial

- [ ] 4.1 Implementar `AprobarReserva` y `RechazarReserva` registrando actor, fecha y motivo, y verificar `TC-APR-001-*` con pruebas unitarias e integración.
- [ ] 4.2 Implementar la consulta de historial con filtros y visibilidad por rol, y verificar `TC-RES-004-*`.
- [ ] 4.3 Crear el panel de aprobación y la vista de historial en Blazor y verificar con `WebApplicationFactory` que coordinador y solicitante ven conjuntos distintos de reservas.

## 5. Integración y cierre del change

- [ ] 5.1 Ejecutar `dotnet format` y `dotnet test` completos y verificar que no hay errores de analyzers y cobertura ≥ 80%.
- [ ] 5.2 Ejecutar `openspec validate add-reservas-y-aprobaciones --strict` sin errores y verificar que cada requisito `USR-*`, `AUTH-*`, `RES-*` y `APR-*` tiene al menos un escenario vinculado a una prueba.

## Workflow follow-up

- Archivar el change después de la revisión del proyecto.
- Verificar el resultado archivado.
