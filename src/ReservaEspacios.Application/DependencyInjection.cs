using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Facade;
using ReservaEspacios.Application.Usuarios;

namespace ReservaEspacios.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRegistrarSpace, RegistrarSpace>();
        services.AddScoped<IEditarSpace, EditarSpace>();
        services.AddScoped<IDesactivarSpace, DesactivarSpace>();
        services.AddScoped<IReactivarSpace, ReactivarSpace>();
        services.AddScoped<IConsultarCatalogo, ConsultarCatalogo>();
        services.AddScoped<IConsultarDisponibilidad, ConsultarDisponibilidad>();
        services.AddScoped<IReservaEspaciosFacade, ReservaEspaciosFacade>();

        // Command pattern: un manejador por caso de uso de usuarios.
        services.AddScoped<RegistrarSolicitante>();
        services.AddScoped<CrearUsuarioPrivilegiado>();
        services.AddScoped<CambiarRolUsuario>();
        services.AddScoped<ActivarDesactivarUsuario>();
        services.AddScoped<AutenticarUsuario>();
        return services;
    }
}
