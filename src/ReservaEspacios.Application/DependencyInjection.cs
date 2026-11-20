using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Facade;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Reservas.Validacion;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Strategy de permisos y Decorator de autorización sobre la administración
        // de espacios (AUTH-001/003). Los casos de uso concretos siguen sin conocer
        // quién los invoca, por lo que sus pruebas unitarias no cambian.
        services.AddScoped<IEvaluadorPermisos, EvaluadorPermisos>();
        // Observer (ADR-002): el despachador se registra aunque este change aún no tenga
        // suscriptores concretos; auditoría y outbox se añadirán en el tercer change.
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
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
        services.AddScoped<IHorarioEspacio, HorarioEspacio>();
        services.AddScoped<ISolapeReservas, SolapeReservas>();
        services.AddScoped<IReservaEspaciosFacade, ReservaEspaciosFacade>();

        // Chain of Responsibility y Strategy de RES-001 (tarea 3.2).
        services.AddScoped<IValidacionPorTipoEspacio, ValidacionPorTipoEspacio>();
        services.AddScoped<ValidadorRango>();
        services.AddScoped<ValidadorEspacioActivo>();
        services.AddScoped<ValidadorHorario>();
        services.AddScoped<ValidadorCapacidad>();
        services.AddScoped<ValidadorSolape>();
        services.AddScoped<CadenaValidacionCrearReserva>();

        // Command pattern: un manejador por caso de uso de reservas y usuarios.
        services.AddScoped<CrearReserva>();
        services.AddScoped<CancelarReserva>();
        services.AddScoped<AprobarReserva>();
        services.AddScoped<RechazarReserva>();
        services.AddScoped<CompletarReserva>();
        services.AddScoped<CerrarReservasVencidas>();
        services.AddScoped<ConsultarHistorial>();
        // La UI depende de la abstracción del patrón Command (ADR-002), no del handler
        // concreto; así los componentes se prueban con un mock del comando (tarea 3.5).
        services.AddScoped<ICommandHandler<CrearReservaCommand, Guid>, CrearReserva>();
        services.AddScoped<ICommandHandler<CancelarReservaCommand>, CancelarReserva>();
        services.AddScoped<ICommandHandler<AprobarReservaCommand>, AprobarReserva>();
        services.AddScoped<ICommandHandler<RechazarReservaCommand>, RechazarReserva>();
        services.AddScoped<ICommandHandler<CompletarReservaCommand>, CompletarReserva>();
        services.AddScoped<
            ICommandHandler<ConsultarHistorialCommand, IReadOnlyList<HistorialReservaDto>>,
            ConsultarHistorial>();
        services.AddScoped<ICerrarReservasVencidas>(provider =>
            provider.GetRequiredService<CerrarReservasVencidas>());
        services.AddScoped<RegistrarSolicitante>();
        services.AddScoped<CrearUsuarioPrivilegiado>();
        services.AddScoped<CambiarRolUsuario>();
        services.AddScoped<ActivarDesactivarUsuario>();
        services.AddScoped<AutenticarUsuario>();
        // La UI inyecta la abstracción, como ya hace con los comandos de reservas.
        services.AddScoped<
            ICommandHandler<AutenticarUsuarioCommand, UsuarioSesionDto>,
            AutenticarUsuario>();
        services.AddScoped<
            ICommandHandler<RegistrarSolicitanteCommand, Guid>,
            RegistrarSolicitante>();
        return services;
    }
}
