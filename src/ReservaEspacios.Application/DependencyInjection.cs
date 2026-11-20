using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;

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
        return services;
    }
}
