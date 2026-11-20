# ADR-001: Trazabilidad de requisitos y precedencia entre secciones del plan

- **Fecha:** 2026-11-19
- **Estado:** Aceptado
- **Contexto:** Fase 2 de la Etapa 2 (revisión de especificaciones).

## Contexto

`PLAN_INGENIERIA.md` §5.3 define una matriz de trazabilidad con rutas como `specs/spaces/spec.md`, `specs/reservations/spec.md`, `specs/auth/spec.md` y `specs/approvals/spec.md`. Los archivos reales de OpenSpec viven en `openspec/changes/<change>/specs/<capacidad>/spec.md` y usan otras capacidades (`espacios`, `disponibilidad`, `usuarios`, `autorizacion`, `reservas`, `aprobaciones`, `notificaciones`, `auditoria`, `reportes`). El plan además se declaró inmutable tras su commit (§6.5), por lo que la corrección no puede hacerse editándolo.

También hay una contradicción interna: §2.4 ordena la creación de reservas antes que los usuarios, mientras §4.3 exige usuarios y roles antes de reservas.

## Decisión

1. La matriz canónica de trazabilidad es la siguiente; sustituye a la de §5.3 sin modificar el plan:

| ID del plan | Requisito | Capability / requisito OpenSpec | Change |
|---|---|---|---|
| REQ-01 | Consultar disponibilidad | `disponibilidad` → `DISP-001`, `DISP-002`, `DISP-003` | `add-espacios-y-disponibilidad` |
| REQ-02 | Evitar solapes | `reservas` → `RES-001`, `RES-002` | `add-reservas-y-aprobaciones` |
| REQ-03 | Aprobar/rechazar según la matriz de estados | `aprobaciones` → `APR-001`, `COM-001` | `add-reservas-y-aprobaciones` |
| REQ-04 | Restringir acceso según los 3 roles | `autorizacion` → `AUTH-001`, `AUTH-002`, `AUTH-003` | `add-reservas-y-aprobaciones` |
| REQ-05 | Registrar auditoría y notificar cambios | `auditoria` → `AUD-001`, `AUD-002`; `notificaciones` → `NOT-001`…`NOT-003` | `add-notificaciones-auditoria-y-reportes` |
| (sin ID en el plan) | Reporte de ocupación | `reportes` → `REP-001`…`REP-003` | `add-notificaciones-auditoria-y-reportes` |
| (sin ID en el plan) | Catálogo de espacios | `espacios` → `ESP-001`…`ESP-003` | `add-espacios-y-disponibilidad` |
| (sin ID en el plan) | Usuarios y autenticación | `usuarios` → `USR-001`…`USR-003` | `add-reservas-y-aprobaciones` |

2. Cuando el plan se contradiga, prevalece la sección con dependencias explícitas: §4.3 sobre §2.4. El orden de implementación es usuarios/roles → reservas → aprobaciones → notificaciones/auditoría → reportes.

3. "Notificar sobre el estado" (§1.2) se interpreta como notificar los cambios a `APROBADA`, `RECHAZADA` y `CANCELADA`; la creación en `PENDIENTE` no genera correo.

## Consecuencias

- Los IDs `REQ-*` del plan siguen siendo válidos como referencia documental, pero su archivo real se resuelve con esta tabla.
- Los mensajes de commit pueden usar el ID de requisito OpenSpec (p. ej. `feat(RES-002): ...`), más preciso que `REQ-02`.
- Toda nueva decisión que contradiga el plan se registra aquí o en el `design.md` del change, nunca editando `PLAN_INGENIERIA.md`.
