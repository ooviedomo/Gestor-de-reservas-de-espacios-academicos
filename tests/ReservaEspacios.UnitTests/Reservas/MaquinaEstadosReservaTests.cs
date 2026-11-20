using FluentAssertions;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using Xunit;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// APR-001, COM-001 y RES-003: verifica la máquina de estados (State) y su esqueleto
/// de transición (Template Method) a través de la API pública de <see cref="Reservation"/>.
/// </summary>
public sealed class MaquinaEstadosReservaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SolicitanteId = Guid.NewGuid();
    private static readonly Guid CoordinadorId = Guid.NewGuid();
    private static readonly ActorTransicion Coordinador =
        ActorTransicion.Usuario(CoordinadorId, Rol.Coordinador);
    private static readonly ActorTransicion Administrador =
        ActorTransicion.Usuario(Guid.NewGuid(), Rol.Administrador);
    private static readonly ActorTransicion Solicitante =
        ActorTransicion.Usuario(SolicitanteId, Rol.Solicitante);

    [Fact]
    public void TC_APR_001_Aprobar_pendiente_registra_actor_y_fecha()
    {
        var reserva = Nueva();

        reserva.Aprobar(Coordinador, Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Aprobada);
        reserva.AprobadoPor.Should().Be(CoordinadorId);
        reserva.AprobadoEn.Should().Be(Ahora);
        reserva.ActorId.Should().Be(CoordinadorId);
        reserva.EsBloqueante.Should().BeTrue();
    }

    [Fact]
    public void TC_APR_001_Rechazar_con_motivo_registra_motivo_y_actor()
    {
        var reserva = Nueva();

        reserva.Rechazar(Administrador, "Espacio en mantención", Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Rechazada);
        reserva.MotivoRechazo.Should().Be("Espacio en mantención");
        reserva.ActorId.Should().Be(Administrador.UsuarioId);
        reserva.AprobadoEn.Should().BeNull();
        reserva.EsBloqueante.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TC_APR_001_Rechazar_sin_motivo_devuelve_MOTIVO_OBLIGATORIO(string motivo)
    {
        var reserva = Nueva();

        var act = () => reserva.Rechazar(Coordinador, motivo, Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.MotivoObligatorio);
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public void TC_APR_001_Reaprobar_devuelve_TRANSICION_INVALIDA()
    {
        var reserva = EnEstado(EstadoReserva.Aprobada);

        var act = () => reserva.Aprobar(Coordinador, Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.TransicionInvalida);
        reserva.Estado.Should().Be(EstadoReserva.Aprobada);
    }

    [Fact]
    public void TC_APR_001_Solicitante_no_aprueba()
    {
        var reserva = Nueva();

        var act = () => reserva.Aprobar(Solicitante, Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.NoAutorizado);
    }

    [Fact]
    public void TC_COM_001_Completar_manual_registra_fecha()
    {
        var reserva = EnEstado(EstadoReserva.Aprobada);

        reserva.Completar(Coordinador, Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Completada);
        reserva.CompletadoEn.Should().Be(Ahora);
        reserva.EsBloqueante.Should().BeFalse();
    }

    [Fact]
    public void TC_COM_001_Automatico_registra_fecha_sin_actor_cuando_fin_vencio()
    {
        var fin = Ahora.AddMinutes(-1);
        var reserva = Nueva(fin);
        reserva.Aprobar(Coordinador, Ahora.AddHours(-2));

        reserva.CompletarAutomaticamente(Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Completada);
        reserva.CompletadoEn.Should().Be(Ahora);
        reserva.ActorId.Should().Be(CoordinadorId);
        reserva.AprobadoPor.Should().Be(CoordinadorId);
    }

    [Fact]
    public void TC_COM_001_Automatico_antes_de_vencer_devuelve_TRANSICION_INVALIDA()
    {
        var reserva = EnEstado(EstadoReserva.Aprobada);

        var act = () => reserva.CompletarAutomaticamente(Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.TransicionInvalida);
        reserva.Estado.Should().Be(EstadoReserva.Aprobada);
    }

    [Fact]
    public void TC_COM_001_Completar_pendiente_devuelve_TRANSICION_INVALIDA()
    {
        var reserva = Nueva();

        var act = () => reserva.Completar(Coordinador, Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.TransicionInvalida);
    }

    [Fact]
    public void TC_COM_001_Solicitante_no_completa()
    {
        var reserva = EnEstado(EstadoReserva.Aprobada);

        var act = () => reserva.Completar(Solicitante, Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.NoAutorizado);
    }

    [Fact]
    public void TC_RES_003_Solicitante_cancela_la_suya_con_motivo()
    {
        var reserva = EnEstado(EstadoReserva.Aprobada);

        reserva.Cancelar(Solicitante, "Docencia reprogramada", Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.MotivoCancelacion.Should().Be("Docencia reprogramada");
        reserva.ActorId.Should().Be(SolicitanteId);
        reserva.CanceladoEn.Should().Be(Ahora);
        reserva.EsBloqueante.Should().BeFalse();
    }

    [Fact]
    public void TC_RES_003_Solicitante_no_cancela_la_de_otro()
    {
        var reserva = Nueva(usuarioId: Guid.NewGuid());

        var act = () => reserva.Cancelar(Solicitante, "Docencia reprogramada", Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.NoAutorizado);
    }

    [Fact]
    public void TC_RES_003_Coordinador_cancela_la_de_otro()
    {
        var reserva = Nueva(usuarioId: Guid.NewGuid());

        reserva.Cancelar(Coordinador, "Espacio fuera de servicio", Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.ActorId.Should().Be(CoordinadorId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TC_RES_003_Cancelar_sin_motivo_devuelve_MOTIVO_OBLIGATORIO(string motivo)
    {
        var reserva = Nueva();

        var act = () => reserva.Cancelar(Solicitante, motivo, Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.MotivoObligatorio);
    }

    [Fact]
    public void TC_RES_003_Cancelar_rechazada_devuelve_TRANSICION_INVALIDA()
    {
        var reserva = EnEstado(EstadoReserva.Rechazada);

        var act = () => reserva.Cancelar(Coordinador, "Motivo", Ahora);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.TransicionInvalida);
    }

    public static IEnumerable<object[]> Matriz()
    {
        yield return [EstadoReserva.Pendiente, Accion.Aprobar, true];
        yield return [EstadoReserva.Pendiente, Accion.Rechazar, true];
        yield return [EstadoReserva.Pendiente, Accion.Cancelar, true];
        yield return [EstadoReserva.Pendiente, Accion.Completar, false];
        yield return [EstadoReserva.Aprobada, Accion.Aprobar, false];
        yield return [EstadoReserva.Aprobada, Accion.Rechazar, false];
        yield return [EstadoReserva.Aprobada, Accion.Cancelar, true];
        yield return [EstadoReserva.Aprobada, Accion.Completar, true];
        yield return [EstadoReserva.Rechazada, Accion.Aprobar, false];
        yield return [EstadoReserva.Rechazada, Accion.Rechazar, false];
        yield return [EstadoReserva.Rechazada, Accion.Cancelar, false];
        yield return [EstadoReserva.Rechazada, Accion.Completar, false];
        yield return [EstadoReserva.Completada, Accion.Aprobar, false];
        yield return [EstadoReserva.Completada, Accion.Rechazar, false];
        yield return [EstadoReserva.Completada, Accion.Cancelar, false];
        yield return [EstadoReserva.Completada, Accion.Completar, false];
        yield return [EstadoReserva.Cancelada, Accion.Aprobar, false];
        yield return [EstadoReserva.Cancelada, Accion.Rechazar, false];
        yield return [EstadoReserva.Cancelada, Accion.Cancelar, false];
        yield return [EstadoReserva.Cancelada, Accion.Completar, false];
    }

    /// <summary>Tabla de verdad del diseño; exercised a través de los cinco States concretos.</summary>
    [Theory]
    [MemberData(nameof(Matriz))]
    public void TC_TRANSICION_Matriz_de_estados(EstadoReserva estado, Accion accion, bool permitida)
    {
        var reserva = EnEstado(estado);

        var act = () => Ejecutar(reserva, accion);

        if (permitida)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<DomainException>()
                .Which.Code.Should().Be(DomainErrors.TransicionInvalida);
        }
    }

    public enum Accion
    {
        Aprobar,
        Rechazar,
        Cancelar,
        Completar
    }

    private static void Ejecutar(Reservation reserva, Accion accion)
    {
        switch (accion)
        {
            case Accion.Aprobar:
                reserva.Aprobar(Coordinador, Ahora);
                break;
            case Accion.Rechazar:
                reserva.Rechazar(Coordinador, "Espacio en mantención", Ahora);
                break;
            case Accion.Cancelar:
                reserva.Cancelar(Coordinador, "Docencia reprogramada", Ahora);
                break;
            case Accion.Completar:
                reserva.Completar(Coordinador, Ahora);
                break;
        }
    }

    private static Reservation EnEstado(EstadoReserva estado)
    {
        var reserva = Nueva();
        switch (estado)
        {
            case EstadoReserva.Pendiente:
                break;
            case EstadoReserva.Aprobada:
                reserva.Aprobar(Coordinador, Ahora);
                break;
            case EstadoReserva.Rechazada:
                reserva.Rechazar(Coordinador, "Espacio en mantención", Ahora);
                break;
            case EstadoReserva.Completada:
                reserva.Aprobar(Coordinador, Ahora);
                reserva.Completar(Coordinador, Ahora);
                break;
            case EstadoReserva.Cancelada:
                reserva.Cancelar(Coordinador, "Docencia reprogramada", Ahora);
                break;
        }

        reserva.Estado.Should().Be(estado);
        return reserva;
    }

    private static Reservation Nueva(DateTimeOffset? fin = null, Guid? usuarioId = null) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        usuarioId ?? SolicitanteId,
        Ahora.AddHours(-1),
        fin ?? Ahora.AddHours(1),
        "Clase de laboratorio",
        20);
}
