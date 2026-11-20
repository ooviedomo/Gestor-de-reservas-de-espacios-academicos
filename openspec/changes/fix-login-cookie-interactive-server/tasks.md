# Tasks

## 1. Formulario de acceso estático

- [x] 1.1 Convertir `Login.razor` en página sin `InteractiveServer`: `<form method="post" action="/acceso/autenticar">` con campos `name="email"` y `name="contrasena"`, sin inyección del handler ni `AccederAsync`; verificar que el archivo ya no contiene `@rendermode InteractiveServer`.
- [x] 1.2 Mostrar el error desde el parámetro `error` de la query, limitado a `CREDENCIALES_INVALIDAS` y `CUENTA_DESACTIVADA`, conservando `data-testid="error"`; verificar con bUnit que `?error=CREDENCIALES_INVALIDAS` renderiza el código y que un valor arbitrario no se muestra.
- [x] 1.3 Ajustar `tests/ReservaEspacios.E2ETests/InterfazAccesoTests.cs` para assertar `method`, `action` y `name` de los campos; verificar que esa prueba bUnit pasa.

## 2. Endpoint HTTP de formulario

- [x] 2.1 Añadir `POST /acceso/autenticar` en `ApiEndpoints.cs`: leer `email` y `contrasena` de `Request.ReadFormAsync()`, llamar a `AutenticarUsuario` y devolver `Results.Redirect("/")` si tiene éxito; verificar que `POST /api/autenticacion/login` sigue devolviendo JSON sin cambios.
- [x] 2.2 Redirigir los fallos a `/acceso/login?error=CODIGO` con el valor codificado para URI y sin cookie de sesión; verificar que los códigos son los devueltos por `USR-003` y que no se expone información del campo que falló.
- [x] 2.3 Añadir regresión HTTP en `tests/ReservaEspacios.IntegrationTests/FlujoAccesoWebTests.cs` con `WebApplicationFactory`: POST real válido → `302`, cabecera `Set-Cookie` y portada con la etiqueta del rol; verificar que la prueba usa el formulario (no el endpoint JSON) y que fallaría si no se emitiera la cookie.

## 3. Cobertura y documentación

- [x] 3.1 Ampliar la regresión HTTP con credenciales inválidas y cuenta desactivada: sin `Set-Cookie`, redirect con el código correcto; verificar que ambas pruebas pasan con PostgreSQL local.
- [x] 3.2 Registrar en `docs/REGISTRO-USO-IA.md` los prompts/correcciones de este fix; verificar que el archivo queda actualizado y sin secretos.
- [x] 3.3 Ampliar el bootstrap para renovar la contraseña de cuentas existentes, rotar la del coordinador en `C:\Users\Undfold\.reservaespacios-bootstrap.env` (fuera del repo) y verificar el acceso con la nueva credencial sin imprimirla ni versionarla.

## 4. Verificación final

- [x] 4.1 Ejecutar `dotnet format --verify-no-changes --no-restore` y `dotnet build`; verificar 0 errores y 0 advertencias.
- [x] 4.2 Ejecutar la suite completa con PostgreSQL local y medir cobertura; verificar que todas las pruebas pasan y que no baja respecto a la línea base (95.1% de líneas).
- [x] 4.3 Ejecutar `openspec validate fix-login-cookie-interactive-server --strict` y `openspec validate --all --strict`; verificar que todos los changes y specs son válidos.
- [x] 4.4 Revisar `git diff` y `git status` antes de commitear; verificar que no hay secretos, archivos temporales ni dependencias nuevas.

## Workflow follow-up

- Commitear la implementación.
- Archivar el change y sincronizar `specs/interfaz` tras la aprobación.
- Verificar que `git status` queda limpio y no pushear sin instrucción explícita.
