using Microsoft.AspNetCore.Identity;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Infrastructure.Identity;

/// <summary>
/// Adaptador del puerto <see cref="IUsuarioIdentityService"/> sobre ASP.NET Core Identity.
/// Traduce los errores de Identity a códigos del dominio para que el cliente nunca vea
/// un mensaje crudo del proveedor (USR-001/002/003).
/// </summary>
public sealed class IdentityUsuarioService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<ApplicationRole> roleManager) : IUsuarioIdentityService
{
    private static readonly string[] CodigosContrasenaDebil =
    [
        "PasswordTooShort", "PasswordRequiresDigit", "PasswordRequiresUpper",
        "PasswordRequiresLower", "PasswordRequiresNonAlphanumeric", "PasswordRequiresUniqueChars"
    ];

    public async Task<Result<Guid>> CrearAsync(
        CrearUsuarioCommand command,
        CancellationToken cancellationToken = default)
    {
        var existente = await userManager.FindByEmailAsync(command.Email);
        if (existente is not null)
        {
            return Result<Guid>.Failure(DomainErrors.EmailDuplicado);
        }

        // USR-001/003: el email es el nombre de usuario (RequireUniqueEmail = true).
        var usuario = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = true,
            Nombre = command.Nombre,
            Activo = true
        };

        var creacion = await userManager.CreateAsync(usuario, command.Contrasena);
        if (!creacion.Succeeded)
        {
            return Result<Guid>.Failure(TraducirErrorCreacion(creacion));
        }

        var rol = await roleManager.FindByNameAsync(command.Rol.Codigo());
        if (rol is null)
        {
            return Result<Guid>.Failure(DomainErrors.UsuarioNoEncontrado);
        }

        var asignacion = await userManager.AddToRoleAsync(usuario, rol.Name!);
        return asignacion.Succeeded
            ? Result<Guid>.Success(usuario.Id)
            : Result<Guid>.Failure(asignacion.Errors.First().Code);
    }

    public async Task<Result<UsuarioSesionDto>> AutenticarAsync(
        string email,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByEmailAsync(email);
        if (usuario is null)
        {
            // El mismo código para email y contraseña: no se revela cuál falló (USR-003).
            return Result<UsuarioSesionDto>.Failure(DomainErrors.CredencialesInvalidas);
        }

        if (!usuario.Activo)
        {
            return Result<UsuarioSesionDto>.Failure(DomainErrors.CuentaDesactivada);
        }

        var inicio = await signInManager.PasswordSignInAsync(
            usuario, contrasena, isPersistent: false, lockoutOnFailure: false);

        if (!inicio.Succeeded)
        {
            return Result<UsuarioSesionDto>.Failure(DomainErrors.CredencialesInvalidas);
        }

        return Result<UsuarioSesionDto>.Success(await SesionAsync(usuario));
    }

    public async Task<Result> CambiarRolAsync(
        Guid usuarioId,
        Rol rol,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Result.Failure(DomainErrors.UsuarioNoEncontrado);
        }

        var rolesActuales = await userManager.GetRolesAsync(usuario);
        if (rolesActuales.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(usuario, rolesActuales);
        }

        var asignacion = await userManager.AddToRoleAsync(usuario, rol.Codigo());
        return asignacion.Succeeded
            ? Result.Success()
            : Result.Failure(asignacion.Errors.First().Code);
    }

    public async Task<Result> ActivarDesactivarAsync(
        Guid usuarioId,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Result.Failure(DomainErrors.UsuarioNoEncontrado);
        }

        usuario.Activo = activo;
        var actualizacion = await userManager.UpdateAsync(usuario);
        return actualizacion.Succeeded
            ? Result.Success()
            : Result.Failure(actualizacion.Errors.First().Code);
    }

    public async Task<Result<UsuarioSesionDto?>> BuscarPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByEmailAsync(email);
        return Result<UsuarioSesionDto?>.Success(
            usuario is null ? null : await SesionAsync(usuario));
    }

    public async Task<Result<DatosActorDto?>> ObtenerPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return Result<DatosActorDto?>.Success(null);
        }

        var roles = await userManager.GetRolesAsync(usuario);
        var rol = roles.Select(RolExtensions.DesdeCodigo).FirstOrDefault(x => x is not null)
            ?? Rol.Solicitante;

        return Result<DatosActorDto?>.Success(
            new DatosActorDto(usuario.Id, usuario.Nombre, rol, usuario.Email ?? string.Empty));
    }

    private async Task<UsuarioSesionDto> SesionAsync(ApplicationUser usuario)
    {
        var roles = await userManager.GetRolesAsync(usuario);
        var rol = roles.Select(RolExtensions.DesdeCodigo).FirstOrDefault(x => x is not null)
            ?? Rol.Solicitante;

        return new UsuarioSesionDto(
            usuario.Id, usuario.Nombre, usuario.Email ?? string.Empty, rol, usuario.Activo);
    }

    private static string TraducirErrorCreacion(IdentityResult resultado)
    {
        var errores = resultado.Errors.Select(x => x.Code).ToArray();
        if (errores.Any(CodigosContrasenaDebil.Contains))
        {
            return DomainErrors.ContrasenaDebil;
        }

        if (errores.Contains("DuplicateUserName") || errores.Contains("DuplicateEmail"))
        {
            return DomainErrors.EmailDuplicado;
        }

        return errores.FirstOrDefault() ?? DomainErrors.CredencialesInvalidas;
    }
}
