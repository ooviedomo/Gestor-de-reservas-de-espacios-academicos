# Proposal

## Why

La aplicación arranca y expone las páginas de reservas, pero la portada sigue siendo el
scaffold de Blazor (`Hello, world!`), `MainLayout` no tiene menú y no existe una forma
visible de iniciar sesión. Sin sesión, `IdentityRoleAdapter` devuelve `UsuarioId = null`
y los comandos protegidos responden `NO_AUTENTICADO`; el panel de aprobación muestra
`NO_AUTORIZADO`. La funcionalidad está implementada y probada, pero una persona no puede
recorrerla desde el navegador.

## What Changes

- Sustituir la portada de plantilla por una página de acceso a los flujos disponibles para
  el rol autenticado.
- Añadir un menú de navegación que filtre los enlaces con la matriz canónica de permisos.
- Añadir páginas Razor de inicio de sesión y autorregistro que reutilicen los comandos
  `AutenticarUsuario` y `RegistrarSolicitante` ya existentes.
- Añadir el cierre de sesión y una verificacación HTTP del flujo completo con cookies.
- Documentar el arranque local y un bootstrap operativo de los tres roles sin versionar
  credenciales.

Fuera de alcance: notificaciones, auditoría y reportes (tercer change); cambios en las
reglas de negocio o en la matriz de permisos; nuevas dependencias NuGet.

## Capabilities

### New Capabilities

- `interfaz`: portada, navegación y flujo de acceso visible para los tres roles.

### Modified Capabilities

- Ninguna. `usuarios`, `autorizacion`, `reservas` y `aprobaciones` se consumen sin cambiar
  sus requisitos.

## Impact

- Proyectos afectados: principalmente `Web`, con un registro adicional de comandos en
  `Application`.
- Datos: ningún cambio de esquema.
- Seguridad: las credenciales siguen en variables de entorno/user-secrets (ADR-003).
- Interfaz: `Home`, `MainLayout`, nuevo `NavMenu`, `Login` y `Registro`.
