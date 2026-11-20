using FluentAssertions;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Infrastructure.Reservas;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// Tarea 2.3: el Decorator traduce la violación real de `no_overlap` antes de
/// devolverla a Application, usando PostgreSQL local (ADR-003).
/// </summary>
public sealed class ReservaRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Inicio = new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin = new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_RES_002_Repositorio_decorado_devuelve_SOLAPE_DETECTADO()
    {
        await using var contexto = fixture.CreateContext();
        IReservaRepository repositorio =
            new TraductorErroresReservaDecorator(new ReservaRepository(contexto));
        var espacio = Guid.NewGuid();

        await repositorio.AddAsync(Nueva(espacio, Inicio, Fin));

        var act = () => repositorio.AddAsync(
            Nueva(espacio, Inicio.AddMinutes(30), Fin.AddMinutes(30)));

        var excepcion = await act.Should().ThrowAsync<DomainException>();
        excepcion.Which.Code.Should().Be(DomainErrors.SolapeDetectado);
        excepcion.Which.Message.Should().NotContain("23P01");
        excepcion.Which.Message.Should().NotContain("no_overlap");
    }

    private static Reservation Nueva(Guid espacio, DateTimeOffset inicio, DateTimeOffset fin) =>
        new(Guid.NewGuid(), espacio, Guid.NewGuid(), inicio, fin, "Clase de laboratorio", 20);
}
