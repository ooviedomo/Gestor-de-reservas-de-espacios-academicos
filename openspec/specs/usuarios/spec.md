# usuarios Specification

## Purpose
Identifica a las personas que interactúan con el sistema y controla su acceso, permitiendo que el Solicitante se registre por sí mismo mientras que los roles con privilegios son creados únicamente por el Administrador del sistema.

## Requirements

### Requirement: USR-001 Registro de Solicitante

El sistema MUST permitir el autorregistro de usuarios con el rol `Solicitante` mediante nombre, email único y contraseña de al menos 8 caracteres que incluya mayúscula y número. La cuenta queda activa al completarse el registro.

#### Scenario: Registro válido
- **GIVEN** que no existe ningún usuario con el email `ana@universidad.edu`
- **WHEN** se registra a `Ana` con ese email y la contraseña `Segura123`
- **THEN** se crea una cuenta activa con rol `Solicitante`

#### Scenario: Email duplicado
- **GIVEN** que ya existe un usuario con email `ana@universidad.edu`
- **WHEN** se intenta registrar otro usuario con el mismo email
- **THEN** la operación se rechaza con `EMAIL_DUPLICADO` y no se crea la cuenta

#### Scenario: Contraseña débil
- **GIVEN** un formulario de registro
- **WHEN** se envía la contraseña `solo_minusculas`
- **THEN** la operación se rechaza con `CONTRASENA_DEBIL`

### Requirement: USR-002 Administración de usuarios

El sistema MUST permitir al `Administrador del sistema` crear cuentas con rol `Coordinador de espacios` o `Administrador del sistema`, cambiar roles y activar o desactivar cuentas. Ningún otro rol MUST poder crear cuentas privilegiadas ni modificar roles.

#### Scenario: Administrador crea coordinador
- **GIVEN** un usuario autenticado como `Administrador del sistema`
- **WHEN** crea una cuenta con rol `Coordinador de espacios`
- **THEN** la cuenta se crea con ese rol y queda activa

#### Scenario: Solicitante intenta crear coordinador
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** intenta crear una cuenta con rol `Coordinador de espacios`
- **THEN** la operación se rechaza con `NO_AUTORIZADO`

#### Scenario: Desactivar cuenta
- **GIVEN** un usuario activo
- **WHEN** el `Administrador del sistema` desactiva su cuenta
- **THEN** el usuario ya no puede iniciar sesión

### Requirement: USR-003 Autenticación

El sistema MUST permitir iniciar sesión con email y contraseña y MUST denegar el acceso a cuentas desactivadas o con credenciales inválidas, devolviendo un error que no revele cuál de los dos campos falló.

#### Scenario: Inicio de sesión válido
- **GIVEN** una cuenta activa con email `ana@universidad.edu`
- **WHEN** se autentica con su contraseña correcta
- **THEN** obtiene una sesión asociada a su rol `Solicitante`

#### Scenario: Credenciales inválidas
- **GIVEN** un usuario que intenta autenticarse
- **WHEN** envía una contraseña incorrecta
- **THEN** se rechaza con `CREDENCIALES_INVALIDAS` sin indicar si falló el email o la contraseña

#### Scenario: Cuenta desactivada
- **GIVEN** una cuenta desactivada
- **WHEN** intenta autenticarse con credenciales correctas
- **THEN** se rechaza con `CUENTA_DESACTIVADA`
