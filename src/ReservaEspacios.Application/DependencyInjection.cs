using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Facade;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;

namespace ReservaEspacios.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Strategy de permisos y Decorator de autorización sobre la administración
        // de espacios (AUTH-001/003). Los casos de uso concretos siguen sin conocer
        // quién los invoca, por lo que sus pruebas unitarias no cambian.
        services.AddScoped<IEvaluadorPermisos, EvaluadorPermisos>();
        services.AddScoped<GuardiaOperacionesProtegidas>();
        services.AddScoped<RegistrarSpace>();
        services.AddScoped<EditarSpace>();
        services.AddScoped<DesactivarSpace>();
        services.AddScoped<ReactivarSpace>();
        services.AddScoped<GuardiasAdministracionEspacios>();
        services.AddScoped<IRegistrarSpace>(sp => sp.GetRequiredService<GuardiasAdministracionEspacios>());
        services.AddScoped<IEditarSpace>(sp => sp.GetRequiredService<GuardiasAdministracionEspacios>());
        services.AddScoped<IDesactivarSpace>(sp => sp.GetRequiredService<GuardiasAdministracionEspacios>());
        services.AddScoped<IReactivarSpace>(sp => sp.GetRequiredService<GuardiasAdministracionEspacios>());
        services.AddScoped<IConsultarCatalogo, ConsultarCatalogo>();
        services.AddScoped<IConsultarDisponibilidad, ConsultarDisponibilidad>();
        services.AddScoped<IReservaEspaciosFacade, ReservaEspaciosFacade>();

        // Command pattern: un manejador por caso de uso de reservas y usuarios.
        services.AddScoped<CrearReserva>();
        services.AddScoped<RegistrarSolicitante>();
        services.AddScoped<CrearUsuarioPrivilegiado>();
        services.AddScoped<CambiarRolUsuario>();
        services.AddScoped<ActivarDesactivarUsuario>();
        services.AddScoped<AutenticarUsuario>();
        return services;
    }
}
