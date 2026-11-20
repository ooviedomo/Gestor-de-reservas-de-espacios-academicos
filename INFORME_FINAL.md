# Informe final — ReservaEspacios

- **Fecha:** 2026-11-20
- **Alcance:** planes 0–5 (planificación, especificación, núcleo de reservas, acceso y aprobación, notificaciones/auditoría/reportes, verificación y cierre)
- **Metodología:** iterativa e incremental con **SDD (Spec-Driven Development) mediante OpenSpec**
- **Anexo técnico:** [`docs/VERIFICACION-FINAL.md`](docs/VERIFICACION-FINAL.md) contiene comandos, evidencia de cobertura y la matriz completa de 33 requisitos

## 1. Análisis de resultados

La aplicación quedó funcional y verificada sobre el stack previsto: Blazor Web App (ASP.NET Core 10), EF Core 10 + Npgsql y PostgreSQL local. Las reglas de negocio están en `Domain`/`Application` y la capa `Web` solo las expone.

| Indicador | Resultado |
|---|---|
| Pruebas automatizadas | **374** superadas (215 unitarias, 100 integración, 59 bUnit), 0 fallidas, 0 omitidas |
| Cobertura de líneas | **95.2%** (5210/5467) |
| Cobertura de ramas | **79.5%** (729/916) |
| Cobertura de métodos | **96.9%** (678/699) |
| Compilación / formato | 0 advertencias, 0 errores / sin cambios |
| Especificaciones OpenSpec | **10/10 válidas** (`openspec validate --all --strict`) |
| Requisitos auditados | **33** en 10 capabilities: 32 `OK` y 1 GAP documentado |
| Changes archivados | **7**, sin changes activos |
| Dependencias nuevas | Ninguna |
| Secretos versionados | Ninguno |

Cobertura por ensamblado: Application 96.6%, Domain 98.1%, Infrastructure 96.4%, Web 88.7%. La cobertura de líneas supera el objetivo del 80% del plan; la de ramas se quedó en 79.5%, ligeramente por debajo de ese objetivo y registrada como tal en el anexo.

### Cumplimiento de entregables

| Entregable | Estado | Ubicación |
|---|---|---|
| Planificación y justificación técnica inicial | ✅ | `PLAN_INGENIERIA.md` (no modificado) |
| Especificaciones, propuestas, tareas y registro de cambios | ✅ | `openspec/specs/` (10), `openspec/changes/archive/` (7 changes) |
| Código fuente funcional | ✅ | `src/ReservaEspacios.{Domain,Application,Infrastructure,Bootstrap,Web}` |
| Pruebas automatizadas | ✅ | `tests/ReservaEspacios.{UnitTests,IntegrationTests,E2ETests}` |
| Instalación, configuración y ejecución | ✅ | `README.md` §§ «Arranque local», «Cuentas de prueba», «Rutas», «Pruebas» |
| Análisis, dificultades, decisiones y conclusiones SDD | ✅ | Este documento |

### Hallazgo de la auditoría final

De los 33 requisitos, 32 tienen correspondencia completa especificación → código → prueba. El único GAP corresponde al escenario «Sin efecto retroactivo» de `RES-005`: no existe una prueba automatizada que inserte una reserva anterior a la regla con duración superior a cuatro horas y verifique que se muestra sin cambios en el historial. El comportamiento está garantizado por diseño — `Reservation` posee un constructor privado que EF Core usa al materializar, y el change no incluyó migración, *backfill* ni cancelación automática—, pero la ausencia de prueba se dejó explícitamente documentada en `docs/VERIFICACION-FINAL.md` §8 en lugar de trasladarla a pruebas de filtros no relacionadas. Se registró también una observación menor: la prueba HTTP de `UI-004` no asevera el cuerpo `{"cerrada":true}`.

## 2. Dificultades encontradas

| Dificultad | Causa | Solución aplicada |
|---|---|---|
| Targetear `net10.0` con un único SDK 11 instalado | No había SDK 10 y no se podía instalar otro | `global.json` con `rollForward: latestMajor` y reference packs 10.0.15 (ADR-003) |
| Imposibilidad de usar Testcontainers.NET | No hay Docker ni privilegios de administrador para instalarlo | PostgreSQL 19 local con un esquema por clase de prueba (ADR-003) |
| `InvalidOperationException` «Headers are read-only» al iniciar sesión | `PasswordSignInAsync` escribía la cookie dentro de un evento `InteractiveServer`, con la respuesta ya iniciada | Formulario HTTP normal en `POST /acceso/autenticar`; endpoint JSON intacto |
| `AmbiguousMatchException` al registrar el login | `POST /acceso/login` colisionaba con la página Razor | Endpoint movido a `/acceso/autenticar`, dejando la página en `/acceso/login` |
| `MSB3026`/`CS2012` al ejecutar suites en paralelo | `VBCSCompiler` bloqueaba DLLs compartidas | Ejecución de la solución en secuencia con `-m:1` |
| Aserciones bUnit frágiles por cultura y zona horaria | Cultura `es` (`1,5`) y hora local (`-5h`) frente a `America/Bogota` | Formateador centralizado `FormatoFechaHora` y comparaciones con la cultura/hora efectivas |
| Motivo de rechazo ilegible en la auditoría | `AuditoriaHandler` serializaba metadata como `\u00f3` | `JavaScriptEncoder.Create(UnicodeRanges.All)` |
| Cobertura inicial incorrecta (82.6%) | ReportGenerator solo procesó un archivo de cobertura | Lista de informes separada por `;`; valor real 95.2% |
| Primer administrador bloqueado por su propio guard | El comando exigía un administrador previo inexistente | Bootstrap vía `IUsuarioIdentityService` para administrador y coordinador |
| `design.md` discrepante con la migración | El diseño afirmaba solo `GRANT INSERT/SELECT`, pero la migración usa `REVOKE ... FROM PUBLIC` y trigger | Alinear el design archivado sin editar el plan |
| Mapeo de `RES-005` apoyado en pruebas de filtros | Auditoría Fase 5: las pruebas citadas no cubrían una reserva previa de más de cuatro horas | Retirar el mapeo y documentar el GAP; no añadir pruebas según el alcance acordado |

