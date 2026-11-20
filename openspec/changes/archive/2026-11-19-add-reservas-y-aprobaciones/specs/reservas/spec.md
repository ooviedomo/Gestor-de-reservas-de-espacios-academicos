# Spec Delta

## Purpose

Gestiona el ciclo de vida operativo de una solicitud de reserva: su creación validada, la garantía de que no existan solapes, la cancelación trazable y el historial consultable según el rol del usuario.

## ADDED Requirements

### Requirement: RES-001 Creación de solicitud de reserva

El sistema MUST permitir a un `Solicitante` autenticado crear una reserva indicando espacio, inicio, fin y propósito, con estado inicial `PENDIENTE`. El inicio MUST ser anterior al fin, el espacio MUST estar activo, la franja MUST estar dentro de su horario y el número de asistentes MUST NO exceder la capacidad.

#### Scenario: Solicitud válida
- **GIVEN** el espacio `Laboratorio A-101` activo y disponible el 20/11/2026 de 10:00 a 11:00
- **WHEN** un `Solicitante` crea una reserva para esa franja con 20 asistentes
- **THEN** se crea la reserva en estado `PENDIENTE`

#### Scenario: Rango horario inválido
- **GIVEN** un formulario de reserva
- **WHEN** se envía un inicio igual o posterior al fin
- **THEN** se rechaza con `RANGO_INVALIDO`

#### Scenario: Asistentes exceden capacidad
- **GIVEN** un espacio con capacidad `30`
- **WHEN** se solicita una reserva con `35` asistentes
- **THEN** se rechaza con `CAPACIDAD_EXCEDIDA`

#### Scenario: Solicitud sin autenticación
- **GIVEN** una petición sin sesión
- **WHEN** intenta crear una reserva
- **THEN** se rechaza con `NO_AUTENTICADO`

#### Scenario: Disponibilidad cambiada antes de confirmar
- **GIVEN** una consulta previa que devolvió `DISPONIBLE`
- **WHEN** otra reserva ocupa la franja antes de confirmar y se envía la creación
- **THEN** la creación revalida y se rechaza con `SOLAPE_DETECTADO`

### Requirement: RES-002 Prevención de reservas simultáneas

El sistema MUST impedir que dos reservas en estado `PENDIENTE` o `APROBADA` del mismo espacio se solapen, incluso cuando las solicitudes llegan en concurrencia. La primera operación válida MUST conservarse y la segunda MUST rechazarse con `SOLAPE_DETECTADO`.

#### Scenario: Solicitudes concurrentes
- **GIVEN** el espacio `Sala de Cómputo 2` sin reservas el 21/11/2026 de 09:00 a 10:00
- **WHEN** dos solicitantes envían reservas simultáneas para esa franja
- **THEN** exactamente una queda `PENDIENTE` y la otra se rechaza con `SOLAPE_DETECTADO`

#### Scenario: Reservas contiguas
- **GIVEN** una reserva `APROBADA` de 10:00 a 11:00
- **WHEN** se crea otra de 11:00 a 12:00 para el mismo espacio
- **THEN** se acepta, porque los intervalos `[10:00,11:00)` y `[11:00,12:00)` no se solapan

#### Scenario: Reserva cancelada no bloquea
- **GIVEN** una reserva de 10:00 a 11:00 en estado `CANCELADA`
- **WHEN** se crea otra reserva para la misma franja
- **THEN** se acepta

### Requirement: RES-003 Cancelación de reservas

El sistema MUST permitir cancelar solo reservas en estado `PENDIENTE` o `APROBADA`, exigiendo un motivo obligatorio. El `Solicitante` MUST poder cancelar únicamente sus propias reservas; el `Coordinador de espacios` y el `Administrador del sistema` pueden cancelar cualquiera de esas reservas.

#### Scenario: Solicitante cancela la suya
- **GIVEN** una reserva `APROBADA` creada por el solicitante autenticado
- **WHEN** la cancela indicando el motivo `Docencia reprogramada`
- **THEN** pasa a `CANCELADA` y se registra el motivo y el actor

#### Scenario: Solicitante intenta cancelar la de otro
- **GIVEN** una reserva creada por otro solicitante
- **WHEN** el solicitante autenticado intenta cancelarla
- **THEN** se rechaza con `NO_AUTORIZADO`

#### Scenario: Cancelación sin motivo
- **GIVEN** una reserva `PENDIENTE`
- **WHEN** se intenta cancelar sin motivo
- **THEN** se rechaza con `MOTIVO_OBLIGATORIO`

#### Scenario: Estado terminal
- **GIVEN** una reserva en estado `RECHAZADA`
- **WHEN** se intenta cancelar
- **THEN** se rechaza con `TRANSICION_INVALIDA`

### Requirement: RES-004 Historial de reservas

El sistema MUST mostrar el historial de reservas con estado, espacio, fechas, solicitante, actor que aprobó, rechazó o canceló —identificado por su nombre y rol—, motivo de rechazo o cancelación y marcas de tiempo. El `Solicitante` MUST ver solo sus reservas; el `Coordinador de espacios` y el `Administrador del sistema` MUST ver todas, con filtros por estado, fecha y espacio.

#### Scenario: Solicitante ve solo las suyas
- **GIVEN** dos reservas de solicitantes distintos
- **WHEN** un `Solicitante` consulta el historial
- **THEN** solo se devuelve su propia reserva

#### Scenario: Coordinador ve todas
- **GIVEN** reservas de varios solicitantes
- **WHEN** un `Coordinador de espacios` consulta el historial
- **THEN** se devuelven todas las reservas

#### Scenario: Filtro por estado
- **GIVEN** reservas en estados `APROBADA` y `CANCELADA`
- **WHEN** se filtra el historial por estado `CANCELADA`
- **THEN** solo se devuelven las reservas canceladas

#### Scenario: Decisión registrada
- **GIVEN** una reserva aprobada por el coordinador
- **WHEN** se consulta su historial
- **THEN** aparecen el actor con su nombre y rol `Coordinador de espacios`, y la fecha de aprobación

#### Scenario: Motivo de rechazo en el historial
- **GIVEN** una reserva rechazada con el motivo `Espacio en mantención`
- **WHEN** se consulta su historial
- **THEN** aparecen el motivo y el actor con su nombre y rol
