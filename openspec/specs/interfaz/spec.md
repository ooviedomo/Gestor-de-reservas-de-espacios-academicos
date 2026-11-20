# interfaz Specification

## Purpose
Hace visible el acceso al sistema y guía a cada rol hacia las operaciones que la matriz
canónica le permite, sin exponer URLs ocultas ni duplicar reglas de autorización.

## Requirements

### Requirement: UI-001 Portada y navegación por rol

El sistema MUST mostrar una portada con accesos a los flujos disponibles y un menú de
navegación que MUST ocultar las operaciones no otorgadas explícitamente al rol activo.
Un visitante sin sesión MUST ver únicamente la consulta de disponibilidad, el registro y
el acceso.

#### Scenario: Visitante sin sesión
- **GIVEN** una persona sin sesión autenticada
- **WHEN** abre la portada
- **THEN** ve los enlaces de disponibilidad, registro y acceso, y no ve el panel de aprobación, el historial ni la administración de espacios

#### Scenario: Solicitante autenticado
- **GIVEN** una sesión con rol `Solicitante`
- **WHEN** abre la portada
- **THEN** ve los accesos a disponibilidad, solicitar reserva, cancelar reserva e historial propio, y no ve aprobación ni administración

#### Scenario: Coordinador autenticado
- **GIVEN** una sesión con rol `Coordinador de espacios`
- **WHEN** abre el menú
- **THEN** ve el panel de aprobación y no ve la administración de espacios ni la solicitud de reservas

#### Scenario: Administrador autenticado
- **GIVEN** una sesión con rol `Administrador del sistema`
- **WHEN** abre el menú
- **THEN** ve la aprobación y la administración de espacios, y no ve la solicitud de reservas

### Requirement: UI-002 Inicio de sesión desde la interfaz

El sistema MUST permitir iniciar sesión con email y contraseña desde una página de acceso
cuyo formulario se envía al servidor en una petición HTTP normal. La sesión MUST
establecerse en la respuesta a esa petición, antes de cualquier redirección, sin depender
de un circuito interactivo. Un fallo MUST mostrar el código estable sin revelar qué campo
falló.

#### Scenario: Inicio válido
- **GIVEN** una cuenta activa con email y contraseña correctos
- **WHEN** se envía el formulario de acceso al servidor
- **THEN** la respuesta HTTP incluye la cookie de sesión, se redirige a la portada y se ve la etiqueta del rol correspondiente

#### Scenario: Formulario sin circuito interactivo
- **GIVEN** una página de acceso cargada sin sesión interactiva ni JavaScript
- **WHEN** se pulsa "Acceder"
- **THEN** la autenticación se resuelve en la petición HTTP del formulario sin un error de cabeceras ya iniciadas

#### Scenario: Credenciales inválidas
- **GIVEN** una contraseña incorrecta o un email inexistente
- **WHEN** se envía el formulario de acceso
- **THEN** se redirige al formulario con `CREDENCIALES_INVALIDAS`, no se emite cookie de sesión y no se indica cuál campo falló

#### Scenario: Cuenta desactivada
- **GIVEN** una cuenta desactivada con credenciales correctas
- **WHEN** se envía el formulario de acceso
- **THEN** se redirige al formulario con `CUENTA_DESACTIVADA` y no se emite cookie de sesión

### Requirement: UI-003 Registro desde la interfaz

El sistema MUST permitir el autorregistro de `Solicitante` desde una página Razor
reutilizando `USR-001`, mostrando sus códigos de error y enviando al usuario al acceso
cuando termine correctamente.

#### Scenario: Registro válido
- **GIVEN** un email no registrado y una contraseña que cumple la directiva
- **WHEN** se envía el formulario de registro
- **THEN** se crea la cuenta de `Solicitante` y se redirige a la página de acceso

#### Scenario: Registro con email duplicado
- **GIVEN** un email ya registrado
- **WHEN** se envía el formulario de registro
- **THEN** se muestra `EMAIL_DUPLICADO`

#### Scenario: Registro con contraseña débil
- **GIVEN** una contraseña sin mayúscula o número
- **WHEN** se envía el formulario de registro
- **THEN** se muestra `CONTRASENA_DEBIL`

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
