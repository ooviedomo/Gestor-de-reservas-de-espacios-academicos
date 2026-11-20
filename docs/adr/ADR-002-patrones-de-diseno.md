# ADR-002: Patrones de diseño adoptados

- **Fecha:** 2026-11-19
- **Estado:** Aceptado
- **Contexto:** Etapa 2 / Fase 2 (revisión de especificaciones), después de `ADR-001`.

## Contexto

El plan define una arquitectura de monolito modular en capas (§3.1) pero no nombra patrones de diseño. El estudiante propuso una lista de 22 patrones clásicos y pidió una selección fundamentada antes de implementar. Conforme a §6.5 del plan, la decisión se registra aquí y se refleja en el `design.md` de cada change, sin editar `PLAN_INGENIERIA.md`.

## Decisión

Se adoptan **10 patrones** (niveles 1 y 2 de la recomendación). Se descartan los demás por no aportar valor al dominio o por ya estar cubiertos por el framework.

### Patrones adoptados

| Patrón | Componente concreto | Capa | Change | Requisito vinculado |
|---|---|---|---|---|
| **State** | `Reservation` + `PendienteState`, `AprobadaState`, `RechazadaState`, `CompletadaState`, `CanceladaState` | Domain | 2 | `APR-001`, `RES-003`, `COM-001` |
| **Strategy** | `IValidacionPorTipoEspacio`, `IEvaluadorPermisos`, `IRetryBackoff`, `ICalculoOcupacion` | Domain / Application | 2, 3 | `RES-001`, `AUTH-002`, `NOT-002`, `REP-003` |
| **Chain of Responsibility** | Cadena de validadores de `CrearReserva` (`Rango → EspacioActivo → Horario → Capacidad → Solape`) | Application | 2 | `RES-001` |
| **Command** | `ICommand` / `ICommandHandler<T>`; `EnviarCorreoCommand` persistido en el outbox | Application | 2, 3 | `NOT-001` |
| **Builder** | `FiltroCatalogoBuilder`, `FiltroHistorialBuilder`, `ReporteBuilder` y builders de datos en pruebas | Application / Tests | 1, 2, 3 | `ESP-003`, `RES-004`, `REP-001` |
| **Observer** | `IDomainEventDispatcher` y suscriptores de auditoría y outbox | Domain / Application | 2, 3 | `AUD-001`, `NOT-001` |
| **Adapter** | `SmtpEmailSender : IEmailSender`; `IdentityRoleAdapter` | Infrastructure | 2, 3 | `NOT-002`, A-1 de `ADR-001` |
| **Decorator** | `DisponibilidadCacheDecorator`, `LoggingCommandHandler<T>` | Application / Infrastructure | 1, 3 | `DISP-001`, `NOT-002` |
| **Template Method** | `TransicionStateBase` con el esqueleto `validar actor → verificar transición → aplicar → registrar fecha` | Domain | 2 | `APR-001`, `COM-001` |
| **Facade** | `IReservaEspaciosFacade` para componentes Razor y Minimal APIs | Application | 1, 2, 3 | Todos los casos de uso |

### Patrones no adoptados

| Patrón | Motivo |
|---|---|
| Factory Method | El contenedor DI y los `ICommandHandler<T>` cubren la creación; no hay jerarquía polimórfica de fábricas. |
| Abstract Factory | No hay familias de objetos relacionados que varíen juntas. |
| Prototype | No se clonan entidades complejas. |
| Singleton | DI gestiona los ciclos de vida; un singleton manual rompería la testabilidad. |
| Bridge | No hay dos jerarquías ortogonales que separar. |
| Composite | Los permisos son planos (tres roles), no un árbol. |
| Flyweight | No hay gran cantidad de objetos pequeños que compartir. |
| Memento | No hay "deshacer"; las transacciones de EF Core cubren el rollback. |
| Visitor | No hay una estructura de objetos estable con muchas operaciones distintas. |
| Mediator | Se considera la misma intención que Observer; se implementa un `IDomainEventDispatcher` propio sin añadir MediatR a la stack del plan §3.2. Adoptar MediatR exigiría un ADR nuevo. |
| Proxy | EF Core ya incluye *lazy-loading proxies*; no se escribe uno a mano. |
| Iterator | `IEnumerable` / `yield` es implícito en C# para recorrer historial y reportes. |

### Regla de no duplicación

Tres patrones tienen formas similares y se usan para propósitos distintos: **Chain of Responsibility** para las validaciones secuenciales de `RES-001`, **Decorator** para preocupaciones transversales (caché y logging) y **Template Method** solo como esqueleto de las clases de **State**. **Observer** reacciona a eventos de dominio, no valida ni envuelve casos de uso.

## Consecuencias

- Los `design.md` de los tres changes nombran los patrones que les corresponden y las `tasks.md` incluyen su verificación.
- La fase de implementación (`openspec-apply-change`) aplica estos patrones; **no se añaden dependencias nuevas** a la stack del plan.
- Toda desviación futura se registra aquí o en el `design.md` del change afectado, nunca editando `PLAN_INGENIERIA.md`.
