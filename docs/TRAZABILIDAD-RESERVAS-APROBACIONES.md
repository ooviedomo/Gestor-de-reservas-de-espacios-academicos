# Trazabilidad: `add-reservas-y-aprobaciones`

Verificación de las tareas 5.2 y 5.3: cada requisito `USR-*`, `AUTH-*`, `RES-*`, `APR-*`
y `COM-*` del delta tiene al menos un escenario vinculado a una prueba ejecutable, y cada
patrón de `docs/adr/ADR-002-patrones-de-diseno.md` aplicado en este change está ejercitado.
Los nombres son los de los métodos de prueba en `tests/`.

## USR-001 — Registro de Solicitante

| Escenario del spec | Pruebas |
| --- | --- |
| Registro válido | `TC_USR_001_Registro_valido_crea_cuenta_de_solicitante`, `TC_USR_001_Registro_valido_crea_usuario_activo_con_rol_solicitante`, `TC_USR_ROLES_002_El_email_se_configura_como_nombre_de_usuario_unico` |
| Email duplicado | `TC_USR_001_Error_de_identidad_se_propaga_al_cliente("EMAIL_DUPLICADO")`, `TC_USR_001_Email_duplicado_se_rechaza` |
| Contraseña débil | `TC_USR_001_Error_de_identidad_se_propaga_al_cliente("CONTRASENA_DEBIL")`, `TC_USR_001_Contrasena_debil_se_rechaza` |

## USR-002 — Administración de usuarios

| Escenario del spec | Pruebas |
| --- | --- |
| Administrador crea coordinador | `TC_USR_002_Administrador_crea_coordinador_activo` (unitaria e integración) |
| Solicitante intenta crear coordinador / cambiar roles | `TC_USR_002_Solicitante_no_puede_crear_coordinador`, `TC_USR_002_Solicitante_no_puede_cambiar_roles`, `TC_AUTH_003_Solo_administrador_administra_espacios_y_usuarios` |
| Desactivar cuenta | `TC_USR_002_Desactivar_cuenta`, `TC_USR_002_Desactivar_cuenta_impide_iniciar_sesion` |

## USR-003 — Autenticación

| Escenario del spec | Pruebas |
| --- | --- |
| Inicio de sesión válido | `TC_USR_003_Inicio_sesion_valido_devuelve_sesion_con_su_rol` (unitaria e integración) |
| Credenciales inválidas | `TC_USR_003_Credenciales_invalidas_no_revelan_campo_fallido` (2 casos), `TC_USR_003_Autenticacion_rechazada_devuelve_el_codigo_del_dominio("CREDENCIALES_INVALIDAS")` |
| Cuenta desactivada | `TC_USR_003_Autenticacion_rechazada_devuelve_el_codigo_del_dominio("CUENTA_DESACTIVADA")`, `TC_USR_002_Desactivar_cuenta_impide_iniciar_sesion` |

## AUTH-001 — Permisos por rol

| Escenario del spec | Pruebas |
| --- | --- |
| Solicitante crea y cancela sus propias reservas | `TC_AUTH_002_Solicitante_puede_crear_reserva_y_no_consultar_auditoria`, `TC_AUTH_002_Solicitante_cancela_solo_las_propias` |
| Solicitante no aprueba reservas | `TC_AUTH_001_Solicitante_no_aprueba_reservas`, `TC_APR_001_Solicitante_no_aprueba_devuelve_NO_AUTORIZADO` |
| Coordinador aprueba pero no administra usuarios | `TC_AUTH_001_Coordinador_aprueba_y_consulta_auditoria_y_reportes`, `TC_AUTH_003_Coordinador_no_administra_espacios_ni_crea_reservas` |
| Administrador gestiona usuarios, espacios y aprobaciones | `TC_AUTH_003_Administrador_no_crea_reservas_pero_gestiona_espacios_usuarios_y_aprueba` |
| Acceso anónimo denegado | `TC_AUTH_001_Acceso_anónimo_al_historial_devuelve_NO_AUTENTICADO`, `TC_AUTH_001_Guard_deniega_acceso_anonimo_con_NO_AUTENTICADO` |
| Coordinador no administra espacios | `TC_AUTH_003_Coordinador_no_administra_espacios_ni_crea_reservas` |
| Coordinador no crea reservas | `TC_AUTH_001_Guard_de_creacion_de_reserva_deniega_a_gestion`, `TC_AUTH_003_Coordinador_no_administra_espacios_ni_crea_reservas` |
| Administrador no crea reservas | `TC_AUTH_003_Administrador_no_crea_reservas_pero_gestiona_espacios_usuarios_y_aprueba`, `TC_AUTH_001_Guard_de_creacion_de_reserva_deniega_a_gestion` |

