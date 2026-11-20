# Verificación final y cierre — Fase 5

- **Fecha:** 2026-11-20
- **Estado:** verificación ejecutada sobre el árbol limpio tras la Fase 4
- **Alcance:** solución `ReservaEspacios` completa; 10 specs OpenSpec, 33 requisitos y 7 *changes* archivados

## 1. Resumen ejecutivo

La verificación final confirmó que el repositorio está en verde y que las especificaciones, el código y las pruebas corresponden entre sí. No se detectaron cambios de comportamiento en esta fase: el trabajo fue de ejecución, auditoría y documentación.

| Indicador | Resultado |
|---|---|
| Pruebas ejecutadas | **374** (215 unitarias, 59 bUnit, 100 integración) |
| Cobertura de líneas | **95.2%** (5210/5467) |
| Cobertura de ramas | **79.5%** (729/916) |
| Compilación | 0 advertencias, 0 errores |
| Formato | sin cambios |
| Specs OpenSpec | **10/10 válidos** (`openspec validate --all --strict`) |
| Requisitos auditados | **33** en 10 capabilities: 32 `OK` y 1 GAP documentado (`RES-005`, sin prueba automatizada del escenario «sin efecto retroactivo») |
| Changes archivados | **7** |
| Dependencias nuevas | Ninguna |
| Secretos versionados | Ninguno |

## 2. Entorno verificado

- Sistema: Windows; SDK .NET 11.0.100 con `rollForward: latestMajor` y *target* `net10.0` (`global.json`), según ADR-003.
- Stack: ASP.NET Core 10, Blazor Web App, EF Core 10 + Npgsql.
- Base de datos: PostgreSQL 19 en `127.0.0.1:5432`, sin Docker. Desarrollo: `reservaespacios`; pruebas: `reservaespacios_test` con esquema por clase de prueba.
- Credenciales: solo variables de entorno / user-secrets. La cadena de pruebas se definió únicamente en el shell que ejecutó `dotnet test`; no aparece en el repositorio ni en este documento.
- Herramientas de verificación: `coverlet.collector` 6.0.4, `dotnet-reportgenerator-globaltool` 5.4.13 y CLI de OpenSpec.

## 3. Comandos ejecutados y resultados

| Comando | Resultado |
|---|---|
| `dotnet format ReservaEspacios.sln --verify-no-changes --no-restore` | Sin cambios. |
| `dotnet build ReservaEspacios.sln --nologo -v minimal --no-restore` | Compilación correcta: 0 advertencias, 0 errores. |
| `dotnet test ReservaEspacios.sln --nologo -m:1 --collect:"XPlat Code Coverage"` | 215 unitarias + 100 integración + 59 bUnit = **374 superadas**, 0 fallidas, 0 omitidas. |
| `dotnet tool run reportgenerator ... -reporttypes:TextSummary;JsonSummary` | Líneas 95.2% (5210/5467); ramas 79.5% (729/916); métodos 96.9% (678/699). |
| `& "$env:APPDATA\npm\openspec.cmd" validate --all --strict` | 10 specs válidos, 0 fallidos. |

Cobertura por ensamblado: Application 96.6%, Domain 98.1%, Infrastructure 96.4%, Web 88.7%.

## 4. Matriz consolidada requisito → spec → código → prueba

Las rutas de código son relativas a `src/`; los nombres de prueba son métodos en `tests/`.

