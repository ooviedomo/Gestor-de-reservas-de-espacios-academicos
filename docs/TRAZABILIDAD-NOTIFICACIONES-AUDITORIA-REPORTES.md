# Trazabilidad: `add-notificaciones-auditoria-y-reportes`

Verificación de las tareas 5.2 y 5.3: cada requisito `NOT-*`, `AUD-*` y `REP-*`
(incluido `REP-003`) del delta tiene al menos un escenario vinculado a una prueba
ejecutable, y cada patrón de `design.md` / `ADR-002` aplicado en este change está
ejercitado. Los nombres son los de los métodos de prueba en `tests/`.

## NOT-001 — Registro de notificación pendiente

| Escenario del spec | Pruebas |
| --- | --- |
| Aprobación genera notificación | `TC_NOT_001_Aprobacion_crea_notificacion_PENDIENTE_para_el_solicitante`, `TC_NOT_001_Aprobacion_inserta_outbox_y_auditoria_en_la_misma_transaccion` |
| Rechazo genera notificación con motivo | `TC_NOT_001_Rechazo_incluye_el_motivo_en_el_contenido`, `TC_NOT_001_Rechazo_y_cancelacion_incluyen_el_motivo` |
| Cancelación genera notificación | `TC_NOT_001_Cancelacion_incluye_el_motivo_en_el_contenido`, `TC_NOT_001_Rechazo_y_cancelacion_incluyen_el_motivo` |
| Creación en `PENDIENTE` no genera notificación | `TC_NOT_001_Creacion_y_completado_no_generan_notificacion`, `TC_NOT_001_Creacion_en_PENDIENTE_no_genera_notificacion` |
| Sin destinatario no rompe la transición | `TC_NOT_001_Sin_destinatario_no_genera_notificacion_ni_falla_la_transicion` |

## NOT-002 — Envío y reintentos

| Escenario del spec | Pruebas |
| --- | --- |
| Envío exitoso → `ENVIADA` con fecha | `TC_NOT_002_Envio_exitoso_marca_la_notificacion_como_ENVIADA` (unitaria e integración) |
| Fallo temporal → `PENDIENTE`, `intentos = 1`, `ultimo_error` | `TC_NOT_002_Fallo_temporal_deja_PENDIENTE_con_un_intento_y_el_error`, `TC_NOT_003_SMTP_caido_deja_la_reserva_APROBADA_y_el_outbox_PENDIENTE` |
| Reintentos agotados → `ERROR` | `TC_NOT_002_Quinto_fallo_pasa_a_ERROR_y_deja_de_reintentarse`, `TC_NOT_002_Strategy_de_reintentos_tiene_limite_de_5_y_backoff_exponencial` |
| Proveedor no configurado, sin red | `TC_NOT_002_Proveedor_Sin_host_devuelve_fallo_controlado_sin_red` |

## NOT-003 — Aislamiento de fallos

| Escenario del spec | Pruebas |
| --- | --- |
| Proveedor caído: reserva `APROBADA`, outbox `PENDIENTE` | `TC_NOT_003_SMTP_caido_deja_la_reserva_APROBADA_y_el_outbox_PENDIENTE` |
| Outbox y cambio de estado se confirman juntos | `TC_NOT_001_Aprobacion_inserta_outbox_y_auditoria_en_la_misma_transaccion`, `TC_NOT_003_Si_falla_el_outbox_la_transicion_de_la_reserva_se_revierte` |

## AUD-001 — Registro inmutable de acciones sensibles

