# Trazabilidad: `add-interfaz-de-acceso-y-navegacion`

Verificación de la tarea 7.1: cada requisito `UI-001`…`UI-004` del delta tiene al menos
un escenario vinculado a una prueba ejecutable. Los nombres son los de los métodos de
prueba en `tests/`. `UI-005` y `UI-006`, añadidos después por
`fix-salida-y-historial-reservas`, se documentan al final para completar la matriz de
interfaz en la Fase 5.

## UI-001 — Portada y navegación por rol

| Escenario del spec | Pruebas |
| --- | --- |
| Visitante sin sesión | `TC_UI_001_Portada_no_es_el_scaffold_y_el_anonimo_ve_solo_acceso` (sin enlaces de sesión en el cuerpo), `TC_UI_001_Menu_anonimo_muestra_login_registro_y_oculta_operaciones` (acceso y registro en la cabecera) |
| Fallo al cargar el catálogo de disponibilidad | `TC_DISP_001_Componente_muestra_mensaje_si_el_catalogo_falla` (mensaje amable, sin SQL ni códigos de PostgreSQL) |
| Solicitante autenticado | `TC_UI_001_Portada_muestra_accesos_segun_el_rol(Rol.Solicitante, …)`, `TC_UI_001_Menu_filtra_enlaces_y_muestra_la_etiqueta_del_rol(Rol.Solicitante, …)` |
| Coordinador autenticado | `TC_UI_001_Portada_muestra_accesos_segun_el_rol(Rol.Coordinador, …)`, `TC_UI_001_Menu_filtra_enlaces_y_muestra_la_etiqueta_del_rol(Rol.Coordinador, …)` |
| Administrador autenticado | `TC_UI_001_Portada_muestra_accesos_segun_el_rol(Rol.Administrador, …)`, `TC_UI_001_Menu_filtra_enlaces_y_muestra_la_etiqueta_del_rol(Rol.Administrador, …)` |

La Strategy `EvaluadorPermisos` es la misma que usan los guards, por lo que la matriz no
se duplica en Razor.

## UI-002 — Inicio de sesión desde la interfaz

| Escenario del spec | Pruebas |
| --- | --- |
| Inicio válido | `TC_UI_002_Login_valido_redirige_a_la_portada`, `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` |
| Credenciales inválidas | `TC_UI_002_Login_muestra_el_codigo_devuelto_por_el_comando("CREDENCIALES_INVALIDAS")` |
| Cuenta desactivada | `TC_UI_002_Login_muestra_el_codigo_devuelto_por_el_comando("CUENTA_DESACTIVADA")` |

## UI-003 — Registro desde la interfaz

| Escenario del spec | Pruebas |
| --- | --- |
| Registro válido | `TC_UI_003_Registro_valido_redirige_al_acceso`, `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` (registro previo por API) |
| Registro con email duplicado | `TC_UI_003_Registro_muestra_el_codigo_devuelto_por_el_comando("EMAIL_DUPLICADO")` |
| Registro con contraseña débil | `TC_UI_003_Registro_muestra_el_codigo_devuelto_por_el_comando("CONTRASENA_DEBIL")` |

## UI-004 — Cierre de sesión

| Escenario del spec | Pruebas |
| --- | --- |
| Cerrar sesión | `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` (cookie → portada autenticada → logout → menú anónimo) |
| Endpoint JSON heredado | `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` (estado 200 y sin redirección; observación: no asevera el cuerpo `{"cerrada":true}`) |

## UI-005 — Formato de fecha y hora (change `fix-salida-y-historial-reservas`)

| Escenario del spec | Pruebas |
| --- | --- |
| Hora `HH:mm` y fecha en columnas separadas | `TC_UI_005_Vistas_muestran_hora_HH_mm_y_fecha_separada` |
| Zona `America/Bogota` y nulos | `TC_UI_005_Formatea_hora_fecha_y_fecha_hora_en_zona_bogota`, `TC_UI_005_Valores_nulos_muestran_guion` |
| Formulario de alta | `TC_UI_005_Formulario_alta_anuncia_el_formato_HH_mm` |
| Formulario de disponibilidad | `TC_UI_005_Formulario_disponibilidad_anuncia_el_formato_HH_mm` |
| Enlaces de auditoría y reportes según rol | `TC_UI_005_Rol_privilegiado_ve_enlaces_de_auditoria_y_reporte`, `TC_UI_005_Solicitante_no_ve_enlaces_de_auditoria_ni_reporte` |

Formateador centralizado en `src/ReservaEspacios.Web/FormatoFechaHora.cs`; sin patrón nuevo de ADR-002.

## UI-006 — Cancelación desde el historial (change `fix-salida-y-historial-reservas`)

| Escenario del spec | Pruebas |
| --- | --- |
| Botón solo en reserva propia y cancelable | `TC_UI_006_Solicitante_ve_cancelar_solo_en_reserva_propia_cancelable` |
| Cancelar con motivo y recargar | `TC_UI_006_Solicitante_cancela_con_motivo_y_recarga_el_historial` |
| Motivo obligatorio | `TC_UI_006_Motivo_obligatorio_no_envia_el_comando` |

Reutiliza `CancelarReservaCommand` y el guard de propiedad/estado; no añade patrón ni dependencia.

## Patrones aplicados (ADR-002)

| Patrón | Implementación | Pruebas |
| --- | --- | --- |
| Command | `AutenticarUsuarioCommand`, `RegistrarSolicitanteCommand` inyectados en Razor | `TC_UI_002_Login_muestra_el_codigo_devuelto_por_el_comando`, `TC_UI_003_Registro_muestra_el_codigo_devuelto_por_el_comando` |
| Strategy | `IEvaluadorPermisos` consultado por `Home` y `NavMenu` | `TC_UI_001_Portada_muestra_accesos_segun_el_rol`, `TC_UI_001_Menu_filtra_enlaces_y_muestra_la_etiqueta_del_rol` |
| Adapter | `SesionWeb` sobre `SignInManager.SignOutAsync()`; `IdentityRoleAdapter` reutilizado | `TC_UI_002_004_Login_cookie_portada_autenticada_y_logout` |
| Facade | `IReservaEspaciosFacade` reutilizado por las páginas de reservas | Sin cambios en este change |

No se añaden patrones nuevos ni dependencias NuGet.

## Verificación ejecutada

- `dotnet build ReservaEspacios.sln`: 0 advertencias, 0 errores.
- `dotnet format ReservaEspacios.sln --verify-no-changes --no-restore`: sin cambios.
- `dotnet test ReservaEspacios.sln`: **287** pruebas superadas (169 unitarias, 36 bUnit, 82 integración).
- Cobertura de líneas consolidada (coverlet + ReportGenerator): **94.2%** (3567/3783).
  Por ensamblado: Application 95.0% (723/761), Domain 92.6% (316/341),
  Infrastructure 96.0% (1901/1980), Web 89.4% (627/701).
- `openspec validate add-interfaz-de-acceso-y-navegacion --strict`: válido.

### Verificación final Fase 5 (2026-11-20)

Solución completa reejecutada: **374** pruebas (215 unitarias, 100 integración, 59 bUnit),
cobertura **95.2%** de líneas (5210/5467) y **79.5%** de ramas (729/916), build 0/0,
formato sin cambios y `openspec validate --all --strict`: 10/10 specs válidos. Las cifras
anteriores se conservan como registro del change. Detalle en
[`VERIFICACION-FINAL.md`](VERIFICACION-FINAL.md).
