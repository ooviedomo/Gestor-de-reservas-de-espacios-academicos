# Design

## Context

Ver `proposal.md` para la motivación. `Login.razor` está en `InteractiveServer` y ejecuta
`ICommandHandler<AutenticarUsuarioCommand, UsuarioSesionDto>` desde `OnValidSubmit`. Ese
handler termina en `SignInManager.PasswordSignInAsync`, que escribe `Set-Cookie` en la
respuesta HTTP. En un evento interactivo la respuesta ya comenzó, por lo que Kestrel
lanza `InvalidOperationException`. En cambio, `POST /api/autenticacion/logout` ya es un
formulario HTML normal en `NavMenu.razor` y funciona: el patrón a seguir existe en el
propio proyecto. La cookie sigue siendo responsabilidad de Identity; aquí solo se cambia
el momento en que se llama.

## Goals / Non-Goals

**Goals:**

- Emitir la cookie antes de que empiece la respuesta, dentro de una petición HTTP de
  formulario gestionada por un endpoint mínimo.
- Mantener un único caso de uso de autenticación (`USR-003`) y un único conjunto de
  códigos de error para UI y API JSON.
- Que la página de acceso pueda renderizarse sin circuito interactivo.

**Non-Goals:**

- Cambiar la directiva de contraseñas, los roles, los estados ni el esquema de BD.
- Sustituir `POST /api/autenticacion/login` ni modificar su contrato JSON.
- Añadir recordatorio de contraseña, bloqueo por intentos, 2FA ni auditoría de accesos.
- Rotar credenciales existentes como parte del despliegue (se hace por separado fuera del
  repositorio).

## Decisions

1. **Formulario HTML estándar en lugar de `EditForm`/`InteractiveServer`.**
   `Login.razor` pasa a ser una página de renderizado estático con `<form method="post"
   action="/acceso/autenticar">` y campos `name="email"` / `name="contrasena"`. El componente
   ya no inyecta el handler ni tiene `AccederAsync`. Alternativa descartada: mantener el
   evento interactivo y llamar al endpoint JSON con `fetch`; sigue siendo una petición
   HTTP válida, pero añade JavaScript y degrada la página si no hay circuito. El cierre de
   sesión ya demuestra que el formulario normal es suficiente.

2. **Endpoint dedicado `POST /acceso/autenticar`.**
   Lee los valores del cuerpo con `HttpContext.Request.ReadFormAsync()` y delega en
   `AutenticarUsuario`. No enlaza parámetros desde el formulario, de modo que no depende
   de la infraestructura de enlace de formularios de minimal APIs; el cuerpo es
   `application/x-www-form-urlencoded` y el endpoint decide el código de error. La ruta
   no coincide con `@page "/acceso/login"`: la página Razor acepta todos los verbos y, si
   el POST se registra en la misma ruta, el router lanza `AmbiguousMatchException`.
   Alternativa descartada: reutilizar `/api/autenticacion/login` con `Content-Type:
   application/x-www-form-urlencoded`; mezclaría el contrato JSON con el de la UI y haría
   más frágil la lectura del comando. El endpoint JSON permanece intacto para clientes no
   navegador.

3. **Errores por redirección con query `?error=CODIGO`.**
   Ante un fallo se devuelve `Results.Redirect` a `/acceso/login?error=...`, con el valor
   codificado para URI. La página solo muestra códigos de la lista permitida
   (`CREDENCIALES_INVALIDAS`, `CUENTA_DESACTIVADA`); cualquier otro valor se ignora. Así
   se conserva el formato `data-testid="error"` que ya consumen las pruebas bUnit sin
   devolver el código como cuerpo de una respuesta 400. Alternativa descartada: mensajes
   en TempData; introduce estado de sesión para un solo valor.

4. **Sin antiforgery explícito en este formulario.**
   El endpoint no enlaza el cuerpo como parámetro, por lo que no se añade metadatos de
   validación antiforgery; el formulario de logout ya opera igual. La cookie de Identity
   usa `SameSite=Lax`, y el inicio de sesión no realiza cambios de estado más allá de la
   propia sesión. Si el pipeline pasara a exigir antiforgery en todos los POST con
   formulario, se añadiría el token y su validación sin cambiar la spec.

5. **Rotación local sin cambiar el contrato del bootstrap.**
   El bootstrap sigue creando cuentas a partir de `BOOTSTRAP_*`; cuando el email ya
   existe, usa `UserManager.GeneratePasswordResetTokenAsync` y `ResetPasswordAsync` para
   aplicar la contraseña configurada en el entorno, sin imprimirla. Alternativa
   descartada: editar `password_hash` directamente en PostgreSQL; saltaría la directiva
   de Identity y dejaría el token de seguridad sin actualizar. La rotación es una
   operación de entorno, no un cambio de spec ni de migración.

6. **Regresión en dos niveles.**
   bUnit comprueba que el formulario es estático (`method=post`, `action=/acceso/login`,
   nombres de campo) y que el error se lee de la query. La prueba de integración realiza
   un POST verdadero con `WebApplicationFactory` y verifica `302`, `Set-Cookie`, portada
   autenticada por rol y el caso inválido sin cookie; es la prueba que habría detectado el
   fallo original.

## Risks / Trade-offs

- [La autenticación queda ligada a un endpoint de formulario] → Se aísla en un único
  endpoint y el caso de uso sigue siendo compartido con la API JSON.
- [Un error de validación de antiforgery bloquea el POST] → Se valida con la prueba HTTP
  real antes de cerrar el change; si aparece, se añade el token sin tocar la spec.
- [La query del error puede contaminarse con valores externos] → La página solo muestra
  los dos códigos permitidos y Razor los codifica al renderizar.
- [El nombre del campo cambia y el endpoint deja de encontrarlo] → Hay una aserción
  `name=` en la prueba bUnit y la prueba HTTP usa el nombre real del formulario.

## Migration Plan

No hay cambios de esquema ni datos: basta con desplegar la nueva versión de la Web. No se
necesita borrar cookies ni reiniciar sesiones. La reversión consiste en volver a la versión
anterior del componente y del endpoint; no hay migración inversa.

## Open Questions

Ninguna.