| ID | Capability | Comportamiento observable | Código (archivo/clave) | Pruebas | Estado |
|---|---|---|---|---|---|
| ESP-001 | espacios | Registra espacios activos con nombre único, capacidad positiva y apertura anterior al cierre. | `Domain/Espacios/Space.cs` (ctor), `Application/Espacios/RegistrarSpace.cs` | `TC_ESP_001_Registro_valido_queda_activo`, `TC_ESP_001_Nombre_duplicado_devuelve_error`, `TC_ESP_001_Horario_invalido_devuelve_error` | OK |
| ESP-002 | espacios | Edita, desactiva y reactiva espacios conservando reservas previas, con aviso al desactivar con reservas futuras. | `Application/Espacios/DesactivarSpace.cs`, `Web/Components/Pages/AdministracionEspacios.razor` | `TC_ESP_002_Edicion_de_capacidad_no_afecta_reservas_previas`, `TC_ESP_002_Desactivar_con_reservas_futuras_muestra_aviso_y_confirma` | OK |
| ESP-003 | espacios | Devuelve por defecto solo activos, filtrables por tipo/capacidad/estado; el filtro de inactivos exige rol de gestión. | `Application/Espacios/FiltroCatalogoBuilder.cs`, `Infrastructure/Espacios/SpaceRepository.cs` | `TC_ESP_003_Filtro_por_tipo_devuelve_solo_salas_de_computo_activas`, `TC_ESP_003_Filtro_de_estado_denegado_a_solicitante` | OK |
| DISP-001 | disponibilidad | Consulta una franja `[inicio, fin)` y devuelve `DISPONIBLE`/`NO_DISPONIBLE` con motivo. | `Application/Disponibilidad/ConsultarDisponibilidad.cs`, `Infrastructure/Disponibilidad/DisponibilidadRepository.cs` | `TC_DISP_001_Espacio_libre`, `TC_DISP_001_Espacio_inactivo`, `TC_DISP_001_Endpoint_devuelve_FUERA_DE_HORARIO` | OK |
| DISP-002 | disponibilidad | Solo bloquean `PENDIENTE`/`APROBADA`; franjas contiguas no solapan. | `Application/Disponibilidad/ReservaBloqueante.cs`, `Infrastructure/Disponibilidad/DisponibilidadRepository.cs` | `TC_DISP_002_Reserva_bloqueante_solapa`, `TC_DISP_002_Reservas_contiguas_no_se_solapan` | OK |
| DISP-003 | disponibilidad | Interpreta el horario en `America/Bogota`, convierte a UTC y acepta terminar al cierre, no empezar al cierre. | `Application/Disponibilidad/IHorarioEspacio.cs`, `Domain/Espacios/HorarioAtencion.cs` | `TC_DISP_003_Franja_que_termina_en_la_hora_de_cierre_esta_dentro`, `TC_DISP_003_La_comparacion_se_hace_en_UTC` | OK |
| USR-001 | usuarios | Autorregistro de `Solicitante` con email único y contraseña segura, con códigos estables. | `Application/Usuarios/RegistrarSolicitante.cs`, `Infrastructure/Identity/IdentityUsuarioService.cs` | `TC_USR_001_Registro_valido_crea_usuario_activo_con_rol_solicitante`, `TC_USR_001_Email_duplicado_se_rechaza`, `TC_USR_001_Contrasena_debil_se_rechaza` | OK |
| USR-002 | usuarios | Solo el administrador crea privilegiados, cambia roles y activa/desactiva cuentas. | `Application/Usuarios/CrearUsuarioPrivilegiado.cs`, `CambiarRolUsuario.cs`, `ActivarDesactivarUsuario.cs` | `TC_USR_002_Administrador_crea_coordinador_activo`, `TC_USR_002_Solicitante_no_puede_crear_coordinador` | OK |
| USR-003 | usuarios | Autentica email/contraseña sin revelar el campo fallido y rechaza cuentas desactivadas. | `Application/Usuarios/AutenticarUsuario.cs`, `Web/ApiEndpoints.cs` | `TC_USR_003_Inicio_sesion_valido_devuelve_sesion_con_rol`, `TC_USR_003_Credenciales_invalidas_no_revelan_campo_fallido` | OK |
| AUTH-001 | autorizacion | Aplica la matriz a operaciones protegidas y deniega por defecto, incluido el acceso anónimo. | `Application/Autorizacion/EvaluadorPermisos.cs`, `GuardiaOperacionesProtegidas.cs` | `TC_AUTH_001_Guard_de_aprobacion_devuelve_NO_AUTORIZADO`, `TC_AUTH_001_Guard_deniega_acceso_anonimo_con_NO_AUTENTICADO` | OK |
| AUTH-002 | autorizacion | Todos consultan; solo el solicitante crea; gestión cancela/consulta todas y aprueba/completa. | `Application/Autorizacion/EvaluadorPermisos.cs`, `Application/Reservas/ConsultarHistorial.cs` | `TC_AUTH_002_Solo_solicitante_crea_reservas`, `TC_AUTH_002_Solicitante_cancela_solo_las_propias` | OK |
| AUTH-003 | autorizacion | Administra espacios/usuarios solo el administrador; auditoría y reportes, administrador y coordinador. | `Application/Autorizacion/GuardiasAdministracionEspacios.cs`, `EvaluadorPermisos.cs` | `TC_AUTH_003_Auditoria_y_reportes_solo_gestion`, `TC_AUTH_003_Solo_administrador_administra_espacios_y_usuarios` | OK |
| RES-001 | reservas | Crea `PENDIENTE` revalidando rango, espacio, horario, capacidad y solape en transacción. | `Application/Reservas/CrearReserva.cs`, `Validacion/CadenaValidacionCrearReserva.cs` | `TC_RES_001_Solicitud_valida_crea_reserva_pendiente_en_transaccion`, `TC_RES_001_Disponibilidad_cambiada_antes_de_confirmar_devuelve_SOLAPE_DETECTADO` | OK |
| RES-002 | reservas | Rechaza el segundo solape concurrente con `SOLAPE_DETECTADO`; prevalece la restricción `EXCLUDE`. | `Infrastructure/Reservas/ReservaRepository.cs`, `Migrations/20261120024632_ReservasYAntiSolape.cs` | `TC_RES_002_Dos_solicitudes_concurrentes_una_aceptada_y_una_rechazada`, `TC_RES_002_Migracion_crea_extension_indice_GiST_y_restriccion` | OK |
| RES-003 | reservas | Cancela solo `PENDIENTE`/`APROBADA` con motivo, actor y propiedad o rol de gestión. | `Application/Reservas/CancelarReserva.cs`, `Domain/Reservas/CanceladaState.cs` | `TC_RES_003_Solicitante_cancela_la_suya_con_motivo`, `TC_RES_003_Cancelacion_sin_motivo_devuelve_MOTIVO_OBLIGATORIO` | OK |
| RES-004 | reservas | Historial con estado, espacio, fechas, solicitante, actor (nombre/rol), motivos y marcas; alcance por rol. | `Application/Reservas/ConsultarHistorial.cs`, `Web/Components/Pages/HistorialReservas.razor` | `TC_RES_004_Solicitante_ve_solo_las_suyas_aunque_filtre_otro_usuario`, `TC_RES_004_Decision_registrada_muestra_nombre_y_rol_del_actor` | OK |
| RES-005 | reservas | Rechaza >4 h con `DURACION_MAXIMA_EXCEDIDA`, acepta 4 h exactas, aplica a todo espacio/usuario y no altera reservas previas. | `Domain/Reservas/Reservation.cs` (`fin - inicio > 4 h`), `Domain/Common/DomainErrors.cs` | `TC_RES_005_Cuatro_horas_exactas_crea_la_reserva`, `TC_RES_005_Mas_de_cuatro_horas_devuelve_DURACION_MAXIMA_EXCEDIDA`, `TC_RES_005_Formulario_alta_muestra_DURACION_MAXIMA_EXCEDIDA` | **GAP**: «sin efecto retroactivo» sin prueba automatizada (ver §8); «todos los espacios» cubierto por el invariante, independiente de `EspacioTipo` |
| APR-001 | aprobaciones | Coordinador/admin aprueban o rechazan solo `PENDIENTE`, con actor/fecha y motivo en el rechazo. | `Application/Reservas/AprobarReserva.cs`, `RechazarReserva.cs`, `Domain/Reservas/AprobadaState.cs` | `TC_APR_001_Aprobar_pendiente_registra_actor_y_fecha`, `TC_APR_001_Reaprobar_devuelve_TRANSICION_INVALIDA` | OK |
| COM-001 | aprobaciones | Completa `APROBADA` manualmente y al vencer `fin`, sin actor en el caso automático. | `Application/Reservas/CompletarReserva.cs`, `Web/BackgroundServices/CerrarReservasVencidasWorker.cs` | `TC_COM_001_Automatico_registra_fecha_sin_actor_cuando_fin_vencio`, `TC_COM_001_Worker_cierra_solo_las_aprobadas_vencidas` | OK |
| NOT-001 | notificaciones | Outbox `CORREO`/`PENDIENTE` en la misma transacción al aprobar/rechazar/cancelar, nunca al crear `PENDIENTE`. | `Application/Notificaciones/NotificacionOutboxHandler.cs`, `Infrastructure/Persistence/Notificacion.cs` | `TC_NOT_001_Aprobacion_inserta_outbox_y_auditoria_en_la_misma_transaccion`, `TC_NOT_001_Creacion_en_PENDIENTE_no_genera_notificacion` | OK |
| NOT-002 | notificaciones | Envía, marca `ENVIADA`, reintenta con `ultimo_error` y pasa a `ERROR` tras 5 fallos. | `Application/Notificaciones/ProcesadorNotificaciones.cs`, `Infrastructure/Notificaciones/SmtpEmailSender.cs` | `TC_NOT_002_Envio_exitoso_marca_la_notificacion_como_ENVIADA`, `TC_NOT_002_Quinto_fallo_pasa_a_ERROR_y_deja_de_reintentarse` | OK |
| NOT-003 | notificaciones | Falla del proveedor: reserva aprobada y outbox pendiente; falla del outbox: se revierte la transición. | `Application/Notificaciones/NotificacionOutboxHandler.cs`, `Web/BackgroundServices/NotificacionesWorker.cs` | `TC_NOT_003_SMTP_caido_deja_la_reserva_APROBADA_y_el_outbox_PENDIENTE`, `TC_NOT_003_Si_falla_el_outbox_la_transicion_de_la_reserva_se_revierte` | OK |
| AUD-001 | auditoria | Registro solo inserción de entidad, id, acción, actor y fecha; rechaza borrado y actualización. | `Application/Auditoria/AuditoriaHandler.cs`, `Infrastructure/Auditoria/AuditoriaRepository.cs` | `TC_AUD_001_Aprobacion_genera_registro_de_Reservation_con_actor_y_rol`, `TC_AUD_001_Intento_de_eliminacion_devuelve_AUDITORIA_INMUTABLE` | OK |
| AUD-002 | auditoria | Consulta filtrable por entidad/actor/fechas para gestión; denegada al solicitante. | `Application/Auditoria/ConsultarAuditoria.cs`, `Web/Components/Pages/Auditoria.razor` | `TC_AUD_002_Consulta_filtrada_por_entidad_actor_y_rango_de_fechas`, `TC_AUD_002_Solicitante_no_puede_consultar_la_auditoria` | OK |
| REP-001 | reportes | Reporte por espacio y fechas con reservas, horas y ocupación de `APROBADA`/`COMPLETADA`. | `Application/Reportes/ConsultarReporteOcupacion.cs`, `Infrastructure/Reportes/RepositorioOcupacion.cs` | `TC_REP_001_Reporte_por_espacio_excluye_canceladas_y_filtra_por_fechas`, `TC_REP_001_La_vista_muestra_las_metricas_de_ocupacion` | OK |
| REP-002 | reportes | Reporte solo para coordinador/admin; `NO_AUTORIZADO` en comando, endpoint y vista. | `Application/Reportes/ConsultarReporteOcupacion.cs` (guardia), `Web/Components/Pages/ReporteOcupacion.razor` | `TC_REP_002_Rol_con_permiso_consulta_el_reporte`, `TC_REP_002_Solicitante_recibe_NO_AUTORIZADO_en_el_endpoint` | OK |
| REP-003 | reportes | Denominador `(cierre-apertura) × días naturales` en Bogotá; horas como unión sin duplicar solapes. | `Application/Reportes/ICalculoOcupacion.cs`, `ReporteBuilder.cs` | `TC_REP_003_Denominador_usa_dos_dias_naturales_de_Bogota_y_da_20_horas`, `TC_REP_003_Intervalos_solapados_no_se_duplican` | OK |
| UI-001 | interfaz | Portada y menú con solo accesos permitidos por rol; el anónimo ve disponibilidad, registro y acceso. | `Web/Components/Pages/Home.razor`, `Web/Components/Layout/NavMenu.razor` | `TC_UI_001_Portada_muestra_accesos_segun_el_rol`, `TC_UI_001_Menu_anonimo_muestra_login_registro_y_oculta_operaciones` | OK |
| UI-002 | interfaz | Login por formulario HTTP que emite la cookie en la misma petición y muestra códigos estables. | `Web/ApiEndpoints.cs` (`POST /acceso/autenticar`), `Web/Components/Pages/Login.razor` | `TC_UI_002_Login_formulario_emite_cookie_y_muestra_el_menu_del_rol`, `TC_UI_002_Login_muestra_el_codigo_de_la_query` | OK |
| UI-003 | interfaz | Autorregistro desde Razor reutilizando `USR-001` y redirección al acceso tras el éxito. | `Web/Components/Pages/Registro.razor`, `Application/Usuarios/RegistrarSolicitante.cs` | `TC_UI_003_Registro_valido_redirige_al_acceso`, `TC_UI_003_Registro_muestra_el_codigo_devuelto_por_el_comando` | OK |
| UI-004 | interfaz | Cierre de sesión redirige a portada; se conserva el endpoint JSON con `{"cerrada":true}`. | `Web/Components/Layout/NavMenu.razor`, `Web/ApiEndpoints.cs` | `TC_UI_004_Logout_formulario_redirige_a_la_portada_y_elimina_la_cookie`, `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` | OK (observación: la prueba HTTP no asevera el cuerpo `{"cerrada":true}`) |
| UI-005 | interfaz | Hora `HH:mm` y fecha `dd/MM/yyyy` en columnas separadas; formularios anuncian `HH:mm` en `America/Bogota`. | `Web/FormatoFechaHora.cs`, `Web/Components/Pages/HistorialReservas.razor` | `TC_UI_005_Vistas_muestran_hora_HH_mm_y_fecha_separada`, `TC_UI_005_Formatea_hora_fecha_y_fecha_hora_en_zona_bogota`, `TC_UI_005_Formulario_alta_anuncia_el_formato_HH_mm` | OK |
| UI-006 | interfaz | Cancelación por fila solo en reserva propia cancelable, con motivo y recarga del historial. | `Web/Components/Pages/HistorialReservas.razor` (`PuedeCancelar`, `CancelarAsync`), `Application/Reservas/CancelarReserva.cs` | `TC_UI_006_Solicitante_ve_cancelar_solo_en_reserva_propia_cancelable`, `TC_UI_006_Solicitante_cancela_con_motivo_y_recarga_el_historial` | OK |

