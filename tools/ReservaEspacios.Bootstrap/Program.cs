using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure;
using ReservaEspacios.Infrastructure.Identity;
using ReservaEspacios.Infrastructure.Persistence;

// Bootstrap operativo (fuera de la aplicación): crea las tres cuentas de prueba.
// Las credenciales se leen SOLO de variables de entorno (ADR-003); nunca se versionan.
// El primer administrador no puede pasar por CrearUsuarioPrivilegiadoCommand porque ese
// guard exige que ya exista un administrador: por eso se usa directamente el puerto
// IUsuarioIdentityService, igual que una migración usa el esquema sin permisos de app.

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ReservaEspacios")
    ?? throw new InvalidOperationException(
        "Defina ConnectionStrings__ReservaEspacios antes de ejecutar el bootstrap (ADR-003).");

var cuentas = new[]
{
    new CuentaBootstrap("ADMIN", "Administrador del sistema", Rol.Administrador),
    new CuentaBootstrap("COORDINADOR", "Coordinador de espacios", Rol.Coordinador),
    new CuentaBootstrap("SOLICITANTE", "Ana Solicitante", Rol.Solicitante)
};

var servicios = new ServiceCollection();
servicios.AddLogging();
servicios.AddApplication();
servicios.AddInfrastructure(connectionString);
servicios
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<ReservaEspaciosDbContext>()
    .AddRoles<ApplicationRole>()
    .AddDefaultTokenProviders();

await using var proveedor = servicios.BuildServiceProvider(true);
using var ambito = proveedor.CreateScope();
var identity = ambito.ServiceProvider.GetRequiredService<IUsuarioIdentityService>();
var userManager = ambito.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var registrar = ambito.ServiceProvider
    .GetRequiredService<ICommandHandler<RegistrarSolicitanteCommand, Guid>>();

var fallos = 0;
foreach (var cuenta in cuentas)
{
    var email = Requerir($"BOOTSTRAP_{cuenta.Prefijo}_EMAIL");
    var contrasena = Requerir($"BOOTSTRAP_{cuenta.Prefijo}_PASSWORD");

    var resultado = cuenta.Rol == Rol.Solicitante
        ? await registrar.HandleAsync(
            new RegistrarSolicitanteCommand(cuenta.Nombre, email, contrasena))
        : await identity.CrearAsync(
            new CrearUsuarioCommand(cuenta.Nombre, email, contrasena, cuenta.Rol));

    if (resultado.IsSuccess)
    {
        Console.WriteLine($"Creada cuenta {cuenta.Rol.Etiqueta()}: {email}");
    }
    else if (resultado.ErrorCode == DomainErrors.EmailDuplicado)
    {
        var usuario = await userManager.FindByEmailAsync(email);
        if (usuario is null)
        {
            Console.Error.WriteLine($"No se encontró la cuenta {email} para rotar la contraseña.");
            fallos++;
            continue;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var renovacion = await userManager.ResetPasswordAsync(usuario, token, contrasena);
        if (renovacion.Succeeded)
        {
            Console.WriteLine($"Contraseña renovada para {email}.");
        }
        else
        {
            Console.Error.WriteLine(
                $"No se pudo renovar la contraseña de {email}: " +
                string.Join(", ", renovacion.Errors.Select(error => error.Code)));
            fallos++;
        }
    }
    else
    {
        Console.Error.WriteLine($"No se pudo crear {email}: {resultado.ErrorCode}");
        fallos++;
    }
}

return fallos == 0 ? 0 : 1;

static string Requerir(string variable) =>
    Environment.GetEnvironmentVariable(variable)
    ?? throw new InvalidOperationException($"Defina {variable} antes de ejecutar el bootstrap.");

internal sealed record CuentaBootstrap(string Prefijo, string Nombre, Rol Rol);
