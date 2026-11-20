# Registro de uso de IA

- **Proyecto:** `ReservaEspacios`
- **Alcance:** fases 2 y 3 (revisión de especificaciones, implementación asistida y cierre del change `add-reservas-y-aprobaciones`), changes `add-interfaz-de-acceso-y-navegacion`, `add-notificaciones-auditoria-y-reportes`, `fix-login-cookie-interactive-server` y `fix-salida-y-historial-reservas`
- **Base:** `PLAN_INGENIERIA.md` §6.3–6.5. Este documento **no modifica** el plan; las decisiones nuevas están en `docs/adr/` y en el `design.md` del change.
- **Estado:** aprobación del estudiante **PENDIENTE** (ver §4).

Los prompts siguientes son **representativos**: parafrasean las instrucciones reales dadas al asistente durante las sesiones de trabajo, agrupando las que produjeron el mismo artefacto. No son transcripciones literales de cada mensaje.

## 1. Prompts representativos y salida obtenida

| # | Etapa | Prompt representativo | Salida de IA (artefactos) | Verificación aplicada |
| --- | --- | --- | --- | --- |
| P-01 | Fase 2 | «Revisa los tres changes propuestos contra el plan §4.3 y detecta inconsistencias de orden y de notificaciones antes de implementar» | `docs/adr/ADR-001-trazabilidad.md`: precedencia usuarios → reservas → aprobaciones → notificaciones; `PENDIENTE` no notifica | Lectura del plan §2.4 vs §4.3; specs ajustadas y validadas con `openspec validate <change> --strict` |
| P-02 | Fase 2 | «Propón una selección fundamentada de patrones de diseño entre 22 candidatos y justifica los descartes» | `docs/adr/ADR-002-patrones-de-diseno.md`: 10 patrones adoptados, 12 descartados, regla de no duplicación | Revisión humana; reflejo en los tres `design.md` y verificación por pruebas (§3 de la matriz de trazabilidad de cada change) |
| P-03 | Fase 2 | «Define el entorno de pruebas sin Docker usando PostgreSQL local y un SDK que compile net10.0» | `docs/adr/ADR-003-entorno-de-pruebas.md`; `global.json` con `rollForward: latestMajor` | `dotnet build` en net10.0; migraciones aplicadas en `127.0.0.1:5432` |
| P-04 | Fase 2 | «Genera los deltas de spec de los tres changes con escenarios GIVEN/WHEN/THEN y sin salirse del alcance de cada uno» | `openspec/changes/*/specs/**/spec.md` (usuarios, autorizacion, reservas, aprobaciones, además de los changes 1 y 3) | `openspec validate <change> --strict` para los tres changes; correcciones C-1…C-6 y A-1…A-7 |
| P-05 | Fase 3 / 2.1–2.3 | «Crea la entidad Reservation, la migración con tstzrange, el índice GiST y la restricción no_overlap; traduce las violaciones a SOLAPE_DETECTADO sin exponer SQL» | `Reservation`, migración `20261120020004_Reservations.cs`, `TraductorErroresReserva`, `TraductorPostgresReserva`, decorators de repositorio | `TC_RES_002_Insercion_solapada_falla_con_exclusion_violation`, `TC_RES_002_Migracion_crea_extension_indice_GiST_y_restriccion`, `TC_RES_002_Cliente_recibe_409_SOLAPE_DETECTADO_sin_SQL_crudo` |
| P-06 | Fase 3 / 1.x | «Configura Identity con email como UserName, semilla de los tres roles en español y guards que nieguen por defecto» | `20261120020003_IdentidadYRoles.cs`, `Rol.cs` (matriz canónica), `EvaluadorPermisos`, `GuardiaOperacionesProtegidas` | `TC_USR_ROLES_001/002`, `TC_AUTH_001_*`, `TC_AUTH_003_*` con `WebApplicationFactory` |
| P-07 | Fase 3 / 3.2–3.3 | «Revalida disponibilidad dentro de una transacción SERIALIZABLE y añade una prueba con dos peticiones concurrentes sobre la misma franja» | `ITransaccionReserva` / `TransaccionReserva`, cadena `Rango → EspacioActivo → Horario → Capacidad → Solape` | `TC_RES_001_Disponibilidad_cambiada_antes_de_confirmar_*`, `TC_RES_002_Dos_solicitudes_concurrentes_una_aceptada_y_una_rechazada` |
| P-08 | Fase 3 / 3.4–4.2 | «Implementa cancelación, aprobación, rechazo y completado como comandos que deleguen las transiciones en la máquina de estados» | `CancelarReserva`, `AprobarReserva`, `RechazarReserva`, `CompletarReserva`, `CerrarReservasVencidas` + worker | `TC_TRANSICION_Matriz_de_estados` (20 combinaciones), `TC_APR_001_*`, `TC_COM_001_*` unitarias e integración |
| P-09 | Fase 3 / 4.3–4.4 | «Construye el historial con filtros por estado/fecha/espacio y visibilidad por rol; expón las columnas de actor, rol y motivos en Blazor» | `ConsultarHistorial`, `FiltroHistorialBuilder`, `HistorialReservas.razor`, `AprobacionesReserva.razor`, endpoint `/api/reservas/historial` | `TC_RES_004_*`, 6 pruebas bUnit y `TC_RES_004_Paginas_de_aprobacion_e_historial_responden_200` |
| P-10 | Fase 3 / 5.3 | «Implementa el Observer de ADR-002 con un despachador propio, sin añadir MediatR a la stack» | `IDomainEvent`, `IDomainEventSubscriber`, `IDomainEventDispatcher`, `NullDomainEventDispatcher`, `DomainEventDispatcher`, eventos `ReservaAprobada/Rechazada/Cancelada/Completada` | `TC_OBS_001`…`TC_OBS_005`; `dotnet list package` sin dependencias nuevas |
| P-11 | Fase 3 / 5.2 | «Genera la matriz que vincule cada escenario de los specs y cada patrón de ADR-002 con una prueba ejecutable» | `docs/TRAZABILIDAD-RESERVAS-APROBACIONES.md` | Revisión de los 5 specs y de los 210 nombres de prueba; `openspec validate add-reservas-y-aprobaciones --strict` |
| P-12 | Fase 3 / cierre | «Documenta los prompts, la salida y la verificación antes de archivar el change» | Este documento (sección original) | Revisión del estudiante (pendiente, §4) |
| P-13 | Interfaz / spec | «Plantea un change separado para sustituir la portada, añadir menú y permitir login/registro desde Razor» | `openspec/changes/add-interfaz-de-acceso-y-navegacion/` con `UI-001`…`UI-004` y `design.md` | `openspec validate add-interfaz-de-acceso-y-navegacion --strict`: válido |
| P-14 | Interfaz / Web | «Reutiliza AutenticarUsuarioCommand y RegistrarSolicitanteCommand; añade solo el cierre de sesión y el menú por IEvaluadorPermisos» | `ISesionWeb`/`SesionWeb`, `POST /api/autenticacion/logout`, `NavMenu.razor`, `Home.razor`, `Login.razor`, `Registro.razor`, `README.md` | 14 pruebas bUnit y `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` con `WebApplicationFactory` |
| P-15 | Interfaz / arranque | «Documenta el arranque local y crea los tres roles sin versionar credenciales» | `tools/ReservaEspacios.Bootstrap`, `README.md` | Variables `BOOTSTRAP_*_EMAIL/PASSWORD`; compilación con 0 errores |
| P-16 | Interfaz / cierre | «Genera la matriz de trazabilidad del change de interfaz y actualiza el registro de uso de IA» | `docs/TRAZABILIDAD-INTERFAZ-ACCESO.md`, este documento | 286 pruebas; cobertura 94.3%; `openspec validate --all --strict` |
| P-17 | Interfaz / robustez | «Evita que un fallo al cargar el catálogo derribe el circuito InteractiveServer y muestra un mensaje sin SQL» | `try/catch` en `ConsultarDisponibilidadPage.OnInitializedAsync` y `TC_DISP_001_Componente_muestra_mensaje_si_el_catalogo_falla` | 287 pruebas; cobertura 94.2% |
| P-18 | Notificaciones / spec | «Ajusta el plan de notificaciones para que solo `APROBADA`, `RECHAZADA` y `CANCELADA` generen correo, con outbox y reintentos, sin añadir dependencias» | `openspec/changes/add-notificaciones-auditoria-y-reportes/` (`NOT-001/002/003`, `AUD-001/002`, `REP-001/002/003`), `design.md` | `openspec validate ... --strict`; ADR-001/002/003 como fuente de decisiones |
| P-19 | Notificaciones / persistencia | «Crea `notifications` con `ultimo_error` y `audit_logs` append-only, con índices y trigger en PostgreSQL local» | Migración `20261120162035_NotificacionesAuditoriaYReportes.cs`, `Notificacion`, `AuditLog`, `TransaccionEventos` | `TC_NOT_001_*`, `TC_AUD_001_Update_y_delete_*`; migración aplicada en `reservaespacios_test` y `reservaespacios` |
| P-20 | Notificaciones / worker | «Separa el envío SMTP de la transacción: outbox en el caso de uso y `IHostedService` con reintentos y proveedor falso en pruebas» | `NotificacionOutboxHandler`, `ProcesadorNotificaciones`, `NotificacionesWorker`, `BackoffExponencial`, `SmtpEmailSender` | `TC_NOT_002_*` y `TC_NOT_003_*` unitarias e integración; reserva aprobada intacta con SMTP caído |
| P-21 | Auditoría y reportes | «Registra quién, cuándo y sobre qué entidad; configura la consulta filtrable y calcula ocupación por espacio con días naturales y unión de intervalos» | `AuditoriaHandler`, `AuditoriaRepository`, `CalculoOcupacion`, `RepositorioOcupacion`, endpoints `/api/auditoria` y `/api/reportes/ocupacion` | `TC_AUD_001_*`, `TC_AUD_002_*`, `TC_REP_001_*`, `TC_REP_003_*` |
| P-22 | UI / cierre | «Añade las vistas de auditoría y reporte, verifica el menú por roles y genera la matriz de trazabilidad del change» | `Auditoria.razor`, `ReporteOcupacion.razor`, `AuditoriaYReportesUITests.cs`, `docs/TRAZABILIDAD-NOTIFICACIONES-AUDITORIA-REPORTES.md` | 48 pruebas bUnit; 54 nombres de prueba verificados; cobertura 95.1% |
| P-23 | Login / diagnóstico | «Investiga el InvalidOperationException "Headers are read-only" al acceder desde la UI y propón un change sin reabrir los archivados» | `openspec/changes/fix-login-cookie-interactive-server/` con `UI-002` modificada y `design.md` | `openspec validate fix-login-cookie-interactive-server --strict`: válido; causa localizada en el evento `InteractiveServer` |
| P-24 | Login / implementación | «Sustituye el EditForm por un formulario HTTP normal, reutiliza USR-003 y añade regresión con Set-Cookie y errores por query» | `Login.razor`, `POST /acceso/autenticar` en `ApiEndpoints.cs`, pruebas bUnit y HTTP en `FlujoAccesoWebTests.cs` | 15 pruebas bUnit y 4 pruebas HTTP del flujo de acceso superadas |
| P-25 | Salida y formato / spec | «Haz que Salir vuelva al inicio, muestra la hora en HH:mm también en los formularios y pon un botón para que el solicitante cancele desde cada fila del historial» | `openspec/changes/fix-salida-y-historial-reservas/` con `UI-004` modificada y `UI-005`/`UI-006` añadidas, `design.md` y `tasks.md` | `openspec validate fix-salida-y-historial-reservas --strict`: válido |
| P-26 | Salida y formato / Web | «Añade un endpoint de formulario para cerrar sesión y redirigir, y un formateador único en America/Bogota para tablas y formularios» | `POST /acceso/cerrar-sesion`, `NavMenu.razor`, `FormatoFechaHora`, columnas `Fecha`, campos `type="time"` con ayuda `HH:mm` | 9 pruebas bUnit/unitarias de `UI-005` y `UI-004`; 5 pruebas HTTP del flujo de acceso, incluida la redirección `302` a `/` |
| P-27 | Historial / cancelación | «Reutiliza CancelarReservaCommand para cancelar desde el historial, mostrando el botón solo en reservas propias cancelables» | `SolicitanteId` en `HistorialReservaDto`, columna de acción con motivo por fila en `HistorialReservas.razor` | 3 pruebas bUnit de visibilidad, motivo obligatorio, envío y recarga; 58 pruebas bUnit del proyecto en verde |

