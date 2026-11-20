using FluentAssertions;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Infrastructure.Persistence;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>Última línea de defensa ante una carrera de altas con el mismo nombre (ESP-001).</summary>
public sealed class UnitOfWorkTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_ESP_001_UnitOfWork_traduce_violacion_de_unicidad_a_NOMBRE_DUPLICADO()
    {
        await using var context = fixture.CreateContext();
        var nombre = $"Laboratorio A-101-{Guid.NewGuid():N}";
        context.Spaces.Add(SpaceDePrueba(nombre));
        await context.SaveChangesAsync();

        context.Spaces.Add(SpaceDePrueba(nombre));
        var sut = new UnitOfWork(context);

        var act = async () => await sut.SaveChangesAsync();

        (await act.Should().ThrowAsync<DomainException>())
            .Which.Code.Should().Be(DomainErrors.NombreDuplicado);
    }

    private static Space SpaceDePrueba(string nombre) => new(
        Guid.NewGuid(),
        nombre,
        EspacioTipo.Laboratorio,
        30,
        "Bloque A",
        new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
}
