# Design

## Context

El change archivado `add-reservas-y-aprobaciones` entregó identidad, permisos y todas las
páginas de reservas, pero dejó la portada de plantilla y no incluyó acceso visible desde
la UI. Las pruebas de integración inyectaban `IUsuarioActual` para simular la sesión; en
la aplicación real ese valor depende de los claims de Identity.

**Corrección a una suposición inicial:** se pensó que `POST /api/autenticacion/login` no
establecía cookie. No es así: `IdentityUsuarioService.AutenticarAsync` llama a
`SignInManager.PasswordSignInAsync`, que emite la cookie de `IdentityConstants.ApplicationScheme`.
El vacío real es que no hay componentes Razor de login/registro ni de cierre de sesión, y
que la portada y el menú no existen.

## Goals / Non-Goals

**Goals:**

- Que una persona pueda entrar, registrarse como `Solicitante` y navegar a los flujos que
  su rol permite sin conocer las URLs.
- Reutilizar los comandos y la Strategy de permisos existentes, sin duplicar reglas.

**Non-Goals:**

- Cambiar la matriz de permisos o las reglas de USR-001/002/003.
- Auditoría, notificaciones y reportes.
- Añadir paquetes a la stack del plan §3.2.

## Decisions

- **Login y registro por comandos existentes.** `Login.razor` inyecta
  `ICommandHandler<AutenticarUsuarioCommand, UsuarioSesionDto>` y `Registro.razor` inyecta
  `ICommandHandler<RegistrarSolicitanteCommand, Guid>`. Se registran las interfaces en
  `Application/DependencyInjection.cs`, igual que los comandos de reservas (ADR-002,
  Command). No se construye un `ClaimsPrincipal` a mano: `PasswordSignInAsync` ya lo hace
  en Infrastructure.
- **Recarga completa tras autenticarse.** `NavigationManager.NavigateTo(..., forceLoad: true)`
  después de login, registro y logout. En `InteractiveServer` el circuito conserva el
  `HttpContext` inicial; sin recarga, `IdentityRoleAdapter` no vería los claims nuevos.
- **Cierre de sesión en Web.** `ISesionWeb` / `SesionWeb` envuelven
  `SignInManager.SignOutAsync()`; es el único paso que no existe hoy (Adapter sobre
  Identity, ADR-002). Se expone además `POST /api/autenticacion/logout` para simetría con
  el resto de endpoints.
- **Navegación gobernada por Strategy.** `NavMenu` y `Home` consultan
  `IEvaluadorPermisos` con el `Rol` de `IUsuarioActual` y exigen `EstaAutenticado` para
  los enlaces protegidos. No se escribe ninguna comparación de roles en los componentes.
- **Sin antiforgery artesanal.** Los componentes inyectan handlers directamente, como ya
  hacen las páginas de reservas; no hacen `fetch` a las APIs.
- **Bootstrap operativo, sin secretos versionados.** La herramienta
  `tools/ReservaEspacios.Bootstrap` crea los tres roles leyendo credenciales de entorno.
  El `Solicitante` usa `RegistrarSolicitanteCommand`; el primer `Administrador` y el
  `Coordinador` usan directamente el puerto `IUsuarioIdentityService` porque
  `CrearUsuarioPrivilegiadoCommand` exige que ya exista un administrador. Las altas
  privilegiadas posteriores sí pasan por el guard (`POST /api/usuarios`). Coherente con
  la desviación registrada en el `design.md` del change archivado.
- **Sin tocar `PLAN_INGENIERIA.md`.** Toda decisión nueva vive aquí.

## Patrones de diseño (ADR-002)

- **Command:** `AutenticarUsuario`, `RegistrarSolicitante` y el resto de casos de uso.
- **Strategy:** `IEvaluadorPermisos` decide qué enlaces se muestran.
- **Adapter:** `IdentityRoleAdapter` (claims → dominio) y el nuevo `SesionWeb` (sign-out).
- **Facade:** `IReservaEspaciosFacade` sigue siendo el acceso de la UI a catálogo y
  disponibilidad.

## Risks / Trade-offs

- **[Riesgo] Claims obsoletos en el circuito InteractiveServer.** → Mitigación: `forceLoad`.
- **[Riesgo] Duplicar la matriz de permisos en Razor.** → Mitigación: uso obligatorio de
  `IEvaluadorPermisos`.
- **[Riesgo] Usuario sin sesión ve enlaces protegidos.** → Mitigación: `EstaAutenticado`
  además del permiso.
- **[Trade-off] El login tiene efecto de sesión dentro de un caso de uso de Application.**
  → Ya es el comportamiento existente y la API depende de él; duplicarlo en Web sería peor.
