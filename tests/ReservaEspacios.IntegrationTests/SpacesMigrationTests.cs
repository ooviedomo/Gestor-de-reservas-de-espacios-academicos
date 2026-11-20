using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReservaEspacios.Domain.Espacios;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>ESP-001: integridad de spaces garantizada también en la base de datos.</summary>
public sealed class SpacesMigrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_ESP_001_Nombre_duplicado_se_rechaza_en_base_de_datos()
    {
        await using var context = fixture.CreateContext();
        context.Spaces.Add(SpaceDePrueba("Laboratorio A-101"));
        await context.SaveChangesAsync();

        context.Spaces.Add(SpaceDePrueba("Laboratorio A-101"));

        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task TC_ESP_001_Horario_invalido_se_rechaza_por_check_constraint()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO spaces (id, nombre, tipo, capacidad, ubicacion, hora_apertura, hora_cierre, activo)
            VALUES (gen_random_uuid(), 'Horario inválido', 'LABORATORIO', 10, 'Bloque 1', '18:00', '08:00', true)
            """;

        var act = async () => await command.ExecuteNonQueryAsync();
        await act.Should().ThrowAsync<NpgsqlException>();
    }

    private static Space SpaceDePrueba(string nombre) => new(
        Guid.NewGuid(),
        nombre,
        EspacioTipo.Laboratorio,
        30,
        "Bloque A",
        new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
}
