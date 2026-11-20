# Proposal

## Why

Durante la fase de gestión de cambios la institución solicitó una nueva regla de negocio:
ningún usuario podrá reservar un espacio académico durante más de cuatro horas
consecutivas. Hoy `CrearReserva` valida rango, espacio activo, horario, aforo y solape, de
modo que una solicitud de 10:00 a 15:00 se acepta. La regla debe quedar como invariante del
agregado y propagarse con un código estable a la UI y a la API.

## What Changes

- Añadir el código estable `DURACION_MAXIMA_EXCEDIDA` entre los errores observables.
- Rechazar en el dominio toda reserva cuya duración `[inicio, fin)` supere cuatro horas;
  una duración de exactamente cuatro horas MUST aceptarse.
- Aplicar el límite a todos los tipos de espacio académico y a todos los usuarios, sin
  efecto retroactivo sobre reservas ya creadas y sin migración de datos.
- Registrar la decisión en `design.md`; no se modifica `PLAN_INGENIERIA.md` ni ningún ADR.
- Añadir regresiones unitarias, de integración y bUnit, y actualizar la trazabilidad y el
  registro de uso de IA.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `reservas`: se añade `RES-005 Límite de duración consecutiva` como requisito observable
  de la creación de reservas. `RES-001` y sus escenarios permanecen intactos.

## Impact

- `src/ReservaEspacios.Domain/Common/DomainErrors.cs`: nuevo código estable.
- `src/ReservaEspacios.Domain/Reservas/Reservation.cs`: invariante de cuatro horas en el
  constructor, junto a la validación de rango existente.
- Pruebas unitarias, de integración y bUnit en `tests/`.
- `docs/TRAZABILIDAD-RESERVAS-APROBACIONES.md` y `docs/REGISTRO-USO-IA.md`.
- Sin cambios en Application, Web, base de datos, dependencias NuGet ni secretos. Sin
  nuevos patrones de diseño respecto a ADR-002.
