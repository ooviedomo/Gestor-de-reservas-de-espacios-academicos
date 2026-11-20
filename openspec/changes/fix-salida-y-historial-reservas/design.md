# Design

## Context

Ver `proposal.md` para la motivación. La UI ya tiene dos patrones que conviene reutilizar:
el acceso usa un formulario HTML normal que se envía a `POST /acceso/autenticar` y cuya
respuesta emite la cookie antes de redirigir; el cierre de sesión del menú, en cambio, apunta
a `POST /api/autenticacion/logout`, un endpoint JSON que responde `{"cerrada":true}` y deja
al usuario en la misma página. Los componentes de `InteractiveServer` muestran
`DateTimeOffset` con `ToString()` implícito, mientras que los formularios existentes ya
interpretan `HH:mm` con `TimeOnly.TryParse` y convierten a `America/Bogota`. La cancelación
ya existe como `CancelarReservaCommand` y su guard comprueba propiedad y estado; lo que
falta es un punto de entrada en el historial. Según ADR-002, la sesión es un Adapter Web,
la autorización una Strategy en Application y la máquina de estados la fuente de verdad de
las transiciones.

## Goals / Non-Goals

**Goals:**

- Que el botón «Cerrar sesión» termine en la portada anónima con una respuesta HTTP normal.
- Un único formateador de presentación para fecha y hora, sin tocar el dominio ni la BD.
- Exponer la cancelación en el historial sin duplicar la validación de permisos ni de estados.
- Mantener los contratos JSON existentes y añadir cobertura en los niveles que ya usa el
  proyecto (bUnit para componentes, `WebApplicationFactory` para HTTP).

**Non-Goals:**

- Cambiar la autenticación, los roles, los estados, los intervalos semiabiertos ni el esquema.
- Sustituir `POST /api/autenticacion/logout` ni la página `/reservas/cancelar`.
- Añadir dependencias NuGet, migraciones, secretos ni variables de entorno nuevas.
- Reabrir changes archivados ni modificar `PLAN_INGENIERIA.md` o los ADR existentes.

## Decisions

1. **Endpoint propio para el cierre de sesión de la UI.**
   Añadir `POST /acceso/cerrar-sesion`, que llama a `ISesionWeb.CerrarSesionAsync()` y
   devuelve `Results.Redirect("/")`. `NavMenu.razor` pasa su formulario a esa ruta. Se
   conserva `POST /api/autenticacion/logout` con su respuesta JSON. Alternativa descartada:
   añadir JavaScript que llame al endpoint JSON y luego navegue; añade un circuito y una
   dependencia del cliente para algo que un formulario resuelve. Otra alternativa
   descartada: registrar el POST en `/acceso/login`, que ya sirve la página Razor y provoca
   `AmbiguousMatchException`. El endpoint no enlaza parámetros del cuerpo, por lo que no
   necesita antiforgery explícito; `UseAntiforgery()` sigue activo para el resto del pipeline.

2. **Formateador estático compartido en la capa Web.**
   `FormatoFechaHora` (namespace `ReservaEspacios.Web`, ya importado por `_Imports.razor`)
   expone `Hora` (`HH:mm`), `Fecha` (`dd/MM/yyyy`) y `FechaHora` (`dd/MM/yyyy HH:mm`) con
   `CultureInfo.InvariantCulture` y conversión previa a `America/Bogota`, más sobrecargas
   que devuelven `—` para valores nulos. Es presentación pura: no tiene estado, no se registra
   en DI y no altera el `DateTimeOffset` almacenado. Alternativa descartada: `IFormatProvider`
   inyectado o `ToString` con la cultura del servidor; introduce estado y depende de la
   configuración de la máquina. En las tablas se añade una columna `Fecha` separada y en los
   formularios los campos pasan a `type="time"` con ayuda visible `HH:mm`; la fecha sigue
   siendo un campo `dd/MM/yyyy`. Las marcas de auditoría usan `FechaHora` para el mismo
   criterio visual.

3. **`SolicitanteId` en la fila del historial.**
   `HistorialReservaDto` incorpora `SolicitanteId` y el query handler lo rellena desde
   `reserva.UsuarioId`. Es el dato mínimo que la UI necesita para llamar a
   `IEvaluadorPermisos.PuedeCancelarReserva(rol, solicitanteId, usuarioActualId)`. Alternativa
   descartada: comparar solo el nombre del solicitante o esconder el botón por rol; el
   primero no es identificador y el segundo duplicaría la matriz canónica de `Rol.cs`.

4. **Cancelación por fila reutilizando el comando.**
   `HistorialReservas.razor` inyecta `ICommandHandler<CancelarReservaCommand>`, mantiene un
   diccionario de motivos por identificador como ya hace el panel de aprobación y muestra el
   botón solo si el evaluador lo permite y el código de estado es `PENDIENTE` o `APROBADA`
   (los únicos que la máquina de estados acepta cancelar). El botón abre un campo de motivo
   en la propia fila; el envío valida `MOTIVO_OBLIGATORIO` localmente, llama al comando y
   recarga el historial. Alternativa descartada: un nuevo endpoint o una nueva regla de
   dominio para cancelar desde la tabla; `CancelarReserva` ya aplica guard, transición,
   motivo, evento de dominio y auditoría. La página `/reservas/cancelar` permanece intacta.

5. **Pruebas en los mismos niveles que el comportamiento.**
   bUnit comprueba el `action` del formulario de salida, los textos `HH:mm`/`dd/MM/yyyy` y
   la visibilidad del botón por propiedad y estado, incluido el motivo obligatorio. La
   prueba HTTP verifica `302` a `/` con eliminación de cookie para el logout de la UI y que
   el endpoint JSON sigue respondiendo `200` con `cerrada: true`. Alternativa descartada:
   probar solo el formateador; no cubriría la integración de la redirección ni la
   visibilidad condicional del botón.

## Risks / Trade-offs

- [El nuevo POST con formulario podría exigir antiforgery] → No enlaza parámetros desde el
  cuerpo, igual que `/acceso/autenticar`; se confirma con la prueba HTTP antes de cerrar.
- [`SolicitanteId` cambia la firma posicional del DTO] → Solo se construye en el query
  handler; el compilador y la suite detectan cualquier otro uso, y las fábricas de pruebas
  se ajustan en el mismo change.
- [La zona horaria podría cambiar el día mostrado] → El formateador convierte explícitamente
  a `America/Bogota` antes de aplicar `dd/MM/yyyy`; las pruebas usan instants fijos.
- [El botón podría aparecer en estados no cancelables] → La comprobación de estado usa los
  códigos canónicos y el comando sigue siendo la última línea de defensa.
- [El formato de los campos `type="time"` cambia el valor enlazado] → Los valores por
  defecto `10:00`/`11:00` ya son válidos y el parsing con `TimeOnly` se mantiene; las
  pruebas bUnit existentes siguen usando esos valores.

## Migration Plan

No hay cambios de esquema ni de datos: basta con desplegar la nueva versión de la Web y de
`ReservaEspacios.Application`. No hay cookie que invalidar ni sesión que migrar. La
reversión consiste en volver a la versión anterior del menú, el endpoint, el DTO y los
componentes; no hay migración inversa.

## Open Questions

Ninguna.
