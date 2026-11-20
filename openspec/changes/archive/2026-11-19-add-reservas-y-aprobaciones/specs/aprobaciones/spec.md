# Spec Delta

## Purpose

Formaliza quién puede aprobar o rechazar una solicitud y qué información debe quedar registrada, de modo que cada decisión sea auditable y coherente con la matriz de estados definida en el plan de ingeniería.

## ADDED Requirements

### Requirement: APR-001 Aprobación y rechazo de solicitudes

El sistema MUST permitir al `Coordinador de espacios` y al `Administrador del sistema` aprobar o rechazar reservas en estado `PENDIENTE`. Al aprobar MUST registrar `aprobado_por` y `aprobado_en` y pasar a `APROBADA`; al rechazar MUST exigir motivo y pasar a `RECHAZADA`. Ningún otro estado MUST ser transicionable mediante estas acciones.

#### Scenario: Aprobar solicitud pendiente
- **GIVEN** una reserva en estado `PENDIENTE`
- **WHEN** el `Coordinador de espacios` la aprueba
- **THEN** pasa a `APROBADA` y quedan registrados el actor y la fecha

#### Scenario: Rechazar con motivo
- **GIVEN** una reserva en estado `PENDIENTE`
- **WHEN** el `Administrador del sistema` la rechaza indicando `Espacio en mantención`
- **THEN** pasa a `RECHAZADA` y el motivo queda disponible en el historial

#### Scenario: Rechazo sin motivo
- **GIVEN** una reserva en estado `PENDIENTE`
- **WHEN** se intenta rechazar sin motivo
- **THEN** se rechaza con `MOTIVO_OBLIGATORIO`

#### Scenario: Transición inválida
- **GIVEN** una reserva en estado `APROBADA`
- **WHEN** se intenta aprobar nuevamente
- **THEN** se rechaza con `TRANSICION_INVALIDA`

#### Scenario: Solicitante intenta aprobar
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** intenta aprobar una reserva `PENDIENTE`
- **THEN** se rechaza con `NO_AUTORIZADO`

### Requirement: COM-001 Completado de reservas

El sistema MUST permitir al `Coordinador de espacios` y al `Administrador del sistema` completar reservas en estado `APROBADA`, pasándolas a `COMPLETADA`. El sistema MUST completar automáticamente una reserva `APROBADA` cuando su fecha `fin` haya vencido. Ningún otro estado MUST ser transicionable a `COMPLETADA`.

#### Scenario: Completado manual
- **GIVEN** una reserva en estado `APROBADA`
- **WHEN** el `Coordinador de espacios` la completa
- **THEN** pasa a `COMPLETADA` y queda registrada la marca de tiempo de completado

#### Scenario: Completado automático al vencer
- **GIVEN** una reserva en estado `APROBADA` cuya fecha `fin` ya pasó
- **WHEN** se ejecuta el proceso de cierre diario
- **THEN** la reserva pasa a `COMPLETADA` sin intervención de un actor

#### Scenario: Completado desde estado no aprobado
- **GIVEN** una reserva en estado `PENDIENTE`
- **WHEN** se intenta completar
- **THEN** la operación se rechaza con `TRANSICION_INVALIDA`

#### Scenario: Solicitante intenta completar
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** intenta completar una reserva `APROBADA`
- **THEN** la operación se rechaza con `NO_AUTORIZADO`
