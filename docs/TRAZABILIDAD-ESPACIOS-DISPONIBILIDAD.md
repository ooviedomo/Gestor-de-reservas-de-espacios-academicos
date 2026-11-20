# Trazabilidad: `add-espacios-y-disponibilidad`

Verificación de la tarea 4.2: cada requisito `ESP-*`/`DISP-*` del delta tiene al menos
un escenario vinculado a una prueba ejecutable. Los nombres son los de los métodos
de prueba en `tests/`.

## ESP-001 — Registro de espacio académico

| Escenario del spec | Pruebas |
| --- | --- |
| Registro válido | `TC_ESP_001_Registro_valido_queda_activo`, `TC_ESP_001_Registro_valido_guarda_el_espacio` |
| Nombre duplicado | `TC_ESP_001_Nombre_duplicado_devuelve_error`, `TC_ESP_001_Nombre_duplicado_se_rechaza_en_base_de_datos`, `TC_ESP_001_UnitOfWork_traduce_violacion_de_unicidad_a_NOMBRE_DUPLICADO` |
| Capacidad inválida | `TC_ESP_001_Capacidad_invalida_se_rechaza`, `TC_ESP_001_Capacidad_invalida_devuelve_error` |
| Horario inválido | `TC_ESP_001_Horario_invalido_se_rechaza`, `TC_ESP_001_Horario_con_apertura_igual_al_cierre_se_rechaza`, `TC_ESP_001_Horario_invalido_devuelve_error`, `TC_ESP_001_Horario_invalido_se_rechaza_por_check_constraint` |

## ESP-002 — Administración del catálogo

| Escenario del spec | Pruebas |
| --- | --- |
| Edición de capacidad | `TC_ESP_002_Edicion_de_capacidad_no_afecta_reservas_previas` |
| Desactivación con reservas futuras | `TC_ESP_002_Desactivacion_con_reserva_futura_la_conserva` |
| Aviso al desactivar con reservas futuras | `TC_ESP_002_Desactivar_con_reservas_futuras_muestra_aviso_y_confirma` |
| Reactivación | `TC_ESP_002_Reactivacion_devuelve_el_espacio_al_catalogo` |

## ESP-003 — Catálogo filtrable

| Escenario del spec | Pruebas |
| --- | --- |
| Filtro por tipo | `TC_ESP_003_Filtro_por_tipo_devuelve_solo_salas_de_computo_activas` |
| Filtro por capacidad mínima | `TC_ESP_003_Filtro_por_capacidad_minima` |
| Espacio desactivado excluido por defecto | `TC_ESP_003_Espacio_desactivado_excluido_por_defecto` |
| Filtro de estado para rol de gestión | `TC_ESP_003_Filtro_de_estado_para_rol_de_gestion`, `TC_ESP_003_Administrador_tambien_puede_filtrar_inactivos` |
| Filtro de estado denegado a solicitante | `TC_ESP_003_Filtro_de_estado_denegado_a_solicitante`, `TC_ESP_003_Solicitante_no_puede_escapar_con_estado_todos` |

## DISP-001 — Consulta de disponibilidad por franja

| Escenario del spec | Pruebas |
| --- | --- |
| Espacio libre | `TC_DISP_001_Espacio_libre`, `TC_DISP_001_Endpoint_devuelve_DISPONIBLE` |
| Espacio inactivo | `TC_DISP_001_Espacio_inactivo`, `TC_DISP_001_Endpoint_devuelve_ESPACIO_INACTIVO` |
| Franja fuera del horario | `TC_DISP_001_Fuera_del_horario_de_atencion`, `TC_DISP_001_Endpoint_devuelve_FUERA_DE_HORARIO` |

## DISP-002 — Reglas de ocupación de la franja

| Escenario del spec | Pruebas |
| --- | --- |
| Reserva `PENDIENTE` bloquea | `TC_DISP_002_Reserva_bloqueante_solapa("PENDIENTE")` |
| Reserva `APROBADA` bloquea | `TC_DISP_002_Reserva_bloqueante_solapa("APROBADA")` |
| Reserva rechazada/cancelada/completada no bloquea | `TC_DISP_002_Reserva_no_bloqueante_no_impide_la_franja` (3 casos de datos) |
| Reservas contiguas | `TC_DISP_002_Reservas_contiguas_no_se_solapan` |
| Solape parcial | `TC_DISP_002_Solape_parcial_por_el_inicio`, `TC_DISP_002_Endpoint_devuelve_SOLAPE_CON_RESERVA` |

## DISP-003 — Zona horaria en el horario de atención

| Escenario del spec | Pruebas |
| --- | --- |
| Franja que termina en la hora de cierre | `TC_DISP_003_Franja_que_termina_en_la_hora_de_cierre_esta_dentro` |
| Franja que empieza en la hora de cierre | `TC_DISP_003_Franja_que_empieza_en_la_hora_de_cierre_esta_fuera` |
| Comparación en UTC (franja dada en `DateTimeOffset` UTC) | `TC_DISP_003_La_comparacion_se_hace_en_UTC` |

## Patrones adoptados (ADR-002, tarea 4.3)

| Patrón | Implementación | Pruebas |
| --- | --- | --- |
| Builder | `FiltroCatalogoBuilder` | `TC_ESP_003_Builder_por_defecto_solo_activos`, `TC_ESP_003_Builder_encadena_todos_los_filtros` |
| Decorator | `DisponibilidadCacheDecorator` | `TC_DISP_DEC_001_Dos_consultas_identicas_llaman_al_inner_una_sola_vez`, `TC_DISP_DEC_002_Franja_distinta_no_usa_la_cache` |
| Facade | `ReservaEspaciosFacade` | `TC_FACADE_001_ConsultarCatalogo_delega_en_el_caso_de_uso`, `TC_FACADE_002_ConsultarDisponibilidad_delega_en_el_caso_de_uso` |
| Aislamiento del stub de reservas | `StubDisponibilidadRepository` | `TC_DISP_AISL_001_Stub_no_devuelve_reservas_y_no_toca_base_de_datos` |

## Verificación ejecutada

- `dotnet format ReservaEspacios.sln --verify-no-changes`: sin cambios.
- `dotnet build ReservaEspacios.sln`: 0 advertencias, 0 errores.
- `dotnet test ReservaEspacios.sln`: 59 pruebas superadas (39 unitarias, 14 integración, 6 bUnit).
- Cobertura de líneas consolidada (coverlet + reportgenerator): **91.7%** (649/707).
- `openspec validate add-espacios-y-disponibilidad --strict`: válido.

### Verificación final Fase 5 (2026-11-20)

Solución completa reejecutada: **374** pruebas (215 unitarias, 100 integración, 59 bUnit),
cobertura **95.2%** de líneas (5210/5467) y **79.5%** de ramas (729/916), build 0/0,
formato sin cambios y `openspec validate --all --strict`: 10/10 specs válidos. La cifra
anterior se conserva como registro del change. Detalle en
[`VERIFICACION-FINAL.md`](VERIFICACION-FINAL.md).
