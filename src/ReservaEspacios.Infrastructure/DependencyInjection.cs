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

        return services;
    }
}
