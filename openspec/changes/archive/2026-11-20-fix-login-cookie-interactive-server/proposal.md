# Proposal

## Why

El inicio de sesión desde la UI falla en ejecución real con `Headers are read-only,
response has already started`: el formulario `EditForm` de `Login.razor` ejecuta
`AutenticarUsuario` dentro de un evento `InteractiveServer`, cuando Kestrel ya inició la
respuesta y no puede añadir la cabecera `Set-Cookie`. El endpoint JSON funciona, pero una
persona no puede entrar con el navegador; por tanto la corrección es bloqueante para
cualquier flujo autenticado.

## What Changes

- Sustituir el formulario interactivo de acceso por un formulario HTML normal que se
  envía al servidor en una petición HTTP propia, sin circuito SignalR.
- Añadir `POST /acceso/autenticar`, un endpoint HTTP de formulario que reutiliza `USR-003`
  (`AutenticarUsuario`), emite la cookie de Identity en esa misma respuesta y redirige a
  la portada.
- Los fallos de autenticación redirigen al formulario con el código de error en la query
  (`CREDENCIALES_INVALIDAS`, `CUENTA_DESACTIVADA`), sin revelar qué campo falló.
- Conservar `POST /api/autenticacion/login` y su contrato JSON para clientes que no son
  la UI; no se crea una segunda lógica de autenticación.
- Añadir regresión HTTP con `WebApplicationFactory` (cabecera `Set-Cookie`, menú por rol,
  fallo sin cookie) y ajustar las pruebas bUnit del formulario.
- Permitir que el bootstrap local renueve la contraseña de una cuenta ya creada, para
  poder rotar la credencial del coordinador que se compartió por error.
- Registrar la decisión en el `design.md` del change; no se reabre ningún change
  archivado ni se modifica `PLAN_INGENIERIA.md`.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `interfaz`: `UI-002` pasa a exigir que la sesión se establezca en la respuesta HTTP del
  envío del formulario, con los códigos de error existentes devueltos por redirección.

## Impact

- `src/ReservaEspacios.Web/Components/Pages/Login.razor`: deja de inyectar el handler y
  de ejecutar `AccederAsync`; pasa a ser un formulario estándar.
- `src/ReservaEspacios.Web/ApiEndpoints.cs`: nuevo `POST /acceso/autenticar`; el endpoint JSON
  queda intacto.
- `tests/ReservaEspacios.E2ETests/InterfazAccesoTests.cs` y
  `tests/ReservaEspacios.IntegrationTests/FlujoAccesoWebTests.cs`.
- `docs/REGISTRO-USO-IA.md`.
- `tools/ReservaEspacios.Bootstrap/Program.cs`: renueva la contraseña cuando el email
  ya existe; sigue leyendo credenciales solo de variables de entorno.
- Sin dependencias NuGet nuevas, sin migraciones y sin cambios en la capa de dominio,
  .Application o .Infrastructure. La cookie sigue siendo emitida por
  `SignInManager.PasswordSignInAsync` a través del caso de uso ya existente.
