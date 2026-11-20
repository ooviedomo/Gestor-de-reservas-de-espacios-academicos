using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Notificaciones;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Infrastructure.Auditoria;
using ReservaEspacios.Infrastructure.Disponibilidad;
using ReservaEspacios.Infrastructure.Espacios;
using ReservaEspacios.Infrastructure.Identity;
using ReservaEspacios.Infrastructure.Notificaciones;
using ReservaEspacios.Infrastructure.Persistence;
using ReservaEspacios.Infrastructure.Reportes;
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
        // Orden de decorators: traducir el error SQL y, solo si la escritura terminó
        // bien, invalidar la caché de disponibilidad del espacio (tarea 3.1).
        services.AddScoped<IReservaRepository>(provider => new InvalidarCacheReservaDecorator(
            new TraductorErroresReservaDecorator(
                provider.GetRequiredService<ReservaRepository>()),
            provider.GetRequiredService<IDisponibilidadCache>()));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransaccionReserva, TransaccionReserva>();
        services.AddScoped<ITransaccionEventos, TransaccionEventos>();
        services.AddScoped<IDisponibilidadRepository, DisponibilidadRepository>();
        services.AddScoped<IUsuarioIdentityService, IdentityUsuarioService>();

        // AUD-001/002: append-only y consulta con actor resuelto.
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

        // NOT-001/002/003: Adapter SMTP, repositorio outbox y Decorator de logging.
        services.Configure<EmailOptions>(options => { });
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificacionRepository, NotificacionRepository>();
        services.AddScoped<ICommandHandler<EnviarCorreoCommand>>(provider =>
            new LoggingCommandHandler<EnviarCorreoCommand>(
                provider.GetRequiredService<EnviarCorreoHandler>(),
                provider.GetRequiredService<ILogger<LoggingCommandHandler<EnviarCorreoCommand>>>()));

        // REP-001: GROUP BY sobre reservas.
        services.AddScoped<IRepositorioOcupacion, RepositorioOcupacion>();

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
