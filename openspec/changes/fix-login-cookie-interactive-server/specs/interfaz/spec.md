# Spec Delta

## MODIFIED Requirements

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
