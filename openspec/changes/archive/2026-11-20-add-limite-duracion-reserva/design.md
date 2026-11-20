# Design

## Context

Ver `proposal.md` para la motivación. La creación de reservas pasa hoy por
`CrearReserva`, que aplica la cadena `Rango → EspacioActivo → Horario → Capacidad → Solape`
y revalida el solape dentro de una transacción `SERIALIZABLE`. El constructor de
`Reservation` ya comprueba `fin > inicio` y lanza `DomainException(RANGO_INVALIDO)` como
segunda barrera. `DomainErrors` concentra los códigos observables, `SolicitarReserva.razor`
muestra `resultado.ErrorCode` y el endpoint JSON lo traduce con `Error(...)`. Los tres
tipos de `EspacioTipo` son espacios académicos, según ESP-001.

## Goals / Non-Goals

**Goals:**

- Impedir reservas individuales de más de cuatro horas consecutivas en cualquier espacio
  y para cualquier usuario.
- Aceptar exactamente cuatro horas, respetando el intervalo semiabierto `[inicio, fin)`.
- Propagar `DURACION_MAXIMA_EXCEDIDA` por igual a la UI y a la API.
- Mantener una única fuente de verdad para la regla, con regresiones en los niveles ya
  usados por el proyecto.

**Non-Goals:**

- Limitar la suma de varias reservas contiguas del mismo usuario (interpretación
  descartada por la institución).
- Cambiar estados, roles, autorización, disponibilidad, solapes ni la cadena de validación.
- Revisar o cancelar reservas creadas antes del cambio; no hay script de datos.
- Añadir dependencias NuGet, migraciones, secretos ni patrones nuevos a ADR-002.
- Modificar `PLAN_INGENIERIA.md` o los ADR existentes.

## Decisions

1. **Invariante en el dominio, no un eslabón más de la cadena.**
   `Reservation` comprueba en su constructor que `fin - inicio <= TimeSpan.FromHours(4)` y
   lanza `DomainException(DomainErrors.DuracionMaximaExcedida, ...)` cuando se excede. Es
   la única fuente de verdad y cubre cualquier vía de creación (formulario, API JSON,
   bootstrap o un futuro caso de uso). Alternativa descartada: añadir
   `ValidadorDuracionMaxima` a `CadenaValidacionCrearReserva`. ADR-002 reserva Chain of
   Responsibility para validaciones dependientes del contexto (espacio activo, horario,
   aforo, solape); duplicar un invariante del agregado en la cadena y en la entidad crearía
   dos fuentes de verdad y permitiría que una vía alternativa lo eludiera. La cadena
   permanece tal cual.

2. **Código estable nuevo, sin mapeo HTTP especial.**
   `DomainErrors.DuracionMaximaExcedida = "DURACION_MAXIMA_EXCEDIDA"`. `CrearReserva` ya
   captura `DomainException` y devuelve `Result.Failure(ex.Code)`; `SolicitarReserva.razor`
   ya muestra el código y `Error(...)` ya responde para errores de negocio. No se cambia
   ningún componente Razor ni el `ApiEndpoints` existente.

3. **Cómputo sobre instantes UTC.**
   El constructor normaliza `Inicio` y `Fin` a UTC antes de compararlos, de modo que el
   cálculo no depende de la zona de presentación `America/Bogota` ni del `DateTimeOffset`
   original. El límite es de 240 minutos exactos; Colombia no observa horario de verano,
   pero el cómputo en UTC lo hace independiente de cualquier desplazamiento.

4. **Frontera inclusiva en cuatro horas.**
   `fin - inicio == 4 h` se acepta; `fin - inicio > 4 h` se rechaza. Se prueba con
   `10:00–14:00` y `10:00–14:01` en dominio, comando y endpoint, para evitar un off-by-one
   en la regla.

5. **Sin retroactividad.**
   La regla solo se aplica al crear. No se recalcula el historial ni se cancelan reservas
   existentes; por tanto no hay migración, ni invalidación de caché adicional, ni eventos
   de dominio nuevos. `design.md` registra esta decisión conforme a la regla de no
   duplicación de ADR-002 y a §6.5 del plan.

## Risks / Trade-offs

- [La frontera de cuatro horas podría interpretarse como «menos de cuatro»] → El spec y
  las pruebas fijan que exactamente cuatro horas se acepta.
- [Otra vía de creación podría eludir la validación] → Al estar en el constructor de la
  entidad, cualquier `new Reservation(...)` aplica la regla; no depende del handler.
- [El orden de errores podría cambiar] → `CrearReserva` valida primero la cadena y después
  construye la entidad, por lo que `RANGO_INVALIDO` u otros códigos siguen ganando cuando
  aplican; se comprueba en una prueba unitaria.
- [La UI podría no mostrar el código tras el cambio] → No se toca `SolicitarReserva.razor`;
  una prueba bUnit verifica que el código devuelto por el comando llega al elemento
  `data-testid="error"`.

## Migration Plan

No hay cambios de esquema ni de datos. Basta con desplegar la nueva versión de
`ReservaEspacios.Domain`; las reservas existentes conservan su duración y su estado. La
reversión consiste en retirar la comprobación del constructor y el código de error; no hay
migración inversa.

## Open Questions

Ninguna.
