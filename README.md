# ReservaEspacios

Aplicación de reserva de espacios construida con Blazor Web App (ASP.NET Core 10),
EF Core 10 + Npgsql y PostgreSQL local. Las reglas de negocio y la matriz de permisos
viven en `Domain`/`Application`; la capa `Web` solo las expone.

## Arranque local

1. PostgreSQL 19 en `127.0.0.1:5432` con la base `reservaespacios` y el usuario
   `reserva_app`.
2. Configurar la cadena de conexión **fuera del repositorio** (ADR-003). Desde Visual
   Studio: clic derecho en `ReservaEspacios.Web` → *Manage User Secrets*:

   ```json
   {
     "ConnectionStrings": {
       "ReservaEspacios": "Host=127.0.0.1;Port=5432;Database=reservaespacios;Username=reserva_app;Password=<password-local>"
     }
   }
   ```

   Desde terminal (sustituye `<password-local>` por tu contraseña; no la escribas en
   ningún archivo versionado):

   ```powershell
   dotnet user-secrets set "ConnectionStrings:ReservaEspacios" `
     "Host=127.0.0.1;Port=5432;Database=reservaespacios;Username=reserva_app;Password=<password-local>" `
     --project src/ReservaEspacios.Web
   ```

3. Aplicar migraciones:

   ```powershell
   dotnet ef database update `
     --project src/ReservaEspacios.Infrastructure `
     --startup-project src/ReservaEspacios.Web
   ```

4. Ejecutar `ReservaEspacios.Web` (proyecto de inicio). La portada ofrece acceso,
   registro y consulta de disponibilidad.

## Cuentas de prueba (bootstrap)

No hay usuarios semilla insertados desde código: las credenciales nunca se versionan.
La herramienta `tools/ReservaEspacios.Bootstrap` crea un `Administrador del sistema`,
un `Coordinador de espacios` y un `Solicitante` leyendo los valores de entorno:

```powershell
$env:ConnectionStrings__ReservaEspacios = "Host=127.0.0.1;Port=5432;Database=reservaespacios;Username=reserva_app;Password=<password-local>"
$env:BOOTSTRAP_ADMIN_EMAIL = "admin@example.com"
$env:BOOTSTRAP_ADMIN_PASSWORD = "<password-segura>"
$env:BOOTSTRAP_COORDINADOR_EMAIL = "coordinador@example.com"
$env:BOOTSTRAP_COORDINADOR_PASSWORD = "<password-segura>"
$env:BOOTSTRAP_SOLICITANTE_EMAIL = "solicitante@example.com"
$env:BOOTSTRAP_SOLICITANTE_PASSWORD = "<password-segura>"
dotnet run --project tools/ReservaEspacios.Bootstrap
```

La contraseña debe tener al menos 8 caracteres, una mayúscula y un número. Si una
cuenta ya existe, se conserva y se informa por consola.

> Nota: el primer administrador se crea directamente sobre el puerto de Identity porque
> `CrearUsuarioPrivilegiadoCommand` exige que ya exista un administrador. Una vez creado,
> las altas privilegiadas posteriores pasan por el guard (`POST /api/usuarios`).

## Rutas de la interfaz

| Ruta | Uso |
|---|---|
| `/` | Portada con accesos según el rol |
| `/acceso/login` | Inicio de sesión |
| `/acceso/registro` | Autorregistro de `Solicitante` |
| `/disponibilidad` | Consulta de franjas libres |
| `/reservas/nueva` | Solicitar reserva (`Solicitante`) |
| `/reservas/cancelar` | Cancelar reserva |
| `/reservas/historial` | Historial |
| `/reservas/aprobaciones` | Aprobación y rechazo (`Coordinador`/`Administrador`) |
| `/administracion/espacios` | Alta y edición de espacios (`Administrador`) |

El menú oculta automáticamente todo enlace no otorgado por la matriz canónica de
permisos. Tras iniciar o cerrar sesión se fuerza una recarga completa para que los
claims se reflejen en la interfaz.

## Pruebas

```powershell
$env:ConnectionStrings__ReservaEspaciosTest = "Host=127.0.0.1;Port=5432;Database=reservaespacios_test;Username=reserva_test;Password=<password-local>"
dotnet test
```

Cada clase de integración usa un esquema PostgreSQL propio. No se requiere Docker.

## Decisiones

Las decisiones de arquitectura están en `docs/adr/`. El plan original, no modificado,
está en `PLAN_INGENIERIA.md`; los cambios posteriores se documentan en los `design.md`
de `openspec/changes/`.
