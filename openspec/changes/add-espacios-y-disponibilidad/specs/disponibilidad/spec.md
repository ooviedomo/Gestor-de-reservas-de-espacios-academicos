# Spec Delta

## Purpose

Permite consultar si un espacio académico está libre en una fecha y franja horaria concretas, usando las reservas que realmente bloquean el calendario y los horarios de atención del espacio como reglas de decisión.

## ADDED Requirements

### Requirement: DISP-001 Consulta de disponibilidad por franja

El sistema MUST permitir consultar la disponibilidad de un espacio para una fecha y un rango `[inicio, fin)`, devolviendo `DISPONIBLE` o `NO_DISPONIBLE` junto con el motivo. Un espacio inactivo MUST devolver `ESPACIO_INACTIVO` y una franja fuera del horario de atención MUST devolver `FUERA_DE_HORARIO`.

#### Scenario: Espacio libre
- **GIVEN** el espacio `Laboratorio A-101` activo, con horario `08:00–18:00` y sin reservas bloqueantes el 20/11/2026
- **WHEN** se consulta la franja `10:00–11:00`
- **THEN** el resultado es `DISPONIBLE`

#### Scenario: Espacio inactivo
- **GIVEN** un espacio desactivado
- **WHEN** se consulta cualquier franja
- **THEN** el resultado es `NO_DISPONIBLE` con motivo `ESPACIO_INACTIVO`

#### Scenario: Franja fuera del horario de atención
- **GIVEN** un espacio con horario de atención `08:00–18:00`
- **WHEN** se consulta la franja `19:00–20:00`
- **THEN** el resultado es `NO_DISPONIBLE` con motivo `FUERA_DE_HORARIO`

### Requirement: DISP-002 Reglas de ocupación de la franja

El sistema MUST considerar como ocupante únicamente una reserva en estado `PENDIENTE` o `APROBADA`, y MUST interpretar los rangos como semiabiertos `[inicio, fin)`, de modo que el fin de una reserva no se solape con el inicio de otra.

#### Scenario: Reserva pendiente bloquea
- **GIVEN** una reserva en estado `PENDIENTE` para `Laboratorio A-101` de `10:00` a `11:00`
- **WHEN** se consulta la franja `10:30–11:30`
- **THEN** el resultado es `NO_DISPONIBLE` con motivo `SOLAPE_CON_RESERVA`

#### Scenario: Reserva rechazada o cancelada no bloquea
- **GIVEN** una reserva de `10:00` a `11:00` en estado `CANCELADA`
- **WHEN** se consulta la franja `10:00–11:00`
- **THEN** el resultado es `DISPONIBLE`

#### Scenario: Reservas contiguas
- **GIVEN** una reserva `APROBADA` de `10:00` a `11:00`
- **WHEN** se consulta la franja `11:00–12:00`
- **THEN** el resultado es `DISPONIBLE`, porque `[10:00,11:00)` y `[11:00,12:00)` no se solapan

#### Scenario: Solape parcial
- **GIVEN** una reserva `APROBADA` de `10:00` a `11:00`
- **WHEN** se consulta la franja `09:30–10:30`
- **THEN** el resultado es `NO_DISPONIBLE` con motivo `SOLAPE_CON_RESERVA`
