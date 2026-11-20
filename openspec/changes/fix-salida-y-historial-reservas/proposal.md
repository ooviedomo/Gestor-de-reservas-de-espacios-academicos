# Proposal

## Why

Tres fricciones observadas en la interfaz ya operativa: al pulsar «Cerrar sesión» el menú
llama al endpoint JSON de la API, que responde `{"cerrada":true}` sin mover al usuario; las
tablas muestran `Inicio` y `Fin` con el formato por defecto de `DateTimeOffset`, ilegible
como hora de clase; y el solicitante solo puede cancelar desde una página que exige escribir
a mano el identificador de la reserva, sin relación visible con su historial.

## What Changes

- Añadir un endpoint de formulario para el cierre de sesión que elimine la cookie y
  redirija a la portada; apuntar el menú a ese endpoint y conservar el contrato JSON de
  `POST /api/autenticacion/logout`.
- Mostrar la hora como `HH:mm` en las tablas y como ayuda de los campos de hora en los
  formularios, con la fecha `dd/MM/yyyy` en columna separada y zona `America/Bogota`.
- Añadir un botón de cancelación por fila en el historial del solicitante, con cuadro de
  motivo y reutilizando `CancelarReservaCommand`; el botón solo aparece para reservas
  propias en estado cancelable (`PENDIENTE` o `APROBADA`).
- Incluir el identificador del solicitante en la fila del historial para que la UI pueda
  aplicar la misma Strategy de permisos que el comando, sin duplicar reglas de autorización.
- Añadir regresiones bUnit y HTTP para los tres comportamientos y actualizar el registro de
  uso de IA.
- Registrar las decisiones en `design.md`; no se reabre ningún change archivado ni se
  modifica `PLAN_INGENIERIA.md`.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `interfaz`: `UI-004` pasa a exigir que el cierre de sesión devuelva la interfaz al inicio
  mediante redirección HTTP, manteniendo intacto el endpoint JSON de la API. Se añaden
  `UI-005` (formato de fecha y hora) y `UI-006` (cancelación desde el historial) como
  comportamientos nuevos de esta misma capacidad.

## Impact

- `src/ReservaEspacios.Web/ApiEndpoints.cs`: nuevo `POST /acceso/cerrar-sesion`; el endpoint
  JSON de logout queda intacto.
- `src/ReservaEspacios.Web/Components/Layout/NavMenu.razor`: formulario de salida apuntando
  al nuevo endpoint.
- `src/ReservaEspacios.Web/Components/Pages/HistorialReservas.razor` y
  `AprobacionesReserva.razor`: celdas de hora y fecha.
- `src/ReservaEspacios.Web/Components/Pages/SolicitarReserva.razor` y
  `ConsultarDisponibilidadPage.razor`: ayuda `HH:mm` en los campos de hora.
- `src/ReservaEspacios.Application/Reservas/ConsultarHistorial.cs`: `HistorialReservaDto`
  incorpora `SolicitanteId`.
- Formateador compartido de fecha/hora en la capa Web; sin dependencias NuGet nuevas.
- `tests/ReservaEspacios.E2ETests/` y `tests/ReservaEspacios.IntegrationTests/`.
- `docs/REGISTRO-USO-IA.md`.
- Sin cambios en dominio, migraciones ni cadena de conexión; no se versionan credenciales.
