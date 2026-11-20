# Spec Delta

## Purpose

Centraliza el catálogo de laboratorios, auditorios y salas de cómputo para que la institución disponga de una única fuente de verdad sobre capacidad, ubicación, horarios de atención y estado de cada espacio.

## ADDED Requirements

### Requirement: ESP-001 Registro de espacio académico

El sistema MUST permitir registrar un espacio académico con nombre, tipo (`LABORATORIO`, `AUDITORIO` o `SALA_COMPUTO`), capacidad, ubicación, hora de apertura, hora de cierre y estado activo. El nombre MUST ser único, la capacidad MUST ser mayor que cero y la hora de apertura MUST ser anterior a la de cierre.

#### Scenario: Registro válido
- **GIVEN** que no existe un espacio con nombre `Laboratorio A-101`
- **WHEN** se registra `Laboratorio A-101` de tipo `LABORATORIO`, capacidad `30`, apertura `08:00` y cierre `18:00`
- **THEN** el espacio queda creado en estado activo y aparece en el catálogo

#### Scenario: Nombre duplicado
- **GIVEN** que ya existe el espacio `Laboratorio A-101`
- **WHEN** se intenta registrar otro espacio con el mismo nombre
- **THEN** la operación se rechaza con el error `NOMBRE_DUPLICADO` y no se crea ningún registro

#### Scenario: Capacidad inválida
- **GIVEN** un usuario con permisos de administración de espacios
- **WHEN** intenta registrar un espacio con capacidad `0`
- **THEN** la operación se rechaza con el error `CAPACIDAD_INVALIDA`

#### Scenario: Horario inválido
- **GIVEN** un usuario con permisos de administración de espacios
- **WHEN** intenta registrar un espacio cuya hora de apertura es igual o posterior a la de cierre
- **THEN** la operación se rechaza con el error `HORARIO_INVALIDO`

### Requirement: ESP-002 Administración del catálogo

El sistema MUST permitir editar los atributos de un espacio y desactivarlo. Un espacio desactivado MUST dejar de ofrecerse en el catálogo y en la consulta de disponibilidad, pero MUST conservar sus reservas existentes y poder reactivarse posteriormente.

#### Scenario: Edición de capacidad
- **GIVEN** el espacio `Auditorio Central` con capacidad `200`
- **WHEN** se actualiza su capacidad a `180`
- **THEN** el catálogo refleja la nueva capacidad sin afectar reservas previas

#### Scenario: Desactivación con reservas futuras
- **GIVEN** el espacio `Sala de Cómputo 2` tiene una reserva futura en estado `APROBADA`
- **WHEN** se desactiva el espacio
- **THEN** deja de aparecer en el catálogo y en nuevas consultas de disponibilidad, y la reserva existente se mantiene

#### Scenario: Aviso al desactivar con reservas futuras
- **GIVEN** el espacio `Sala de Cómputo 2` tiene una reserva futura en estado `APROBADA`
- **WHEN** se desactiva el espacio desde la interfaz de administración
- **THEN** la interfaz muestra un aviso de que las reservas existentes se conservan, y la desactivación se confirma

#### Scenario: Reactivación
- **GIVEN** un espacio desactivado
- **WHEN** se reactiva
- **THEN** vuelve a aparecer en el catálogo y en las consultas de disponibilidad

### Requirement: ESP-003 Catálogo filtrable

El sistema MUST devolver por defecto solo el catálogo de espacios activos y MUST permitir filtrarlo por tipo, capacidad mínima y estado, indicando para cada espacio su nombre, tipo, capacidad y ubicación. El filtro por estado que incluye espacios inactivos MUST estar disponible únicamente para el `Administrador del sistema` y el `Coordinador de espacios`; para un `Solicitante` MUST devolver solo espacios activos aunque solicite lo contrario.

#### Scenario: Filtro por tipo
- **WHEN** se consulta el catálogo con filtro de tipo `SALA_COMPUTO`
- **THEN** se devuelven únicamente las salas de cómputo activas

#### Scenario: Filtro por capacidad mínima
- **WHEN** se consulta el catálogo con capacidad mínima `30`
- **THEN** se devuelven solo los espacios activos con capacidad mayor o igual a `30`

#### Scenario: Espacio desactivado excluido por defecto
- **GIVEN** un espacio activo y un espacio desactivado
- **WHEN** se consulta el catálogo sin solicitar explícitamente espacios inactivos
- **THEN** solo se devuelve el espacio activo

#### Scenario: Filtro de estado para rol de gestión
- **GIVEN** un espacio activo y un espacio desactivado
- **WHEN** un `Coordinador de espacios` consulta el catálogo con filtro de estado `INACTIVO`
- **THEN** se devuelve el espacio desactivado

#### Scenario: Filtro de estado denegado a solicitante
- **GIVEN** un espacio desactivado
- **WHEN** un `Solicitante` consulta el catálogo con filtro de estado `INACTIVO`
- **THEN** no se devuelve ningún espacio inactivo
