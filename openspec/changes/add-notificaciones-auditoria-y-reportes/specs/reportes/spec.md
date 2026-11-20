# Spec Delta

## Purpose

Proporciona a la coordinación y a la administración una vista agregada del uso real de los espacios en un período, derivada de las reservas confirmadas, para apoyar decisiones de asignación y mantenimiento.

## ADDED Requirements

### Requirement: REP-001 Reporte de ocupación por espacio y período

El sistema MUST generar un reporte de ocupación agrupado por espacio para un rango de fechas, mostrando número de reservas, horas reservadas y porcentaje de ocupación sobre el horario de atención disponible. Solo MUST contabilizarse reservas en estado `APROBADA` o `COMPLETADA`.

#### Scenario: Reporte por espacio
- **GIVEN** reservas `APROBADAS` en `Laboratorio A-101` y `Auditorio Central` durante noviembre
- **WHEN** se genera el reporte filtrando por `Laboratorio A-101`
- **THEN** solo se devuelven las métricas de ese laboratorio

#### Scenario: Reservas canceladas excluidas
- **GIVEN** una reserva `CANCELADA` y una `APROBADA` en el mismo espacio
- **WHEN** se genera el reporte
- **THEN** solo la reserva `APROBADA` contribuye a las horas y al porcentaje de ocupación

#### Scenario: Rango de fechas
- **GIVEN** reservas en octubre y noviembre
- **WHEN** se solicita el reporte solo para noviembre
- **THEN** no se incluyen las reservas de octubre

### Requirement: REP-002 Acceso restringido al reporte

El sistema MUST permitir consultar el reporte de ocupación únicamente al `Coordinador de espacios` y al `Administrador del sistema`, aplicando la matriz de permisos definida en `autorizacion`.

#### Scenario: Coordinador consulta el reporte
- **GIVEN** un usuario autenticado como `Coordinador de espacios`
- **WHEN** solicita el reporte de ocupación
- **THEN** se le devuelven las métricas de los espacios

#### Scenario: Solicitante intenta consultar el reporte
- **GIVEN** un usuario autenticado como `Solicitante`
- **WHEN** solicita el reporte de ocupación
- **THEN** se rechaza con `NO_AUTORIZADO`
