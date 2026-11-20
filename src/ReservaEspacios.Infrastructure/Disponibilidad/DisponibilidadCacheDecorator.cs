using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;

namespace ReservaEspacios.Infrastructure.Disponibilidad;

/// <summary>
/// Patrón Decorator (ADR-002): cachea por (espacio, fecha, franja) sin alterar
/// las reglas de disponibilidad del caso de uso envuelto.
/// </summary>
public sealed class DisponibilidadCacheDecorator(
    IConsultarDisponibilidad inner,
    IDisponibilidadCache cache) : IConsultarDisponibilidad
{
    public async Task<Result<DisponibilidadDto>> HandleAsync(
        ConsultarDisponibilidadCommand command,
        CancellationToken cancellationToken = default)
    {
        var key = new DisponibilidadCacheKey(
            command.SpaceId,
            command.Inicio.ToUniversalTime(),
            command.Fin.ToUniversalTime());

        var cached = cache.Get(key);
        if (cached is not null)
        {
            return Result<DisponibilidadDto>.Success(cached);
        }

        var resultado = await inner.HandleAsync(command, cancellationToken);
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            cache.Set(key, resultado.Value);
        }

        return resultado;
    }
}
