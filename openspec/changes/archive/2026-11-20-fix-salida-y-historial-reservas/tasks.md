# Tasks

## 1. Cierre de sesión que vuelve al inicio

- [x] 1.1 Añadir `POST /acceso/cerrar-sesion` en `ApiEndpoints.cs`: invoca `ISesionWeb.CerrarSesionAsync()` y devuelve `Results.Redirect("/")`; verificar que `POST /api/autenticacion/logout` sigue respondiendo `{"cerrada":true}` sin redirección.
- [x] 1.2 Apuntar el formulario de `NavMenu.razor` a `/acceso/cerrar-sesion` y añadir la aserción bUnit del `action` en `InterfazAccesoTests.cs`; verificar que la prueba del menú autenticado pasa.
- [x] 1.3 Añadir en `FlujoAccesoWebTests.cs` una prueba HTTP con `WebApplicationFactory`: login por formulario, POST a `/acceso/cerrar-sesion`, `302` a `/`, cookie eliminada y portada anónima; verificar que pasa con PostgreSQL local y que el test del endpoint JSON sigue verde.

## 2. Formato de hora compartido

- [x] 2.1 Crear `FormatoFechaHora` en `ReservaEspacios.Web` con `Hora` (`HH:mm`), `Fecha` (`dd/MM/yyyy`), `FechaHora` (`dd/MM/yyyy HH:mm`) y variantes nulas con `—`, usando `America/Bogota` e `InvariantCulture`; verificar con una prueba unitaria que un `DateTimeOffset` fijo produce `10:00`, `20/11/2026` y `20/11/2026 10:00`.
- [x] 2.2 Mostrar `Inicio`/`Fin` en `HH:mm`, añadir la columna `Fecha` y aplicar el formateador a las marcas de auditoría en `HistorialReservas.razor` y `AprobacionesReserva.razor`; verificar con bUnit que la fila contiene `10:00`, `11:00` y `20/11/2026`.
- [x] 2.3 Poner `type="time"` y ayuda `HH:mm` en los campos de inicio y fin de `SolicitarReserva.razor` y `ConsultarDisponibilidadPage.razor`, con la fecha en `dd/MM/yyyy`; verificar que las pruebas bUnit existentes siguen enviando `10:00`/`11:00` y que una nueva aserción comprueba el atributo `title` y el tipo.

## 3. Cancelación desde el historial

- [x] 3.1 Añadir `SolicitanteId` a `HistorialReservaDto` y rellenarlo desde `reserva.UsuarioId` en `ConsultarHistorial.cs`; verificar que la solución compila y que las fábricas de `HistorialReservaDto` en pruebas incluyen el nuevo valor.
- [x] 3.2 Añadir en `HistorialReservas.razor` la columna de acción, el botón condicionado a `PuedeCancelarReserva` y a los estados `PENDIENTE`/`APROBADA`, y el campo de motivo por fila; verificar con bUnit que el botón aparece solo en la reserva propia cancelable.
- [x] 3.3 Implementar el envío con `CancelarReservaCommand`, el mensaje `MOTIVO_OBLIGATORIO` local, la recarga del historial y el mensaje de éxito; verificar con bUnit que el comando recibe el id y el motivo, que no se envía sin motivo y que la fila desaparece como cancelable tras la recarga.

## 4. Regresión y documentación

- [x] 4.1 Registrar en `docs/REGISTRO-USO-IA.md` los prompts de este change (salida al inicio, formato `HH:mm`, cancelación por fila) y la verificación aplicada; verificar que el archivo queda actualizado y sin secretos.
- [x] 4.2 Revisar que ningún archivo contiene contraseñas ni cadenas de conexión versionadas y que no hay dependencias NuGet nuevas; verificar con `git diff` y `git status`.

## 5. Verificación final

- [x] 5.1 Ejecutar `dotnet format --verify-no-changes --no-restore` y `dotnet build`; verificar 0 errores y 0 advertencias.
- [x] 5.2 Ejecutar la suite completa con PostgreSQL local y medir cobertura; verificar que todas las pruebas pasan y que no baja respecto a la línea base (95.1% de líneas).
- [x] 5.3 Ejecutar `openspec validate fix-salida-y-historial-reservas --strict` y `openspec validate --all --strict`; verificar que todos los changes y specs son válidos.

## Workflow follow-up

- Commitear la implementación.
- Sincronizar `specs/interfaz` y archivar el change tras la aprobación.
- Verificar que `git status` queda limpio y no pushear sin instrucción explícita.