| Escenario del spec | Pruebas |
| --- | --- |
| Aprobación con actor y rol | `TC_AUD_001_Aprobacion_genera_registro_de_Reservation_con_actor_y_rol`, `TC_AUD_001_Aprobacion_y_cambio_de_rol_registran_actor_nombre_y_rol` |
| Cambio de rol con administrador como actor | `TC_AUD_001_Cambio_de_rol_genera_registro_de_User`, `TC_AUD_001_Cambio_de_rol_publica_RolCambiado_con_el_nuevo_rol`, `TC_AUD_001_Aprobacion_y_cambio_de_rol_registran_actor_nombre_y_rol` |
| Alta de usuarios y resto de transiciones de reserva | `TC_AUD_001_Alta_de_solicitante_publica_UsuarioCreado_sin_actor`, `TC_AUD_001_Alta_privilegiada_registra_al_administrador_como_actor`, `TC_AUD_001_Creacion_y_completado_tambien_se_registran`, `TC_AUD_001_Rechazo_y_cancelacion_conservan_el_motivo` |
| Intento de eliminación → `AUDITORIA_INMUTABLE` | `TC_AUD_001_Intento_de_eliminacion_devuelve_AUDITORIA_INMUTABLE`, `TC_AUD_001_Update_y_delete_sobre_audit_logs_son_rechazados` |
| Evento desconocido y actor ausente | `TC_AUD_001_Higher_de_usuario_sin_actor_queda_como_sistema`, `TC_AUD_001_Evento_desconocido_no_escribe_registro`, `TC_AUD_001_Operacion_rechazada_no_publica_evento` |

## AUD-002 — Consulta de auditoría

| Escenario del spec | Pruebas |
| --- | --- |
| Consulta filtrada por entidad | `TC_AUD_002_Consulta_devuelve_registros_filtrados`, `TC_AUD_002_Consulta_filtrada_por_entidad_actor_y_rango_de_fechas` |
| Consulta por rango de fechas | `TC_AUD_002_Consulta_filtrada_por_entidad_actor_y_rango_de_fechas`, `TC_AUD_002_Filtrar_envia_la_entidad_actor_y_el_rango_a_la_facade` |
| Solicitante sin acceso | `TC_AUD_002_Solicitante_no_puede_consultar_la_auditoria`, `TC_AUD_002_Solicitante_ve_NO_AUTORIZADO_y_no_llama_a_la_facade` |
| Coordinador y administrador con acceso | `TC_AUD_002_Coordinador_tambien_puede_consultar`, `TC_AUD_002_La_vista_de_auditoria_muestra_actor_rol_y_detalle`, `TC_UI_005_Rol_privilegiado_ve_enlaces_de_auditoria_y_reporte` |

## REP-001 — Reporte de ocupación por espacio y período

| Escenario del spec | Pruebas |
| --- | --- |
| Reporte por espacio | `TC_REP_001_Reporte_devuelve_solo_el_espacio_solicitado`, `TC_REP_001_Reporte_por_espacio_excluye_canceladas_y_filtra_por_fechas`, `TC_REP_001_La_vista_muestra_las_metricas_de_ocupacion` |
| Reservas canceladas excluidas | `TC_REP_001_Reporte_por_espacio_excluye_canceladas_y_filtra_por_fechas` |
| Rango de fechas | `TC_REP_001_Reporte_por_espacio_excluye_canceladas_y_filtra_por_fechas`, `TC_REP_001_Generar_envia_el_espacio_y_el_rango_a_la_facade`, `TC_REP_001_Rango_invalido_devuelve_RANGO_INVALIDO` |

## REP-002 — Acceso restringido al reporte

| Escenario del spec | Pruebas |
| --- | --- |
| Coordinador consulta el reporte | `TC_REP_002_Rol_con_permiso_consulta_el_reporte`, `TC_UI_005_Rol_privilegiado_ve_enlaces_de_auditoria_y_reporte`, `TC_REP_001_La_vista_muestra_las_metricas_de_ocupacion` |
| Solicitante intenta consultar | `TC_REP_002_Solicitante_no_puede_consultar_el_reporte`, `TC_REP_002_Solicitante_recibe_NO_AUTORIZADO_en_el_endpoint`, `TC_REP_002_Solicitante_ve_NO_AUTORIZADO_y_no_llama_a_la_facade` |

## REP-003 — Cálculo del porcentaje de ocupación

