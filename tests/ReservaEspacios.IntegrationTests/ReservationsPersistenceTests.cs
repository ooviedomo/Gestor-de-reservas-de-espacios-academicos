using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReservaEspacios.Domain.Reservas;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// Tarea 2.1: la tabla `reservations` y su restricción de exclusión son la última
/// barrera contra solapes (plan §3.3), verificada contra PostgreSQL local (ADR-003).
/// </summary>
public sealed class ReservationsPersistenceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Inicio = new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin = new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_RES_002_Insercion_solapada_falla_con_exclusion_violation()
    {
        var espacio = Guid.NewGuid();
        await InsertarAsync(Guid.NewGuid(), espacio, EstadoReserva.Pendiente, Inicio, Fin);

        var accion = () => InsertarAsync(
            Guid.NewGuid(),
            espacio,
            EstadoReserva.Pendiente,
            Inicio.AddMinutes(30),
            Fin.AddMinutes(30));

        var excepcion = await accion.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be("23P01");
    }

    [Fact]
    public async Task TC_RES_002_Reservas_contiguas_se_aceptan()
    {
        var espacio = Guid.NewGuid();
        await InsertarAsync(Guid.NewGuid(), espacio, EstadoReserva.Aprobada, Inicio, Fin);

        await InsertarAsync(Guid.NewGuid(), espacio, EstadoReserva.Pendiente, Fin, Fin.AddHours(1));
    }

    [Theory]
    [InlineData(EstadoReserva.Cancelada)]
    [InlineData(EstadoReserva.Rechazada)]
    [InlineData(EstadoReserva.Completada)]
    public async Task TC_RES_002_Estados_no_bloqueantes_permiten_nueva_reserva(EstadoReserva estado)
    {
        var espacio = Guid.NewGuid();
        await InsertarAsync(Guid.NewGuid(), espacio, estado, Inicio, Fin);

        await InsertarAsync(Guid.NewGuid(), espacio, EstadoReserva.Pendiente, Inicio, Fin);
    }

    [Fact]
    public async Task TC_RES_002_Aprobada_tambien_bloquea_la_misma_franja()
    {
        var espacio = Guid.NewGuid();
        await InsertarAsync(Guid.NewGuid(), espacio, EstadoReserva.Aprobada, Inicio, Fin);

        var accion = () => InsertarAsync(
            Guid.NewGuid(), espacio, EstadoReserva.Pendiente, Inicio, Fin);

        (await accion.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23P01");
    }

    [Fact]
    public async Task TC_RES_002_Migracion_crea_extension_indice_GiST_y_restriccion()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        (await EscalarAsync(connection,
            "SELECT COUNT(*) FROM pg_extension WHERE extname = 'btree_gist'")).Should().Be(1);

        (await EscalarAsync(connection, """
            SELECT COUNT(*)
            FROM pg_constraint c
            JOIN pg_index i ON i.indexrelid = c.conindid
            JOIN pg_class clas ON clas.oid = i.indexrelid
            JOIN pg_am am ON am.oid = clas.relam
            WHERE c.conname = 'no_overlap'
              AND c.contype = 'x'
              AND am.amname = 'gist'
              AND c.connamespace = (SELECT oid FROM pg_namespace WHERE nspname = current_schema())
            """)).Should().Be(1);

        var columnas = await EscalarAsync(connection, """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = current_schema()
              AND table_name = 'reservations'
              AND column_name IN ('asistentes', 'motivo_rechazo', 'completado_en', 'actor_id')
            """);
        columnas.Should().Be(4);
    }

    [Fact]
    public async Task TC_RES_001_Entidad_se_persiste_con_todos_sus_campos()
    {
        var id = Guid.NewGuid();
        var espacio = Guid.NewGuid();
        var solicitante = Guid.NewGuid();
        var reserva = new Reservation(
            id, espacio, solicitante, Inicio, Fin, "Clase de laboratorio", 20);
        await using (var contexto = fixture.CreateContext())
        {
            contexto.Reservations.Add(reserva);
            await contexto.SaveChangesAsync();
        }

        await using var lectura = fixture.CreateContext();
        var guardada = await lectura.Reservations.AsNoTracking().SingleAsync(x => x.Id == id);

        guardada.Estado.Should().Be(EstadoReserva.Pendiente);
        guardada.EsBloqueante.Should().BeTrue();
        guardada.SpaceId.Should().Be(espacio);
        guardada.UsuarioId.Should().Be(solicitante);
        guardada.Inicio.Should().Be(Inicio);
        guardada.Fin.Should().Be(Fin);
        guardada.Proposito.Should().Be("Clase de laboratorio");
        guardada.Asistentes.Should().Be(20);
        guardada.CreadoEn.Should().BeAfter(DateTimeOffset.MinValue);
    }

    private async Task InsertarAsync(
        Guid id,
        Guid espacio,
        EstadoReserva estado,
        DateTimeOffset inicio,
        DateTimeOffset fin)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO reservations
                (id, space_id, user_id, inicio, fin, proposito, asistentes, estado, creado_en)
            VALUES
                (@id, @espacio, @usuario, @inicio, @fin, 'Clase', 10, @estado, now());
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("espacio", espacio);
        command.Parameters.AddWithValue("usuario", Guid.NewGuid());
        command.Parameters.AddWithValue("inicio", inicio);
        command.Parameters.AddWithValue("fin", fin);
        command.Parameters.AddWithValue("estado", estado.Codigo());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> EscalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