## AUTH-002 — Permisos sobre reservas

| Escenario del spec | Pruebas |
| --- | --- |
| Coordinador cancela reserva ajena | `TC_RES_003_y_AUTH_002_Coordinador_cancela_reserva_ajena`, `TC_RES_003_Coordinador_cancela_la_de_otro` |
| Solicitante ve solo su historial | `TC_RES_004_Solicitante_ve_solo_las_suyas_aunque_filtre_otro_usuario`, `TC_RES_004_Coordinador_y_solicitante_reciben_historiales_distintos` |
| Consultar disponibilidad (todos los roles) | `TC_AUTH_002_Todos_pueden_consultar_disponibilidad_y_su_historial` |
| Crear reserva (solo solicitante, propia) | `TC_AUTH_002_Solo_solicitante_crea_reservas`, `TC_AUTH_002_Solicitante_cancela_solo_las_propias` |

## AUTH-003 — Permisos sobre espacios, usuarios, auditoría y reportes

| Escenario del spec | Pruebas |
| --- | --- |
| Coordinador consulta auditoría y reportes | `TC_AUTH_001_Coordinador_aprueba_y_consulta_auditoria_y_reportes`, `TC_AUTH_003_Auditoria_y_reportes_solo_gestion` |
| Solicitante sin acceso a auditoría ni reportes | `TC_AUTH_002_Solicitante_puede_crear_reserva_y_no_consultar_auditoria`, `TC_AUTH_003_Auditoria_y_reportes_solo_gestion` |
| Solo administrador administra espacios y usuarios | `TC_AUTH_003_Solo_administrador_administra_espacios_y_usuarios` |

## RES-001 — Creación de solicitud de reserva

| Escenario del spec | Pruebas |
| --- | --- |
| Solicitud válida (estado `PENDIENTE`) | `TC_RES_001_Solicitud_valida_crea_reserva_pendiente_en_transaccion`, `TC_RES_001_Solicitud_valida_crea_reserva_en_estado_PENDIENTE`, `TC_RES_001_Entidad_se_persiste_con_todos_sus_campos` |
| Rango horario inválido | `TC_RES_001_Rango_invalido_no_abre_transaccion`, `TC_RES_002_Cliente_recibe_400_RANGO_INVALIDO`, `TC_RES_001_Formulario_alta_valida_rango_invalido_localmente` |
| Asistentes exceden capacidad | `TC_RES_001_Capacidad_excedida_no_abre_transaccion`, `TC_RES_001_Asistentes_exceden_capacidad_devuelve_CAPACIDAD_EXCEDIDA`, `TC_RES_001_Formulario_alta_muestra_el_codigo_devuelto_por_el_comando("CAPACIDAD_EXCEDIDA")` |
| Solicitud sin autenticación | `TC_RES_001_Sin_autenticacion_devuelve_NO_AUTENTICADO` |
| Disponibilidad cambiada antes de confirmar | `TC_RES_001_Disponibilidad_cambiada_antes_de_confirmar_se_rechaza`, `TC_RES_001_Disponibilidad_cambiada_antes_de_confirmar_devuelve_SOLAPE_DETECTADO` |
| Validaciones encadenadas (rango, espacio, horario, solape) | `TC_RES_001_Cadena_completa_sin_errores_devuelve_null`, `TC_RES_001_Rango_invalido_detiene_la_cadena_antes_de_consultar_solape`, `TC_RES_001_Espacio_inactivo_devuelve_ESPACIO_INACTIVO`, `TC_RES_001_Fuera_de_horario_devuelve_FUERA_DE_HORARIO`, `TC_RES_001_Solape_devuelve_SOLAPE_DETECTADO` |

