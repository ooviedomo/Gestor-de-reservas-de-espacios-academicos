# ADR-003: Entorno de compilación y base de datos de pruebas

- **Fecha:** 2026-11-19
- **Estado:** Aceptado
- **Contexto:** Inicio de la Fase 3 (implementación asistida). Sustituye, solo para el entorno local del estudiante, la referencia a Testcontainers.NET de `PLAN_INGENIERIA.md` §5.4 y de los `tasks.md`.

## Contexto

Al verificar el entorno se encontró:

- SDK de .NET **11.0.100** como único SDK, pero con runtimes y *reference packs* de **10.0.15** instalados, por lo que es posible targetear `net10.0` sin instalar otro SDK.
- No hay Docker ni Podman, y el usuario no tiene privilegios de administrador de Windows para instalar Docker Desktop.
- Sí hay una instancia de **PostgreSQL 19** funcionando en `127.0.0.1:5432`, con autenticación `scram-sha-256`.

Los `tasks.md` y el plan §5.4 daban por supuesto Testcontainers.NET, que necesita un runtime de contenedores. Mantener esa dependencia impediría ejecutar las pruebas de integración y de concurrencia.

## Decisión

1. Los proyectos siguen targeteando **`net10.0`**, conforme al plan §3.2. Se añade un `global.json` que expresa la preferencia por la banda 10 y permite usar el SDK 11 instalado mediante `rollForward: latestMajor`.
2. Se usan dos bases de datos en la instancia local de PostgreSQL 19:
   - `reservaespacios` con el rol `reserva_app` para desarrollo.
   - `reservaespacios_test` con el rol `reserva_test` para pruebas de integración y concurrencia.
3. Las cadenas de conexión se leen de **variables de entorno / user-secrets** (`ConnectionStrings__ReservaEspacios` y `ConnectionStrings__ReservaEspaciosTest`). Las credenciales no se versionan en el repositorio.
4. Las pruebas de integración usan Npgsql contra `reservaespacios_test` con una fixture xUnit que crea un esquema único por clase de prueba y lo elimina al terminar. Esto conserva el aislamiento que se buscaba con Testcontainers, sin contenedores.
5. Se mantiene la dependencia de `Testcontainers.NET` **fuera** del proyecto: las tareas que la mencionaban pasan a referirse a "PostgreSQL local (`reservaespacios_test`)".

## Consecuencias

- Es obligatorio tener el servicio `postgresql-x64-19` en ejecución para correr `dotnet test` de integración.
- Las pruebas ya no son portátiles a una máquina sin PostgreSQL; el README del proyecto documentará la creación de roles y bases.
- No se añade ninguna dependencia nueva a la stack del plan; se retira Testcontainers.NET del listado de paquetes.
- Si más adelante se instala Docker, esta decisión puede revisarse con un ADR nuevo sin cambiar el código de dominio.
