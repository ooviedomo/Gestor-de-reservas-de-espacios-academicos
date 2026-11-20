# Spec Delta

## MODIFIED Requirements

### Requirement: UI-004 Cierre de sesión

El sistema MUST permitir cerrar la sesión desde el menú y MUST devolver la interfaz al
estado anónimo mediante una redirección HTTP a la portada. El endpoint JSON de cierre de
sesión existente MUST conservar su contrato para clientes que no son el navegador.

#### Scenario: Cerrar sesión
- **GIVEN** una sesión autenticada
- **WHEN** se pulsa "Cerrar sesión"
- **THEN** se elimina la cookie de autenticación, la respuesta redirige a la portada y no se muestran enlaces protegidos

#### Scenario: Contrato JSON conservado
- **GIVEN** un cliente que consume la API JSON
- **WHEN** envía una petición al endpoint JSON de cierre de sesión
- **THEN** se cierra la sesión y se devuelve la respuesta `{"cerrada":true}` sin redirección

## ADDED Requirements

### Requirement: UI-005 Formato de fecha y hora en la interfaz

El sistema MUST mostrar las horas de inicio y fin de una reserva como `HH:mm` en las
tablas, con la fecha `dd/MM/yyyy` en una columna separada. Los formularios de solicitud y
consulta MUST indicar el formato `HH:mm` en sus campos de hora. La conversión MUST usar la
zona horaria `America/Bogota` sin alterar los intervalos semiabiertos `[inicio, fin)` del
dominio.

#### Scenario: Tabla de historial con hora legible
- **GIVEN** una reserva con inicio `2026-11-20T10:00:00-05:00` y fin `2026-11-20T11:00:00-05:00`
- **WHEN** se muestra en el historial
- **THEN** la columna Inicio muestra `10:00`, la columna Fin muestra `11:00` y la fecha aparece como `20/11/2026`

#### Scenario: Panel de aprobación con hora legible
- **GIVEN** una reserva pendiente visible para el coordinador
- **WHEN** se muestra en el panel de aprobación
- **THEN** sus columnas Inicio y Fin muestran la hora en `HH:mm` y la fecha en `dd/MM/yyyy`

#### Scenario: Formularios con ayuda de formato
- **GIVEN** una persona en el formulario de solicitud o de consulta de disponibilidad
- **WHEN** observa los campos de inicio y fin
- **THEN** ambos campos aceptan y anuncian el formato `HH:mm` y la fecha se introduce por separado

### Requirement: UI-006 Cancelación de reservas desde el historial

El sistema MUST ofrecer una acción de cancelación por fila en el historial para cada
reserva que la persona pueda cancelar según la Strategy de permisos y cuyo estado permita
la transición (`PENDIENTE` o `APROBADA`). La acción MUST pedir un motivo obligatorio y
MUST reutilizar el comando de cancelación existente; tras una cancelación correcta el
historial MUST recargarse y la acción MUST desaparecer para esa reserva.

#### Scenario: Solicitante cancela su propia reserva
- **GIVEN** un solicitante con una reserva propia en estado `APROBADA`
- **WHEN** escribe un motivo y pulsa el botón de cancelación de esa fila
- **THEN** se envía `CancelarReservaCommand` con el identificador y el motivo, se recarga el historial y la fila pasa a `CANCELADA`

#### Scenario: Sin acción sobre reservas no cancelables
- **GIVEN** una reserva ajena al solicitante activo o en estado `RECHAZADA`, `COMPLETADA` o `CANCELADA`
- **WHEN** se muestra su fila en el historial
- **THEN** la fila no ofrece el botón de cancelación

#### Scenario: Motivo obligatorio
- **GIVEN** una reserva cancelable del solicitante
- **WHEN** pulsa el botón de cancelación sin escribir motivo
- **THEN** se muestra `MOTIVO_OBLIGATORIO` y no se envía el comando de cancelación