| Escenario del spec | Pruebas |
| --- | --- |
| Denominador por días naturales en `America/Bogota` (20 h) | `TC_REP_003_Denominador_usa_dos_dias_naturales_de_Bogota_y_da_20_horas`, `TC_REP_001_Reporte_por_espacio_excluye_canceladas_y_filtra_por_fechas` |
| Intervalos solapados no se duplican (1,5 h) | `TC_REP_003_Intervalos_solapados_no_se_duplican`, `TC_REP_003_Solapes_entre_COMPLETADA_y_APROBADA_no_se_duplican` |
| Sin reservas, sin división por cero | `TC_REP_003_Sin_reservas_el_porcentaje_es_cero_y_no_divide_por_cero` |

## Patrones aplicados (tarea 5.3)

| Patrón | Implementación | Pruebas |
| --- | --- | --- |
| Command | `EnviarCorreoCommand` / `ICommandHandler<T>` ejecutado por el worker | `TC_NOT_002_Envio_exitoso_marca_la_notificacion_como_ENVIADA`, `TC_NOT_002_Decorator_de_logging_no_altera_el_resultado_del_comando` |
| Strategy | `BackoffExponencial`, `ICalculoOcupacion` | `TC_NOT_002_Strategy_de_reintentos_tiene_limite_de_5_y_backoff_exponencial`, `TC_REP_003_Intervalos_solapados_no_se_duplican` |
| Observer | `AuditoriaHandler`, `NotificacionOutboxHandler`, `RolCambiado` | `TC_AUD_001_Aprobacion_genera_registro_de_Reservation_con_actor_y_rol`, `TC_NOT_001_Aprobacion_crea_notificacion_PENDIENTE_para_el_solicitante`, `TC_AUD_001_Cambio_de_rol_publica_RolCambiado_con_el_nuevo_rol` |
| Adapter | `SmtpEmailSender : IEmailSender` | `TC_NOT_002_Proveedor_Sin_host_devuelve_fallo_controlado_sin_red` |
| Decorator | `LoggingCommandHandler<T>` | `TC_NOT_002_Decorator_de_logging_no_altera_el_resultado_del_comando` |
| Builder | `ReporteBuilder` | `TC_REP_001_Builder_construye_el_filtro_con_valores_explicitos`, `TC_REP_001_Generar_envia_el_espacio_y_el_rango_a_la_facade` |
| Facade | `IReservaEspaciosFacade.ConsultarAuditoria` / `GenerarReporte` | `TC_FACADE_ConsultarAuditoria_delega_en_el_caso_de_uso_protegido`, `TC_FACADE_GenerarReporte_delega_en_el_caso_de_uso_protegido`, `TC_FACADE_El_solicitante_recibe_NO_AUTORIZADO_al_usar_la_facade` |

Los siete patrones exigidos por la tarea 5.3 están cubiertos; los tres restantes de
ADR-002 (State, Chain of Responsibility, Template Method) pertenecen al change anterior
y se documentan en `docs/TRAZABILIDAD-RESERVAS-APROBACIONES.md`.

## Verificación ejecutada

- `dotnet build`: 0 advertencias, 0 errores.
- `dotnet format --verify-no-changes --no-restore`: sin cambios.
- `dotnet test`: **353** pruebas superadas (211 unitarias, 48 bUnit, 94 integración).
- Cobertura de líneas consolidada (coverlet + ReportGenerator): **95.1%** (5127/5390); cobertura de ramas 78.7% (704/894).
- `openspec validate add-notificaciones-auditoria-y-reportes --strict`: válido.
- Migración `20261120162035_NotificacionesAuditoriaYReportes` aplicada en `reservaespacios_test` (fixture) y en `reservaespacios` (desarrollo).
- Sin dependencias NuGet nuevas; credenciales solo en user-secrets/variables de entorno (ADR-003).

### Verificación final Fase 5 (2026-11-20)

Solución completa reejecutada: **374** pruebas (215 unitarias, 100 integración, 59 bUnit),
cobertura **95.2%** de líneas (5210/5467) y **79.5%** de ramas (729/916), build 0/0,
formato sin cambios y `openspec validate --all --strict`: 10/10 specs válidos. Las cifras
anteriores se conservan como registro del change. Detalle en
[`VERIFICACION-FINAL.md`](VERIFICACION-FINAL.md).
