# Spec Delta

## Purpose

Asegura que los cambios de estado de una reserva se comuniquen al solicitante sin que un fallo del servicio de correo afecte la operación que ya se confirmó, mediante un registro persistente de mensajes pendientes y reintentos controlados.

## ADDED Requirements

### Requirement: NOT-001 Registro de notificación pendiente

El sistema MUST crear un registro en `notifications` con canal `CORREO`, estado `PENDIENTE` y destinatario el solicitante, cada vez que una reserva cambia a `APROBADA`, `RECHAZADA` o `CANCELADA`. El registro MUST vincularse a la reserva que originó el evento.

#### Scenario: Aprobación genera notificación
- **GIVEN** una reserva `PENDIENTE` del solicitante `ana@universidad.edu`
- **WHEN** el coordinador la aprueba
- **THEN** se crea una notificación `PENDIENTE` con destino `ana@universidad.edu` asociada a esa reserva

#### Scenario: Rechazo genera notificación
- **GIVEN** una reserva `PENDIENTE`
- **WHEN** se rechaza con motivo `Espacio en mantención`
- **THEN** se crea una notificación `PENDIENTE` cuyo contenido incluye el motivo del rechazo

#### Scenario: Cancelación genera notificación
- **GIVEN** una reserva `APROBADA`
- **WHEN** se cancela
- **THEN** se crea una notificación `PENDIENTE` para el solicitante

#### Scenario: Creación en PENDIENTE no genera notificación
- **GIVEN** un solicitante que crea una reserva
- **WHEN** la reserva queda en estado `PENDIENTE`
- **THEN** no se crea ninguna notificación; se notifican solo las transiciones a `APROBADA`, `RECHAZADA` o `CANCELADA`

### Requirement: NOT-002 Envío y reintentos

El sistema MUST intentar enviar las notificaciones `PENDIENTES`, marcarlas como `ENVIADA` cuando el proveedor confirme el envío e incrementar `intentos` manteniéndolas `PENDIENTE` cuando falle, hasta un máximo de 5 intentos, tras los cuales pasan a `ERROR`.

#### Scenario: Envío exitoso
- **GIVEN** una notificación `PENDIENTE`
- **WHEN** el proveedor de correo confirma el envío
- **THEN** la notificación pasa a `ENVIADA` y registra la fecha de envío

#### Scenario: Fallo temporal
- **GIVEN** una notificación con `intentos = 0`
- **WHEN** el proveedor falla una vez
- **THEN** la notificación sigue `PENDIENTE`, con `intentos = 1` y el error registrado en `ultimo_error`

#### Scenario: Reintentos agotados
- **GIVEN** una notificación con `intentos = 4`
- **WHEN** el proveedor falla nuevamente
- **THEN** la notificación pasa a `ERROR` y deja de reintentarse

### Requirement: NOT-003 Aislamiento de fallos

El sistema MUST NO revertir, modificar ni impedir una transición de estado de reserva cuando el envío de la notificación falla. El registro de la notificación (outbox) MUST insertarse en la misma transacción que el cambio de estado, de modo que ambos persistan o ambos fallen; el envío y sus reintentos MUST ejecutarse de forma independiente, en un proceso aparte.

#### Scenario: Proveedor de correo caído
- **GIVEN** el proveedor de correo no responde
- **WHEN** se aprueba una reserva `PENDIENTE`
- **THEN** la reserva queda `APROBADA` y la notificación permanece `PENDIENTE` para reintentarse después
