using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Infrastructure.Disponibilidad;
using ReservaEspacios.Infrastructure.Espacios;
using ReservaEspacios.Infrastructure.Identity;
using ReservaEspacios.Infrastructure.Persistence;
using ReservaEspacios.Infrastructure.Reservas;
using ReservaEspacios.Application.Usuarios;

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
        services.AddScoped<ReservaRepository>();
        services.AddScoped<IReservaRepository>(provider => new TraductorErroresReservaDecorator(
            provider.GetRequiredService<ReservaRepository>()));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDisponibilidadRepository, StubDisponibilidadRepository>();
        services.AddScoped<IUsuarioIdentityService, IdentityUsuarioService>();

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