## 2. Salidas rechazadas o corregidas durante la revisión

| Salida de IA | Problema detectado | Corrección aplicada |
| --- | --- | --- |
| Orden de implementación según plan §2.4 (reservas antes que usuarios) | Incompatible con la dependencia real de autenticación y con §4.3 | ADR-001; se sigue usuarios → reservas → aprobaciones |
| Notificación automática al crear reserva `PENDIENTE` | Cambiaba el alcance del plan | ADR-001: `PENDIENTE` no notifica; notificaciones quedan para el tercer change |
| MediatR como bus de eventos | Añade una dependencia fuera de la stack del plan §3.2 | ADR-002: `IDomainEventDispatcher` propio |
| Roles administrativos sembrados por defecto con el mismo rol que el solicitante | Rompe la matriz de permisos de los tres roles | Semilla exacta de `SOLICITANTE`, `COORDINADOR`, `ADMINISTRADOR` en la migración de Identity |
| `FiltroHistorialBuilder` nombrado en `design.md` pero no implementado | Decisión de diseño incumplida | Implementado y ejercitado por `TC_RES_004_Builder_*` |
| Expresiones Moq con operador `is not null` en árboles de expresión | Error de compilación `CS8122` en las pruebas bUnit | Sustituidas por `!= null` |
| Captura del evento de dominio configurada después de invocar al handler | La prueba no capturaba nada | Callback registrado en el constructor de la prueba |
| Suposición de que `/api/autenticacion/login` no emitía cookie | `PasswordSignInAsync` ya emite la cookie en una petición HTTP; el fallo real era invocarlo desde un evento `InteractiveServer` | Corregido en el `design.md` del change de login: la UI envía un formulario normal y reutiliza el comando |
| Iniciar sesión desde `OnValidSubmit` de `EditForm` | La respuesta ya estaba iniciada y Kestrel rechazaba `Set-Cookie` (`Headers are read-only`) | Formulario estático que hace POST a `/acceso/autenticar`; la cookie se emite antes de la redirección |
| Registrar el POST en `/acceso/login`, la misma ruta del `@page` Razor | `AmbiguousMatchException`: la página Razor acepta todos los verbos | Endpoint movido a `/acceso/autenticar`; la página sigue sirviéndose en `/acceso/login` |
| `CrearUsuarioPrivilegiadoCommand` para el primer administrador del bootstrap | El guard exige un administrador previo, que aún no existe | El primer administrador y el coordinador se crean vía `IUsuarioIdentityService`; el solicitante usa el comando |
| `record UsuarioDePrueba(Rol? Rol)` en bUnit | El parámetro primary-constructor chocaba con la propiedad `Rol` | Renombrado a `RolSesion` |
| Fusión de cobertura con archivos separados por espacio | ReportGenerator solo procesó un archivo y dio 82.6% | Lista separada por `;`; cobertura real 94.3% |
| `AuditoriaHandler` serializaba metadata con `\u00f3` | El motivo «mantención» aparecía ilegible en la vista | `JavaScriptEncoder.Create(UnicodeRanges.All)`; Razor codifica el HTML al mostrarlo |
| Prueba de cambio de rol construida con `new CambiarRolUsuario(identity, usuario)` | El parámetro opcional `eventos` quedó nulo y no se publicó `RolCambiado` | Resolver el caso de uso por DI con sesión de administrador |
| `TC_OBS_005` sin `IReservaRepository` en el contenedor de pruebas | Fallo de resolución del `NotificacionOutboxHandler` | Registrar `Mock<IReservaRepository>` en el contenedor |
| Expectativa de auditoría vacía en `AutorizacionTests` | Otras pruebas de la misma clase siembran registros en el esquema | Comprobar que la consulta devuelve historial no vacío |
| Aserciones bUnit con decimales y fechas literales | Cultura `es` (`1,5`) y zona local (`-5h`) rompían las pruebas | Usar `ToString()` de la cultura actual y comparar la fecha local |
| `design.md` afirmaba que solo se concedían `GRANT INSERT/SELECT` | La migración usa `REVOKE ... FROM PUBLIC` y trigger porque el usuario es propietario | Alinear el `design.md` con la migración sin editar `PLAN_INGENIERIA.md` |

