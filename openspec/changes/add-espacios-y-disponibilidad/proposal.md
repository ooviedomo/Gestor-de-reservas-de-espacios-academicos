# Proposal

## Why

La institución no cuenta con un catálogo confiable de laboratorios, auditorios y salas de cómputo, por lo que la disponibilidad se consulta en hojas de cálculo desactualizadas. Sin una fuente única de verdad para los espacios y sus horarios, cualquier consulta de reservas es ambigua y aparecen conflictos antes de que exista una solicitud formal.

## What Changes

- Registrar espacios académicos con nombre, tipo (`LABORATORIO`, `AUDITORIO`, `SALA_COMPUTO`), capacidad, ubicación, horario de atención y estado activo/inactivo.
- Permitir editar y desactivar espacios; un espacio desactivado deja de ofrecerse en el catálogo pero conserva sus reservas existentes.
- Exponer un catálogo filtrable por tipo, capacidad mínima y estado.
- Consultar disponibilidad por fecha y rango horario, considerando ocupantes solo las reservas en estado `PENDIENTE` o `APROBADA`, con intervalos semiabiertos `[inicio, fin)`.
- Establecer los requisitos y escenarios de aceptación que el cambio posterior de reservas reutilizará.

Fuera de alcance de este change: alta de usuarios, roles, creación de reservas, aprobación/rechazo, cancelación, notificaciones y reportes.

## Capabilities

### New Capabilities

- `espacios`: Registro, administración y consulta del catálogo de espacios académicos.
- `disponibilidad`: Consulta de franjas libres u ocupadas a partir del catálogo y del estado de las reservas.

### Modified Capabilities

- Ninguna. El proyecto aún no tiene specs publicadas.

## Impact

- Proyectos afectados: `ReservaEspacios.Domain`, `ReservaEspacios.Application`, `ReservaEspacios.Infrastructure` y `ReservaEspacios.Web`.
- Datos: nueva tabla `spaces` y contrato de lectura para la consulta de disponibilidad.
- Dependencias: Entity Framework Core + Npgsql sobre PostgreSQL; extensión `btree_gist` requerida más adelante por la restricción de solapes.
- Sin impacto en autenticación, roles ni flujo de aprobación.
