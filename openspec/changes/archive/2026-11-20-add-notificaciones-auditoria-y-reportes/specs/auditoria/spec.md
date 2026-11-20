# Spec Delta

## Purpose

Conserva un registro inmutable de las acciones sensibles del sistema para que la institución pueda demostrar quién realizó cada cambio relevante, cuándo ocurrió y sobre qué entidad, sin que el propio registro pueda alterarse.

## ADDED Requirements

### Requirement: AUD-001 Registro inmutable de acciones sensibles

El sistema MUST insertar un registro de auditoría con entidad, identificador, acción, actor —almacenado como identificador de usuario y mostrado con su nombre y rol— y marca de tiempo para el alta de usuarios, el cambio de roles y la creación, aprobación, rechazo, cancelación o completado de reservas. El registro MUST ser de solo inserción: el sistema MUST NO permitir actualizarlo ni eliminarlo.

#### Scenario: Registro de aprobación
- **GIVEN** una reserva aprobada por el coordinador
- **WHEN** se consulta la auditoría
- **THEN** existe un registro con entidad `Reservation`, acción `APROBADA`, actor `Coordinador de espacios` y fecha de la operación

#### Scenario: Registro de cambio de rol
- **GIVEN** un administrador que cambia el rol de un usuario
- **WHEN** se consulta la auditoría
- **THEN** existe un registro con entidad `User`, acción `ROL_CAMBIADO` y el administrador como actor

#### Scenario: Intento de eliminación
- **GIVEN** un registro de auditoría existente
- **WHEN** un usuario intenta eliminarlo
- **THEN** la operación se rechaza con `AUDITORIA_INMUTABLE`

### Requirement: AUD-002 Consulta de auditoría

El sistema MUST permitir al `Administrador del sistema` y al `Coordinador de espacios` consultar la auditoría filtrando por entidad, actor y rango de fechas. El `Solicitante` MUST NO poder consultarla.

#### Scenario: Consulta filtrada por entidad
- **GIVEN** registros de auditoría de `User` y `Reservation`
- **WHEN** el administrador filtra por entidad `Reservation`
- **THEN** solo se devuelven los registros de reservas

#### Scenario: Consulta por rango de fechas
- **GIVEN** registros en fechas distintas
- **WHEN** se consulta un rango determinado
- **THEN** solo se devuelven los registros dentro de ese rango

#### Scenario: Solicitante sin acceso
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** intenta consultar la auditoría
- **THEN** se rechaza con `NO_AUTORIZADO`