## 3. Verificación global aplicada a todo el código generado

- **Revisión humana del diff** antes de cada commit (`5455661`, `2cf104b`, `35346fb`, `725c2ce`, `9be23f4`, `0a6f902`, `a45b2b3`, los del change de interfaz y los del change actual).
- `dotnet build`: 0 advertencias, 0 errores.
- `dotnet format --verify-no-changes --no-restore`: sin cambios.
- `dotnet test`: **367** pruebas superadas (211 unitarias, 58 bUnit, 98 integración).
- Cobertura de líneas consolidada (coverlet): **95.1%** (5196/5462); ramas 79.1% (723/914), por encima del umbral del 80% de la tarea 5.1.
- `openspec validate add-reservas-y-aprobaciones --strict`, `openspec validate add-interfaz-de-acceso-y-navegacion --strict`, `openspec validate add-notificaciones-auditoria-y-reportes --strict`, `openspec validate fix-login-cookie-interactive-server --strict` y `openspec validate fix-salida-y-historial-reservas --strict`: válidos.
- Migraciones de EF Core revisadas: `btree_gist`, `EXCLUDE ... WHERE estado IN ('PENDIENTE','APROBADA')`, columnas `asistentes`, `motivo_rechazo`, `completado_en`, `ultimo_error`, índices de auditoría y trigger `audit_logs_inmutable`.
- Dependencias: sin paquetes añadidos a la stack del plan §3.2.
- Trazabilidad requisito → prueba: `docs/TRAZABILIDAD-ESPACIOS-DISPONIBILIDAD.md`, `docs/TRAZABILIDAD-RESERVAS-APROBACIONES.md`, `docs/TRAZABILIDAD-INTERFAZ-ACCESO.md` y `docs/TRAZABILIDAD-NOTIFICACIONES-AUDITORIA-REPORTES.md`.
- Flujo local verificado con PostgreSQL 19 en `127.0.0.1:5432` (ADR-003), sin Docker.

