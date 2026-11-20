# autorizacion Specification

## Purpose
Define el conjunto exacto de permisos de los tres roles del sistema y garantiza que cada operación protegida se evalúe contra esos permisos antes de ejecutarse, evitando accesos por omisión.

## Requirements

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

#### Scenario: Coordinador no administra espacios
- **GIVEN** un usuario autenticado como `Coordinador de espacios`
- **WHEN** intenta registrar o desactivar un espacio
- **THEN** la operación se rechaza con `NO_AUTORIZADO`

#### Scenario: Coordinador no crea reservas
- **GIVEN** un usuario autenticado como `Coordinador de espacios`
- **WHEN** intenta crear una reserva
- **THEN** la operación se rechaza con `NO_AUTORIZADO`

#### Scenario: Administrador no crea reservas
- **GIVEN** un usuario autenticado como `Administrador del sistema`
- **WHEN** intenta crear una reserva
- **THEN** la operación se rechaza con `NO_AUTORIZADO`

### Requirement: AUTH-002 Permisos sobre reservas

El sistema MUST aplicar esta matriz a las operaciones de reservas y disponibilidad:

| Operación | Solicitante | Coordinador | Administrador |
|---|---|---|---|
| Consultar disponibilidad | Sí | Sí | Sí |
| Crear reserva | Solo propia | No | No |
| Cancelar reserva | Solo propia | Todas | Todas |
| Consultar historial | Solo propio | Todas | Todas |
| Aprobar, rechazar y completar | No | Sí | Sí |

#### Scenario: Coordinador cancela reserva ajena
- **GIVEN** una reserva `APROBADA` de otro solicitante
- **WHEN** el `Coordinador de espacios` la cancela con motivo
- **THEN** la operación se permite

#### Scenario: Solicitante ve solo su historial
- **GIVEN** reservas de dos solicitantes
- **WHEN** un `Solicitante` consulta el historial
- **THEN** solo ve sus propias reservas

### Requirement: AUTH-003 Permisos sobre espacios, usuarios, auditoría y reportes

El sistema MUST aplicar esta matriz a las operaciones de configuración y consulta global:

| Operación | Solicitante | Coordinador | Administrador |
|---|---|---|---|
| Registrar y administrar espacios | No | No | Sí |
| Crear usuarios y cambiar roles | No | No | Sí |
| Consultar auditoría | No | Sí | Sí |
| Consultar reportes | No | Sí | Sí |

#### Scenario: Coordinador consulta auditoría y reportes
- **GIVEN** un usuario autenticado como `Coordinador de espacios`
- **WHEN** consulta la auditoría y el reporte de ocupación
- **THEN** ambas operaciones se permiten

#### Scenario: Solicitante sin acceso a auditoría ni reportes
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** intenta consultar la auditoría o el reporte
- **THEN** ambas operaciones se rechazan con `NO_AUTORIZADO`
