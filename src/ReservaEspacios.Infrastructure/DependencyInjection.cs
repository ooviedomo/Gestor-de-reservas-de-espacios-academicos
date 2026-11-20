using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
