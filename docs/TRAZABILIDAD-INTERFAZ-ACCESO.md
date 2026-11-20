# Trazabilidad: `add-interfaz-de-acceso-y-navegacion`

Verificación de la tarea 7.1: cada requisito `UI-001`…`UI-004` del delta tiene al menos
un escenario vinculado a una prueba ejecutable. Los nombres son los de los métodos de
prueba en `tests/`.

## UI-001 — Portada y navegación por rol

| Escenario del spec | Pruebas |
| --- | --- |
| Visitante sin sesión | `TC_UI_001_Portada_no_es_el_scaffold_y_el_anonimo_ve_solo_acceso` (sin enlaces de sesión en el cuerpo), `TC_UI_001_Menu_anonimo_muestra_login_registro_y_oculta_operaciones` (acceso y registro en la cabecera) |
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
- `dotnet test ReservaEspacios.sln`: **286** pruebas superadas (169 unitarias, 35 bUnit, 82 integración).
- Cobertura de líneas consolidada (coverlet + ReportGenerator): **94.3%** (3557/3770).
  Por ensamblado: Application 95.0% (723/761), Domain 92.6% (316/341),
  Infrastructure 96.0% (1901/1980), Web 89.6% (617/688).
- `openspec validate add-interfaz-de-acceso-y-navegacion --strict`: válido.
