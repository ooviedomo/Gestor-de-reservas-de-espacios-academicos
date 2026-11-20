# Design

## Context

El proyecto `ReservaEspacios` es un monolito modular en C# con Blazor, ASP.NET Core, Entity Framework Core y PostgreSQL, según lo definido en `PLAN_INGENIERIA.md`. Este change es el primero de tres y no existe aún código ni specs publicadas. Las reglas de usuarios, roles y reservas se especificarán en cambios posteriores; aquí solo se define la base de catálogo y disponibilidad que esos cambios consumirán.

## Goals / Non-Goals

**Goals:**

- Modelar el catálogo de espacios con integridad en la base de datos.
- Definir una única regla de disponibilidad reutilizable por la futura creación de reservas.
- Dejar preparada la extensión `btree_gist` que el siguiente change usará para la restricción anti-solape.

**Non-Goals:**

- Autenticación, roles y permisos (se especifican en `add-reservas-y-aprobaciones`).
- Creación, aprobación o cancelación de reservas.
- Notificaciones, auditoría y reportes.

## Decisions

- **Monolito modular en capas (`Domain` / `Application` / `Infrastructure` / `Web`).** Las reglas de disponibilidad viven en `Domain` y se exponen como caso de uso en `Application`. Alternativa descartada: microservicios, innecesaria para el alcance y con costo de consistencia distribuida.
- **PostgreSQL + EF Core con Npgsql.** El tipo `spaces` se mapea con `hora_apertura`/`hora_cierre` como `time` y los horarios de reserva futuros como `tstzrange`. Alternativa descartada: MongoDB, que no ofrece la integridad ni los tipos de rango necesarios para evitar solapes.
- **Unicidad de nombre y validación de horario en dominio más restricciones en BD.** El índice único de `spaces.nombre` y un `CHECK (hora_apertura < hora_cierre)` son la última línea de defensa. Alternativa descartada: validar solo en la interfaz Blazor.
- **Intervalos semiabiertos `[inicio, fin)` y `DateTimeOffset` en UTC.** Las horas de atención se almacenan como `time` y se interpretan en la zona institucional (`America/Bogota`); antes de compararlas con una franja se convierten a UTC aplicando la zona de la fecha consultada, de modo que un horario de verano no desplace el límite. La conversión a la zona institucional para **visualización** ocurre en la presentación; para **comparación** ocurre en `Domain`. Alternativa descartada: `DateTime` sin offset, ambiguo ante cambios de horario.
- **Disponibilidad calculada a partir del estado de reservas, aunque las reservas aún no existan en este change.** El caso de uso consulta `reservations` con estado `PENDIENTE` o `APROBADA`; la tabla se creará en el siguiente change, por lo que la implementación deberá dejar el repositorio detrás de una interfaz y devolver `DISPONIBLE` mientras no haya tabla. Alternativa descartada: duplicar un flag de ocupación en `spaces`, que se desincronizaría.

## Risks / Trade-offs

- **[Riesgo] La disponibilidad cambia entre la consulta y la creación de una reserva.** → Mitigación: el siguiente change MUST revalidar disponibilidad dentro de la transacción de creación; la consulta de este change es informativa.
- **[Riesgo] Desactivar un espacio con reservas futuras puede confundir a los usuarios.** → Mitigación: la spec obliga a conservar las reservas y a ocultar el espacio solo de nuevas consultas; se mostrará un aviso en la UI.
- **[Trade-off] Consultar reservas desde este change acopla `disponibilidad` con un dato aún no implementado.** → Mitigación: se aísla detrás de `IDisponibilidadRepository` y se prueba con un stub hasta que exista `reservations`.

## Migration Plan

1. Ejecutar `CREATE EXTENSION IF NOT EXISTS btree_gist`.
2. Crear la migración de EF Core para `spaces` con índice único en `nombre` y `CHECK` de horario.
3. Desplegar sin datos previos; no hay migración de hojas de cálculo en este change.
4. Rollback: eliminar la migración de `spaces` (la extensión se conserva porque la usa el siguiente change).
