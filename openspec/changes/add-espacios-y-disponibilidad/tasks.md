# Tasks

## 1. Estructura y persistencia

- [ ] 1.1 Crear `ReservaEspacios.sln` y los proyectos `Domain`, `Application`, `Infrastructure` y `Web`, y verificar que `dotnet build` termina sin errores.
- [ ] 1.2 Agregar los paquetes EF Core 10, Npgsql, xUnit, bUnit, WebApplicationFactory y Testcontainers.NET, y verificar que `dotnet restore` completa correctamente.
- [ ] 1.3 Crear la migración de `spaces` con índice único en `nombre`, `CHECK (hora_apertura < hora_cierre)` y columna `activo`, y verificar con una prueba de integración que se rechazan nombre duplicado y horario inválido (ESP-001).

## 2. Dominio y casos de uso de espacios

- [ ] 2.1 Implementar la entidad `Space` y el value object `HorarioAtencion` con las validaciones de ESP-001, y verificar las pruebas unitarias `TC-ESP-001-*`.
- [ ] 2.2 Implementar `RegistrarSpace`, `EditarSpace`, `DesactivarSpace` y `ReactivarSpace` según ESP-002, y verificar con pruebas unitarias que desactivar un espacio conserva sus reservas futuras simuladas.
- [ ] 2.3 Implementar la consulta de catálogo con filtros por tipo, capacidad mínima y estado según ESP-003, y verificar `TC-ESP-003-*` sobre PostgreSQL con Testcontainers.NET.
- [ ] 2.4 Crear el componente Razor de administración de espacios y verificar con bUnit que el formulario muestra `NOMBRE_DUPLICADO`, `CAPACIDAD_INVALIDA` y `HORARIO_INVALIDO`.

## 3. Disponibilidad

- [ ] 3.1 Definir `IDisponibilidadRepository` con un stub de reservas bloqueantes hasta que exista la tabla `reservations`, y verificar su prueba unitaria de aislamiento.
- [ ] 3.2 Implementar `ConsultarDisponibilidad` con las reglas DISP-001 y DISP-002 (estados `PENDIENTE`/`APROBADA`, intervalos `[inicio, fin)` y motivos de no disponibilidad), y verificar `TC-DISP-001-*` y `TC-DISP-002-*`.
- [ ] 3.3 Exponer la consulta de disponibilidad y crear el componente Razor correspondiente, y verificar con `WebApplicationFactory` los resultados `DISPONIBLE`, `ESPACIO_INACTIVO`, `FUERA_DE_HORARIO` y `SOLAPE_CON_RESERVA`.

## 4. Integración y cierre del change

- [ ] 4.1 Ejecutar `dotnet format` y `dotnet test` completos, y verificar que no hay errores de analyzers y que la cobertura de este change es ≥ 80%.
- [ ] 4.2 Ejecutar `openspec validate add-espacios-y-disponibilidad` sin errores y verificar que cada requisito `ESP-*` y `DISP-*` tiene al menos un escenario vinculado a una prueba.

## Workflow follow-up

- Archivar el change después de la revisión del proyecto.
- Verificar el resultado archivado.
