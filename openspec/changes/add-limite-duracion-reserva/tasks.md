# Tasks

## 1. Invariante de duración en el dominio

- [ ] 1.1 Añadir `DuracionMaximaExcedida = "DURACION_MAXIMA_EXCEDIDA"` a `DomainErrors` junto a los códigos de RES-001; verificar que la solución compila.
- [ ] 1.2 Comprobar en el constructor de `Reservation` que `fin - inicio <= TimeSpan.FromHours(4)` y lanzar `DomainException` con el código nuevo cuando se exceda, sin tocar la cadena de validadores; verificar que el mensaje de `RANGO_INVALIDO` sigue ejecutándose primero para un rango inverso.

## 2. Regresiones

- [ ] 2.1 Añadir pruebas unitarias del agregado: 10:00–14:00 crea la reserva, 10:00–14:01 falla con `DURACION_MAXIMA_EXCEDIDA`, y un rango inverso de más de cuatro horas sigue fallando con `RANGO_INVALIDO`.
- [ ] 2.2 Añadir en `CrearReservaTests` una prueba de integración que rechace 10:00–14:01 con el código nuevo y otra que acepte 10:00–14:00, usando PostgreSQL local.
- [ ] 2.3 Añadir una prueba HTTP del endpoint JSON de creación que devuelva el código `DURACION_MAXIMA_EXCEDIDA` para una franja de más de cuatro horas.
- [ ] 2.4 Añadir una prueba bUnit de `SolicitarReserva` que muestre `DURACION_MAXIMA_EXCEDIDA` cuando `CrearReservaCommand` devuelve ese error, sin cambiar el componente.

## 3. Documentación

- [ ] 3.1 Registrar `RES-005` en `docs/TRAZABILIDAD-RESERVAS-APROBACIONES.md` con su escenario, patrón y verificación.
- [ ] 3.2 Registrar los prompts de este change en `docs/REGISTRO-USO-IA.md` y actualizar conteos y cobertura; verificar que no aparecen secretos.

## 4. Verificación final

- [ ] 4.1 Ejecutar `dotnet format --verify-no-changes --no-restore` y `dotnet build`; verificar 0 errores y 0 advertencias.
- [ ] 4.2 Ejecutar la suite completa con PostgreSQL local y medir cobertura; verificar que todas las pruebas pasan y que no baja de 95.1% de líneas ni de 79.1% de ramas.
- [ ] 4.3 Ejecutar `openspec validate add-limite-duracion-reserva --strict` y `openspec validate --all --strict`; verificar que todos los changes y specs son válidos.

## Workflow follow-up

- Commitear los artefactos del change antes de implementar.
- Commitear la implementación por separado.
- Sincronizar `specs/reservas` y archivar el change tras la verificación.
- Verificar que `git status` queda limpio y no pushear sin instrucción explícita.
