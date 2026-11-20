# Spec Delta

## ADDED Requirements

### Requirement: RES-005 Límite de duración consecutiva

El sistema MUST rechazar cualquier solicitud de reserva cuya duración `[inicio, fin)`
supere cuatro horas consecutivas. Una duración de exactamente cuatro horas MUST
aceptarse. El límite MUST aplicarse a todos los tipos de espacio académico y a todos los
usuarios, y el rechazo MUST usar el código estable `DURACION_MAXIMA_EXCEDIDA`. La regla
MUST NO alterar reservas ya creadas ni la semántica del intervalo semiabierto.

#### Scenario: Cuatro horas exactas
- **GIVEN** el espacio `Laboratorio A-101` activo y disponible
- **WHEN** un `Solicitante` solicita una reserva de 10:00 a 14:00
- **THEN** se crea en estado `PENDIENTE`

#### Scenario: Más de cuatro horas
- **GIVEN** el mismo espacio activo y disponible
- **WHEN** un `Solicitante` solicita una reserva de 10:00 a 14:01
- **THEN** se rechaza con `DURACION_MAXIMA_EXCEDIDA`

#### Scenario: Límite aplicado a todos los espacios
- **GIVEN** espacios de tipo `Laboratorio`, `Auditorio` y `SalaComputo`
- **WHEN** se solicita en cualquiera de ellos una reserva de más de cuatro horas
- **THEN** se rechaza con `DURACION_MAXIMA_EXCEDIDA`

#### Scenario: Sin efecto retroactivo
- **GIVEN** una reserva creada antes de esta regla con duración superior a cuatro horas
- **WHEN** se consulta su historial
- **THEN** se muestra sin cambios y no se cancela automáticamente