## 5. Comprobaciones transversales

- **Roles exactos:** `Solicitante`, `Coordinador de espacios`, `Administrador del sistema`; códigos persistidos `SOLICITANTE`, `COORDINADOR`, `ADMINISTRADOR`.
- **Estados:** `PENDIENTE`, `APROBADA`, `RECHAZADA`, `COMPLETADA`, `CANCELADA`; transiciones delegadas en el State concreto.
- **Intervalos:** semiabiertos `[inicio, fin)`; la restricción `EXCLUDE` de PostgreSQL y la revalidación `SERIALIZABLE` siguen siendo la última barrera anti-solapes.
- **Límite de duración:** `RES-005` es invariante del agregado; 4 h exactas se aceptan y 4 h + 1 min se rechaza con `DURACION_MAXIMA_EXCEDIDA`.
- **Migraciones:** revisadas `btree_gist`, `EXCLUDE ... WHERE estado IN ('PENDIENTE','APROBADA')`, columnas de auditoría y trigger `audit_logs_inmutable`.
- **Código:** sin `TODO`/`FIXME` pendientes en fuentes; sin paquetes NuGet añadidos a la stack del plan §3.2.
- **OpenSpec:** no hay changes activos; los siete changes están archivados con sus specs sincronizadas.

## 6. Changes archivados