## 3. Decisiones modificadas

Las decisiones iniciales están en `PLAN_INGENIERIA.md` §3; las posteriores se registraron en ADR y en los `design.md` de los changes archivados. `ADR-004` las indexa todas sin duplicarlas.

| Decisión original / situación | Decisión final | Fuente |
|---|---|---|
| Precedencia ambigua entre §2.4 y §4.3; matriz de rutas desactualizada | Prevalece §4.3 (usuarios antes que reservas) y la matriz canónica de ADR-001 | `ADR-001` |
| Notificación en toda transición | Solo `APROBADA`, `RECHAZADA` y `CANCELADA`; `PENDIENTE` no envía correo | `ADR-001` |
| MediatR para el despachador de eventos | Diez patrones propios sin MediatR, para no ampliar la stack | `ADR-002` |
| Testcontainers.NET para pruebas de integración | PostgreSQL local con esquemas por clase de prueba | `ADR-003` |
| Contraseñas y cadena en archivos de configuración | Solo variables de entorno / user-secrets | `ADR-003` |
| Login en `InteractiveServer` con `OnValidSubmit` | Formulario HTTP que emite la cookie antes de la respuesta | `design.md` de `fix-login-cookie-interactive-server` |
| Cierre de sesión considerado completo; hora con `ToString()`; sin cancelación desde historial | Redirección a `/`, formato `HH:mm` en `America/Bogota` y cancelación por fila reutilizando el comando existente | `design.md` de `fix-salida-y-historial-reservas` |
| Límite de cuatro horas como validador de la cadena de creación | Invariante único en `Reservation` con `DURACION_MAXIMA_EXCEDIDA` | `design.md` de `add-limite-duracion-reserva` |
| Playwright y GitHub Actions previstos en §5.1 y §5.3 | No implementados en este cierre; registrados como desviación pendiente | `ADR-004` |
| Change OpenSpec para la documentación final | No requerido: no hay cambio de comportamiento observable | `ADR-004` |

`PLAN_INGENIERIA.md` permaneció inmutable en todo el proceso, conforme a su §6.5.

## 4. Conclusiones sobre el uso de SDD

**Beneficios observados**

- Las 10 especificaciones y 33 requisitos permitieron verificar el sistema por comportamiento observable y no solo por cobertura de código: cada ID tiene una ruta identificable spec → código → prueba.
- Los 7 changes archivados conservan propuesta, diseño, tareas y deltas, de modo que el historial explica *por qué* el código es como es; ADR-004 los indexa sin duplicarlos.
- La trazabilidad fue lo que permitió detectar el GAP de `RES-005`: las pruebas existían y pasaban, pero no cubrían el escenario de retroactividad. Sin la matriz, el cierre habría dado por buena una correspondencia aparente.
- Las reglas transversales (roles, estados, intervalos semiabiertos, límites de duración) quedaron especificadas antes de implementarse, evitando interpretaciones divergentes entre UI, API y dominio.

**Limitaciones y costes**

- La disciplina SDD añadió una carga documental notable: cuatro matrices de trazabilidad, cuatro ADR, registro de IA e informe, además de los artefactos de cada change.
- No se implementó CI ni E2E con Playwright, así que la verificación depende de comandos ejecutados manualmente y de PostgreSQL local; la reproducibilidad en otro entorno no está automatizada.
- La cobertura de ramas (79.5%) quedó algo por debajo del objetivo del 80% del plan, aunque la de líneas lo supera ampliamente.
- La aprobación del estudiante en `docs/REGISTRO-USO-IA.md` sigue **PENDIENTE**: el registro documenta el uso de IA, pero aún no ha sido firmado.

**Conclusión**

El enfoque SDD cumplió su objetivo principal: mantener especificación, implementación y prueba alineadas a lo largo de siete cambios, con evidencia verificable y decisiones trazables. El proyecto queda en estado cerrado y reproducible en local; los únicos pendientes —prueba de retroactividad de `RES-005`, Playwright/CI y la firma de aprobación— están documentados en lugar de ocultos, y no impiden la entrega funcional.

## Anexos

- [Verificación final y cierre — Fase 5](docs/VERIFICACION-FINAL.md): evidencia, comandos, cobertura y matriz de los 33 requisitos.
- Trazabilidad: [espacios y disponibilidad](docs/TRAZABILIDAD-ESPACIOS-DISPONIBILIDAD.md), [reservas y aprobaciones](docs/TRAZABILIDAD-RESERVAS-APROBACIONES.md), [interfaz y acceso](docs/TRAZABILIDAD-INTERFAZ-ACCESO.md), [notificaciones, auditoría y reportes](docs/TRAZABILIDAD-NOTIFICACIONES-AUDITORIA-REPORTES.md).
- Decisiones: [ADR-001](docs/adr/ADR-001-trazabilidad.md), [ADR-002](docs/adr/ADR-002-patrones-de-diseno.md), [ADR-003](docs/adr/ADR-003-entorno-de-pruebas.md), [ADR-004](docs/adr/ADR-004-decisiones-de-cierre.md).
- [Registro de uso de IA](docs/REGISTRO-USO-IA.md) y [`README.md`](README.md).