## RES-002 — Prevención de reservas simultáneas

| Escenario del spec | Pruebas |
| --- | --- |
| Solicitudes concurrentes | `TC_RES_002_Dos_solicitudes_concurrentes_una_aceptada_y_una_rechazada`, `TC_RES_002_Insercion_solapada_falla_con_exclusion_violation`, `TC_RES_002_Repositorio_decorado_devuelve_SOLAPE_DETECTADO`, `TC_RES_002_Cliente_recibe_409_SOLAPE_DETECTADO_sin_SQL_crudo` |
| Reservas contiguas | `TC_RES_002_Reservas_contiguas_se_aceptan`, `TC_RES_002_Reservas_contiguas_son_aceptadas` |
| Reserva cancelada no bloquea | `TC_RES_002_Reserva_cancelada_no_bloquea_la_nueva`, `TC_RES_002_Estados_no_bloqueantes_permiten_nueva_reserva` (3 casos de datos) |
| Reserva aprobada también bloquea | `TC_RES_002_Aprobada_tambien_bloquea_la_misma_franja` |
| Restricción de base de datos y traducción del error | `TC_RES_002_Migracion_crea_extension_indice_GiST_y_restriccion`, `TC_RES_002_Violacion_de_no_overlap_se_traduce_a_SOLAPE_DETECTADO`, `TC_RES_002_Fallo_de_serializacion_se_traduce_a_SOLAPE_DETECTADO`, `TC_RES_002_Otra_restriccion_no_se_traduce` |

## RES-003 — Cancelación de reservas

| Escenario del spec | Pruebas |
| --- | --- |
| Solicitante cancela la suya | `TC_RES_003_Solicitante_cancela_la_suya_con_motivo`, `TC_RES_003_Solicitante_cancela_la_suya_y_persiste_motivo_y_actor`, `TC_RES_003_Solicitante_cancela_la_suya_y_la_franja_vuelve_a_estar_disponible` |
| Solicitante intenta cancelar la de otro | `TC_RES_003_Solicitante_no_cancela_la_de_otro`, `TC_RES_003_Solicitante_no_cancela_la_de_otro_devuelve_NO_AUTORIZADO` |
| Cancelación sin motivo | `TC_RES_003_Cancelacion_sin_motivo_devuelve_MOTIVO_OBLIGATORIO` (unitaria e integración), `TC_RES_003_Formulario_cancelacion_valida_motivo_localmente` |
| Estado terminal | `TC_RES_003_Cancelar_rechazada_devuelve_TRANSICION_INVALIDA`, `TC_RES_003_Estado_terminal_devuelve_TRANSICION_INVALIDA` |
| Coordinador cancela la de otro | `TC_RES_003_Coordinador_cancela_la_de_otro`, `TC_RES_003_y_AUTH_002_Coordinador_cancela_reserva_ajena` |
| Códigos de error en la UI | `TC_RES_003_Formulario_cancelacion_muestra_el_codigo_devuelto` (3 casos de datos) |

## RES-004 — Historial de reservas

