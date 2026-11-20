# Design

## Context

Los changes anteriores definieron `espacios` y `disponibilidad` y dejaron preparada la extensión `btree_gist`. Ahora se introduce el primer estado mutable del sistema: `reservations`, junto con identidad y permisos. La regla de disponibilidad MUST reutilizarse, no duplicarse. Ver `proposal.md` para la motivación y los specs para el comportamiento observable.

Nota de orden: el plan §2.4 lista la creación de reservas antes que los usuarios, pero su §4.3 exige usuarios y roles antes de reservas. Se sigue §4.3 (users → reservations → aprobaciones), que es además el orden de dependencias real y el que usa `tasks.md`. Esta precedencia quedó registrada en `docs/adr/ADR-001-trazabilidad.md`.

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
- **Códigos de rol como clave persistida y nombres en español como etiqueta.** La tabla `roles` guarda `SOLICITANTE`, `COORDINADOR` y `ADMINISTRADOR` (según el plan §1 y §3.3); los specs y la UI muestran `Solicitante`, `Coordinador de espacios` y `Administrador del sistema`. Alternativa descartada: persistir la etiqueta, que cambiaría con traducciones y rompería los guards.
- **Máquina de estados explícita en `Domain`.** Una tabla de transiciones define `PENDIENTE → APROBADA/RECHAZADA/CANCELADA`, `APROBADA → COMPLETADA/CANCELADA` y los estados terminales `RECHAZADA`/`CANCELADA`/`COMPLETADA`, con los actores permitidos. El completado puede ser manual (Coordinador/Administrador, `COM-001`) o automático al vencer `fin` mediante un proceso periódico. Alternativa descartada: cadenas de estado comparadas con `if` dispersos en controladores.
- **Un único código de error para transiciones inválidas.** Toda transición no permitida, incluida la cancelación de un estado terminal, devuelve `TRANSICION_INVALIDA`. Alternativa descartada: `ESTADO_TERMINAL`, que duplicaba el significado y obligaba a la UI a distinguir dos casos idénticos.
- **Doble barrera anti-solapes.** Validación en `Application` + `EXCLUDE USING gist (space_id WITH =, tstzrange(inicio, fin, '[)') WITH &&) WHERE estado IN ('PENDIENTE','APROBADA')`. Alternativa descartada: solo `SELECT FOR UPDATE`, que no protege si otra vía escribe fuera del caso de uso.
- **Creación dentro de transacción `SERIALIZABLE` con revalidación de disponibilidad.** La consulta previa es informativa; la confirmación vuelve a comprobar. Alternativa descartada: confiar en la respuesta de la consulta previa.
- **Historial como proyección de eventos de reserva.** `aprobado_por`, `aprobado_en` y `motivo_cancelacion` se guardan en la propia reserva y se exponen con filtros por rol. Alternativa descartada: reconstructrarlo desde correos.
- **Extensiones del esquema del plan.** Además de las columnas de `reservations` en el plan §3.3 se añaden `asistentes` (validado contra `spaces.capacidad` en `RES-001`), `motivo_rechazo` (exigido en `APR-001` y visible en `RES-004`) y `completado_en` (para `COM-001`). El actor se persiste como `actor_id` y se muestra como `nombre + rol` en historial y auditoría; el email solo se usa como destinatario de notificaciones. Alternativa descartada: guardar el nombre del actor como texto libre, que quedaría desactualizado tras un cambio de rol.

## Risks / Trade-offs

- **[Riesgo] `btree_gist` no disponible en el entorno de pruebas.** → Mitigación: Testcontainers usa imagen PostgreSQL oficial y la migración crea la extensión antes que la restricción.
- **[Riesgo] Error 23514/23P01 de PostgreSQL mal traducido al usuario.** → Mitigación: mapear la violación de `no_overlap` a `SOLAPE_DETECTADO` en la capa de aplicación.
- **[Trade-off] La tabla `reservations` acopla este change con `disponibilidad`.** → Mitigación: `ConsultarDisponibilidad` consume la misma interfaz definida en el change anterior; no se duplica la regla.

## Migration Plan

1. Crear tablas `roles` (semilla con los códigos `SOLICITANTE`, `COORDINADOR`, `ADMINISTRADOR` y sus etiquetas en español) y `users` mediante Identity.
2. Crear tabla `reservations` con índice `btree_gist`, restricción `no_overlap` y las columnas adicionales `asistentes`, `motivo_rechazo` y `completado_en`.
3. Insertar usuarios semilla: un `Solicitante`, un `Coordinador de espacios` y un `Administrador del sistema`; contraseñas temporales a cambiar en primer ingreso.
4. Registrar el proceso periódico que completa automáticamente las reservas `APROBADA` vencidas (`COM-001`).
5. Rollback: eliminar `reservations`, luego `users` y `roles`; mantener `spaces` y la extensión.
