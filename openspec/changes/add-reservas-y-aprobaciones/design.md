# Design

## Context

Los changes anteriores definieron `espacios` y `disponibilidad` y dejaron preparada la extensión `btree_gist`. Ahora se introduce el primer estado mutable del sistema: `reservations`, junto con identidad y permisos. La regla de disponibilidad MUST reutilizarse, no duplicarse. Ver `proposal.md` para la motivación y los specs para el comportamiento observable.

## Goals / Non-Goals

**Goals:**

- Aislar la máquina de estados de reserva en `Domain` para que las transiciones sean explícitas y verificables.
- Garantizar integridad ante concurrencia con una restricción de base de datos como última línea de defensa.
- Mantener los tres roles en español como único modelo de autorización.

**Non-Goals:**

- Envío de correo, auditoría separada y reportes (corresponden al tercer change).
- Integración con LDAP/SSO o reservas recurrentes.

## Decisions

- **ASP.NET Core Identity con autenticación por cookies y roles propios.** El autorregistro crea solo `Solicitante`; los otros dos roles los crea el administrador. Alternativa descartada: OAuth institucional, aún no disponible, y Basic, inseguro.
- **Máquina de estados explícita en `Domain`.** Una tabla de transiciones define qué acción permite pasar de `PENDIENTE` a `APROBADA`/`RECHAZADA`/`CANCELADA` y qué actores pueden ejecutarla. Alternativa descartada: cadenas de estado comparadas con `if` dispersos en controladores.
- **Doble barrera anti-solapes.** Validación en `Application` + `EXCLUDE USING gist (space_id WITH =, tstzrange(inicio, fin, '[)') WITH &&) WHERE estado IN ('PENDIENTE','APROBADA')`. Alternativa descartada: solo `SELECT FOR UPDATE`, que no protege si otra vía escribe fuera del caso de uso.
- **Creación dentro de transacción `SERIALIZABLE` con revalidación de disponibilidad.** La consulta previa es informativa; la confirmación vuelve a comprobar. Alternativa descartada: confiar en la respuesta de la consulta previa.
- **Historial como proyección de eventos de reserva.** `aprobado_por`, `aprobado_en` y `motivo_cancelacion` se guardan en la propia reserva y se exponen con filtros por rol. Alternativa descartada: reconstructrarlo desde correos.

## Risks / Trade-offs

- **[Riesgo] `btree_gist` no disponible en el entorno de pruebas.** → Mitigación: Testcontainers usa imagen PostgreSQL oficial y la migración crea la extensión antes que la restricción.
- **[Riesgo] Error 23514/23P01 de PostgreSQL mal traducido al usuario.** → Mitigación: mapear la violación de `no_overlap` a `SOLAPE_DETECTADO` en la capa de aplicación.
- **[Trade-off] La tabla `reservations` acopla este change con `disponibilidad`.** → Mitigación: `ConsultarDisponibilidad` consume la misma interfaz definida en el change anterior; no se duplica la regla.

## Migration Plan

1. Crear tablas `roles` (semilla con los tres roles en español) y `users` mediante Identity.
2. Crear tabla `reservations` con índice `btree_gist` y restricción `no_overlap`.
3. Insertar usuarios semilla: un `Solicitante`, un `Coordinador de espacios` y un `Administrador del sistema`; Contraseñas temporales a cambiar en primer ingreso.
4. Rollback: eliminar `reservations`, luego `users` y `roles`; mantener `spaces` y la extensión.
