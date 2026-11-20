namespace ReservaEspacios.Web.Security;

/// <summary>
/// Adapter (ADR-002) para las operaciones de sesión que la UI necesita y que no son
/// un caso de uso de Application. El alta de la cookie ya la realiza Identity al
/// autenticar; aquí solo se cierra.
/// </summary>
public interface ISesionWeb
{
    Task CerrarSesionAsync(CancellationToken cancellationToken = default);
}