| Escenario del spec | Pruebas |
| --- | --- |
| Solicitante ve solo las suyas | `TC_RES_004_Solicitante_ve_solo_las_suyas_aunque_filtre_otro_usuario`, `TC_RES_004_Solicitante_ve_solo_sus_reservas`, `TC_RES_004_Coordinador_y_solicitante_reciben_historiales_distintos` |
| Coordinador ve todas | `TC_RES_004_Coordinador_ve_todas_las_reservas` (unitaria e integración) |
| Filtro por estado, fecha y espacio | `TC_RES_004_Filtros_de_estado_fecha_y_espacio_se_reenvian`, `TC_RES_004_Filtro_por_estado`, `TC_RES_004_Vista_envia_los_filtros_al_consultar`, `TC_RES_004_Builder_encadena_todos_los_filtros` |
| Decisión registrada (nombre y rol del actor) | `TC_RES_004_Decision_registrada_muestra_nombre_y_rol_del_actor` (unitaria e integración), `TC_RES_004_Vista_muestra_actor_rol_y_motivo_de_rechazo` |
| Motivo de rechazo en el historial | `TC_RES_004_Motivo_de_rechazo_aparece_en_el_historial`, `TC_RES_004_Motivo_de_rechazo_y_rol_del_administrador` |
| Acceso anónimo y páginas Blazor | `TC_RES_004_Sin_autenticacion_devuelve_NO_AUTENTICADO`, `TC_RES_004_Paginas_de_aprobacion_e_historial_responden_200` (2 rutas) |

## APR-001 — Aprobación y rechazo de solicitudes

| Escenario del spec | Pruebas |
| --- | --- |
| Aprobar solicitud pendiente | `TC_APR_001_Aprobar_pendiente_registra_actor_y_fecha`, `TC_APR_001_Coordinador_aprueba_pendiente_y_registra_actor_y_fecha`, `TC_APR_001_Rol_con_permiso_aprueba_y_registra_actor_y_fecha` |
| Rechazar con motivo | `TC_APR_001_Rechazar_con_motivo_registra_motivo_y_actor`, `TC_APR_001_Administrador_rechaza_con_motivo`, `TC_APR_001_Administrador_rechaza_con_motivo_y_lo_persiste` |
| Rechazo sin motivo | `TC_APR_001_Rechazar_sin_motivo_devuelve_MOTIVO_OBLIGATORIO`, `TC_APR_001_Rechazo_sin_motivo_devuelve_MOTIVO_OBLIGATORIO` |
| Transición inválida | `TC_APR_001_Reaprobar_devuelve_TRANSICION_INVALIDA`, `TC_APR_001_Rechazar_reserva_aprobada_devuelve_TRANSICION_INVALIDA` |
| Solicitante intenta aprobar | `TC_APR_001_Solicitante_no_aprueba`, `TC_APR_001_Solicitante_no_aprueba_devuelve_NO_AUTORIZADO` |
| Panel de aprobación (UI) | `TC_APR_001_Panel_muestra_pendientes_y_aprueba`, `TC_APR_001_Panel_rechaza_con_el_motivo_escrito`, `TC_APR_001_Panel_solicitante_ve_NO_AUTORIZADO_y_no_consulta`, `TC_APR_001_Panel_muestra_el_codigo_devuelto_por_el_comando` |

## COM-001 — Completado de reservas

| Escenario del spec | Pruebas |
| --- | --- |
| Completado manual | `TC_COM_001_Completar_manual_registra_fecha`, `TC_COM_001_Rol_con_permiso_completa_y_registra_fecha` (unitaria e integración) |
| Completado automático al vencer | `TC_COM_001_Automatico_registra_fecha_sin_actor_cuando_fin_vencio`, `TC_COM_001_Cierra_todas_las_aprobadas_vencidas_y_devuelve_el_conteo`, `TC_COM_001_Worker_cierra_solo_las_aprobadas_vencidas`, `TC_COM_001_Worker_dispara_el_cierre_automatico_en_cada_tick`, `TC_COM_001_Cierre_automatico_es_idempotente`, `TC_OBS_004_Cierre_automatico_publica_ReservaCompletada_sin_actor` |
| Completado desde estado no aprobado | `TC_COM_001_Completar_pendiente_devuelve_TRANSICION_INVALIDA` (unitaria e integración), `TC_COM_001_Automatico_antes_de_vencer_devuelve_TRANSICION_INVALIDA` |
| Solicitante intenta completar | `TC_COM_001_Solicitante_no_completa`, `TC_COM_001_Solicitante_no_completa_devuelve_NO_AUTORIZADO` |

## Patrones aplicados (ADR-002, tarea 5.3)

