using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Infrastructure.Disponibilidad;
using ReservaEspacios.Infrastructure.Espacios;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ReservaEspaciosDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDisponibilidadRepository, StubDisponibilidadRepository>();

        // El decorator se registra después que el caso de uso en AddApplication(),
        // por lo que es la implementación efectiva de IConsultarDisponibilidad.
        services.AddScoped<ConsultarDisponibilidad>();
        services.AddScoped<IDisponibilidadCache, DisponibilidadCache>();
        services.AddScoped<IConsultarDisponibilidad>(provider => new DisponibilidadCacheDecorator(
            provider.GetRequiredService<ConsultarDisponibilidad>(),
            provider.GetRequiredService<IDisponibilidadCache>()));

        return services;
    }
}
