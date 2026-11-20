# ADR-004: Índice de decisiones técnicas de cierre

- **Fecha:** 2026-11-20
- **Estado:** Aceptado
- **Contexto:** Fase 5 (verificación y cierre). Consolidación de las decisiones tomadas durante el desarrollo sin modificar `PLAN_INGENIERIA.md`.

## Contexto

`PLAN_INGENIERIA.md` §6.5 establece que, tras el commit del plan, las decisiones nuevas se registran en `docs/adr/` o en el `design.md` del *change* correspondiente. Hasta el cierre existen tres ADR y siete *design.md* archivados. La Fase 5 requiere un índice único que permita localizar cada desviación respecto al plan y su justificación, sin duplicar el contenido ya aprobado.

## Decisión

Este ADR no sustituye a ADR-001, ADR-002 ni ADR-003; solo los indexa junto con las decisiones posteriores registradas en los `design.md` de los changes archivados.

| Decisión | Fuente de verdad | Motivo |
|---|---|---|
| Precedencia de §4.3 sobre §2.4 y matriz canónica de trazabilidad | `ADR-001-trazabilidad.md` | El plan es inmutable y las dependencias explícitas resuelven la contradicción de orden. |
| Notificar solo `APROBADA`, `RECHAZADA` y `CANCELADA`; `PENDIENTE` no envía correo | `ADR-001-trazabilidad.md` | Evita correos prematuros y respeta el alcance Institucional. |
| Diez patrones de ADR-002, sin MediatR | `ADR-002-patrones-de-diseno.md` | El despachador propio cubre Observer sin añadir dependencias a la stack del plan. |
| `net10.0` con SDK 11 (`rollForward: latestMajor`) y PostgreSQL 19 local en lugar de Testcontainers | `ADR-003-entorno-de-pruebas.md` | No hay Docker en el entorno; se conserva el aislamiento con un esquema por clase de prueba. |
| Credenciales solo en variables de entorno / user-secrets | `ADR-003-entorno-de-pruebas.md` | Ninguna contraseña se versiona ni se muestra en la documentación. |
| Login por formulario HTML y `POST /acceso/autenticar`, con errores por query | `design.md` de `2026-11-20-fix-login-cookie-interactive-server` | `InteractiveServer` iniciaba la respuesta antes de `Set-Cookie`; el endpoint JSON sigue intacto. |
| Cierre de sesión que redirige a `/`, formateador `FormatoFechaHora` en `America/Bogota` y cancelación desde el historial | `design.md` de `2026-11-20-fix-salida-y-historial-reservas` | Reutiliza comandos y endpoints existentes; solo cambia la presentación y el punto de entrada. |
| Límite de cuatro horas como invariante de `Reservation`, con `DURACION_MAXIMA_EXCEDIDA` | `design.md` de `2026-11-20-add-limite-duracion-reserva` | Única fuente de verdad; la cadena de validadores queda reservada a reglas dependientes del contexto. |
| Sin Playwright ni GitHub Actions en este cierre | Decisión de Fase 5 | El plan §5.1 los preveía, pero su incorporación añadiría dependencias y un change nuevo; se registra como desviación pendiente, no se implementa. |
| Sin *change* OpenSpec para la verificación y documentación final | Decisión de Fase 5 | No hay cambio de comportamiento observable; la documentación se versiona directamente. |

## Consecuencias

- `docs/VERIFICACION-FINAL.md` es el índice operativo de resultados; este ADR es el índice de decisiones.
- Los `design.md` archivados siguen siendo la fuente detallada: este ADR solo apunta a ellos.
- `PLAN_INGENIERIA.md` permanece sin modificaciones.
- Cualquier decisión técnica futura requiere un ADR nuevo o un `design.md` de change, conforme a §6.5.
- Los pendientes de verificación no son decisiones: el GAP de prueba del escenario «sin efecto retroactivo» de `RES-005` se registra en `docs/VERIFICACION-FINAL.md` §8 y no aquí.