| Patrón | Implementación | Pruebas |
| --- | --- | --- |
| State | `Reservation` + cinco estados concretos | `TC_TRANSICION_Matriz_de_estados` (20 combinaciones), `TC_APR_001_Aprobar_pendiente_registra_actor_y_fecha` |
| Template Method | `TransicionStateBase` (`validar actor → verificar transición → aplicar → registrar fecha`) | `TC_APR_001_Aprobar_pendiente_registra_actor_y_fecha`, `TC_COM_001_Completar_manual_registra_fecha` |
| Chain of Responsibility | `ValidacionRango → EspacioActivo → Horario → Capacidad → Solape` | `TC_RES_001_Cadena_completa_sin_errores_devuelve_null`, `TC_RES_001_Rango_invalido_detiene_la_cadena_antes_de_consultar_solape`, `TC_RES_001_Solape_devuelve_SOLAPE_DETECTADO` |
| Command | `ICommand` / `ICommandHandler<T>` en los casos de uso de reservas | `TC_RES_001_Solicitud_valida_crea_reserva_pendiente_en_transaccion`, `TC_APR_001_Coordinador_aprueba_pendiente_y_registra_actor_y_fecha`, `TC_COM_001_Rol_con_permiso_completa_y_registra_fecha` |
| Strategy | `IEvaluadorPermisos`, `IValidacionPorTipoEspacio` | `TC_AUTH_002_Solo_solicitante_crea_reservas`, `TC_AUTH_003_Solo_administrador_administra_espacios_y_usuarios`, `TC_RES_001_Capacidad_excedida_devuelve_CAPACIDAD_EXCEDIDA` |
| Observer | `IDomainEventDispatcher` + eventos `ReservaAprobada/Rechazada/Cancelada/Completada` | `TC_OBS_001_Despachador_entrega_el_evento_a_todos_los_suscriptores`, `TC_OBS_002_Aprobar_publica_ReservaAprobada_con_actor_y_espacio`, `TC_OBS_003_Transicion_invalida_no_publica_evento`, `TC_OBS_004_Cierre_automatico_publica_ReservaCompletada_sin_actor`, `TC_OBS_005_Application_resuelve_el_despachador_sin_suscriptores` |
| Adapter | `IdentityRoleAdapter` (claims de Identity → roles del dominio) | `TC_AUTH_001_Adapter_traduce_claims_de_Identity_a_rol_y_usuario_del_dominio` |
| Builder | `FiltroHistorialBuilder` (filtros opcionales de `RES-004`) | `TC_RES_004_Builder_sin_filtros_devuelve_todo_nulo`, `TC_RES_004_Builder_encadena_todos_los_filtros` |
| Decorator | Repositorio de reservas traductor de errores e invalidador de caché (reutiliza los decorators del change 1) | `TC_RES_002_Repositorio_decorado_devuelve_SOLAPE_DETECTADO`, `TC_DISP_INV_001_Escritura_exitosa_invalida_la_cache_del_espacio` |
| Facade | `IReservaEspaciosFacade` para Razor y Minimal APIs | `TC_FACADE_001_ConsultarCatalogo_delega_en_el_caso_de_uso`, `TC_FACADE_002_ConsultarDisponibilidad_delega_en_el_caso_de_uso` |

Los nueve patrones exigidos por la tarea 5.3 están cubiertos; se añade **Decorator** como
décimo patrón de ADR-002 porque el repositorio de reservas lo reutiliza en este change.

## Verificación ejecutada

- `dotnet build ReservaEspacios.sln`: 0 advertencias, 0 errores.
- `dotnet format ReservaEspacios.sln --verify-no-changes`: sin cambios.
- `dotnet test ReservaEspacios.sln`: **271** pruebas superadas (169 unitarias, 21 bUnit, 81 integración).
- Cobertura de líneas consolidada (coverlet): **93.9%** (3461/3686). Por ensamblado: Application 94.7% (715/755), Domain 92.7% (316/341), Infrastructure 96.0% (1901/1980), Web 86.7% (529/610).
- `openspec validate add-reservas-y-aprobaciones --strict`: válido.