## 4. Aprobación del estudiante — PENDIENTE

De acuerdo con `PLAN_INGENIERIA.md` §6.5, la aprobación de especificaciones, reglas de negocio, arquitectura y del momento de archivar cada *change* es responsabilidad del estudiante.

- [ ] He revisado los prompts y las salidas documentadas.
- [ ] He revisado el `diff` de los commits listados en §3.
- [ ] Acepto la matriz de estados, los tres roles en español y la regla de `PENDIENTE` que bloquea.
- [ ] Acepto que no se añadieron dependencias nuevas y que la cobertura supera el 80%.
- [ ] Autorizo archivar `add-reservas-y-aprobaciones`.
- [ ] He revisado el diff del change `add-interfaz-de-acceso-y-navegacion`.
- [ ] Acepto que la portada, el menú y el bootstrap se implementaron sin nuevas dependencias y con cobertura superior al 80%.
- [ ] Autorizo archivar `add-interfaz-de-acceso-y-navegacion`.
- [ ] He revisado el diff del change `add-notificaciones-auditoria-y-reportes`.
- [ ] Acepto el outbox, el trigger de auditoría, el reporte de ocupación y las nuevas vistas.
- [ ] Autorizo archivar `add-notificaciones-auditoria-y-reportes`.
- [ ] He revisado el diff del change `fix-login-cookie-interactive-server`.
- [ ] Acepto que la cookie se emita en una petición HTTP de formulario y que los errores viajen por query.
- [ ] Autorizo archivar `fix-login-cookie-interactive-server`.
- [ ] He revisado el diff del change `fix-salida-y-historial-reservas`.
- [ ] Acepto que «Cerrar sesión» redirija a la portada, que la hora se muestre en `HH:mm` y que la cancelación reutilice el comando existente.
- [ ] Autorizo archivar `fix-salida-y-historial-reservas`.

**Nombre del estudiante:** ______________________________________

**Fecha:** ______________________________________

**Firma o confirmación:** ______________________________________

> Nota: mientras esta sección esté sin marcar, el registro queda como evidencia preparada para revisión; no debe interpretarse como aprobación concedida.