| Change | Fecha de archivo |
|---|---|
| `add-espacios-y-disponibilidad` | 2026-11-20 |
| `add-reservas-y-aprobaciones` | 2026-11-19 |
| `add-notificaciones-auditoria-y-reportes` | 2026-11-20 |
| `add-interfaz-de-acceso-y-navegacion` | 2026-11-20 |
| `fix-login-cookie-interactive-server` | 2026-11-20 |
| `fix-salida-y-historial-reservas` | 2026-11-20 |
| `add-limite-duracion-reserva` | 2026-11-20 |

## 7. Decisiones técnicas

El índice de decisiones está en [`docs/adr/ADR-004-decisiones-de-cierre.md`](adr/ADR-004-decisiones-de-cierre.md), que referencia ADR-001, ADR-002, ADR-003 y los `design.md` de los changes archivados. `PLAN_INGENIERIA.md` no se modificó.

## 8. Desviaciones y pendientes

- **GAP documentado en `RES-005`:** el escenario «Sin efecto retroactivo» (spec `reservas`, líneas 128-131) no tiene prueba automatizada. La trazabilidad de Fase 4 citaba `TC_RES_004_Filtro_por_estado` y `TC_RES_004_Vista_envia_los_filtros_al_consultar`, que solo comprueban filtros del historial y no una reserva previa de más de cuatro horas; ese mapeo se ha retirado. El comportamiento sí está garantizado por diseño: `Reservation` expone un constructor privado sin parámetros que EF Core usa al materializar, por lo que la regla no se ejecuta al leer filas antiguas; el change `add-limite-duracion-reserva` no incluyó migración, backfill ni cancelación automática. No se añadió la prueba en este cierre porque el alcance elegido fue consolidar las pruebas existentes sin introducir nuevo comportamiento ni cambiar el conjunto de 374 pruebas. Queda pendiente una prueba de integración que inserte por SQL una reserva antigua de más de cuatro horas, consulte su historial y verifique que se muestra sin cambios y sigue sin cancelarse.
- **Observación menor en `UI-004`:** `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` comprueba el estado 200 y la redirección, pero no asevera el cuerpo `{"cerrada":true}` del endpoint JSON. No bloquea el cierre; se registra para una futura ampliación de aserciones.
- Playwright (plan §5.1) y GitHub Actions (plan §5.3) no se implementaron en este cierre: añadirlos habría introducido dependencias y un change nuevo. Quedan registrados como desviación pendiente en `ADR-004`.
- No se generó `CHANGELOG.md`; el historial se consulta con `git log`.
- La sección «Aprobación del estudiante — PENDIENTE» de `docs/REGISTRO-USO-IA.md` permanece intacta, a la espera de firma.
- No se realizó `git push`.

## 9. Conclusión

La Fase 5 queda cerrada: pruebas en verde, cobertura por encima del umbral, specs válidas, 32 de 33 requisitos con correspondencia plena spec↔código↔prueba, un GAP de prueba documentado en `RES-005`, decisiones indexadas y documentación final consolidada.
