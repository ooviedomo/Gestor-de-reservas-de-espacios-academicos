# Spec Delta

## Purpose

Define el conjunto exacto de permisos de los tres roles del sistema y garantiza que cada operación protegida se evalúe contra esos permisos antes de ejecutarse, evitando accesos por omisión.

## ADDED Requirements

### Requirement: AUTH-001 Permisos por rol

El sistema MUST aplicar la matriz de permisos de los roles `Solicitante`, `Coordinador de espacios` y `Administrador del sistema` a todas las operaciones protegidas, y MUST denegar por defecto cualquier acción no otorgada explícitamente a algún rol.

#### Scenario: Solicitante crea y cancela sus propias reservas
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** crea una reserva y después cancela esa misma reserva
- **THEN** ambas operaciones se permiten

#### Scenario: Solicitante no aprueba reservas
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** intenta aprobar una reserva `PENDIENTE`
- **THEN** la operación se rechaza con `NO_AUTORIZADO`

#### Scenario: Coordinador aprueba pero no administra usuarios
- **GIVEN** un usuario autenticado como `Coordinador de espacios`
- **WHEN** aprueba una reserva y luego intenta crear un usuario
- **THEN** la aprobación se permite y la creación de usuario se rechaza con `NO_AUTORIZADO`

#### Scenario: Administrador gestiona usuarios, espacios y aprobaciones
- **GIVEN** un usuario autenticado como `Administrador del sistema`
- **WHEN** crea un usuario, edita un espacio y aprueba una reserva
- **THEN** las tres operaciones se permiten

#### Scenario: Acceso anónimo denegado
- **GIVEN** una solicitud sin sesión autenticada
- **WHEN** intenta consultar el historial de reservas
- **THEN** se rechaza con `NO_AUTENTICADO`
