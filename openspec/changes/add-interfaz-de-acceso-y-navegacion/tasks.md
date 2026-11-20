# Tasks

## 1. Especificación

- [x] 1.1 Crear `interfaz/spec.md` con `UI-001`…`UI-004` y validar el change con `openspec validate add-interfaz-de-acceso-y-navegacion --strict`.

## 2. Sesión y comandos

- [x] 2.1 Registrar `ICommandHandler<AutenticarUsuarioCommand, UsuarioSesionDto>` e `ICommandHandler<RegistrarSolicitanteCommand, Guid>` en `Application/DependencyInjection.cs`.
- [x] 2.2 Implementar `ISesionWeb` / `SesionWeb` sobre `SignInManager.SignOutAsync()` y el endpoint `POST /api/autenticacion/logout`; verificar con pruebas que el logout limpia la sesión.

## 3. Portada y navegación

- [x] 3.1 Crear `NavMenu.razor` e integrarlo en `MainLayout.razor`, filtrando enlaces por `IEvaluadorPermisos` y `EstaAutenticado`; verificar con bUnit los enlaces visibles para anónimo y para cada rol.
- [x] 3.2 Sustituir `Home.razor` por la portada de accesos; verificar con bUnit que no aparece `Hello, world!` y que los accesos respetan el rol.

## 4. Acceso visible

- [x] 4.1 Crear `Login.razor` en `/acceso/login`: éxito con recarga completa y errores `CREDENCIALES_INVALIDAS` / `CUENTA_DESACTIVADA`; verificar con bUnit.
- [x] 4.2 Crear `Registro.razor` en `/acceso/registro`: reutilizar `RegistrarSolicitanteCommand`, mostrar `EMAIL_DUPLICADO` / `CONTRASENA_DEBIL` y redirigir a login; verificar con bUnit.

## 5. Verificación HTTP

- [x] 5.1 Añadir pruebas con `WebApplicationFactory`: login real por API → cookie → portada con menú autenticado → logout → menú anónimo.

## 6. Arranque local

- [x] 6.1 Documentar en `README.md` la cadena de conexión por user-secrets, las migraciones y las rutas; añadir un script de bootstrap de los tres roles sin credenciales versionadas.

## 7. Cierre del change

- [ ] 7.1 Ejecutar build, format y todas las pruebas; verificar cobertura ≥ 80%, generar la matriz de trazabilidad, actualizar el registro de uso de IA, commitear y archivar el change.
