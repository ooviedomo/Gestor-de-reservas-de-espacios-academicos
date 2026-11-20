# Spec Delta

## Purpose

Hace visible el acceso al sistema y guía a cada rol hacia las operaciones que la matriz
canónica le permite, sin exponer URLs ocultas ni duplicar reglas de autorización.

## ADDED Requirements

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

El sistema MUST permitir iniciar sesión con email y contraseña desde una página Razor,
reutilizando el caso de uso `USR-003`. Un inicio válido MUST establecer la sesión del
rol correspondiente; un fallo MUST mostrar el código estable sin revelar qué campo falló.

#### Scenario: Inicio válido
- **GIVEN** una cuenta activa con email y contraseña correctos
- **WHEN** se envía el formulario de acceso
- **THEN** se establece la sesión y se redirige a la portada con la etiqueta del rol visible

#### Scenario: Credenciales inválidas
- **GIVEN** una contraseña incorrecta o un email inexistente
- **WHEN** se envía el formulario de acceso
- **THEN** se muestra `CREDENCIALES_INVALIDAS` sin indicar cuál campo falló

#### Scenario: Cuenta desactivada
- **GIVEN** una cuenta desactivada con credenciales correctas
- **WHEN** se envía el formulario de acceso
- **THEN** se muestra `CUENTA_DESACTIVADA`

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
estado anónimo.

#### Scenario: Cerrar sesión
- **GIVEN** una sesión autenticada
- **WHEN** se pulsa "Cerrar sesión"
- **THEN** se elimina la cookie de autenticación, se vuelve a la portada y no se muestran enlaces protegidos
