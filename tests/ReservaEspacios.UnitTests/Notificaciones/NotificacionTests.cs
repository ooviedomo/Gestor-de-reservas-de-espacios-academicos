using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Notificaciones;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Notificaciones;

namespace ReservaEspacios.UnitTests.Notificaciones;

/// <summary>
/// NOT-001/002/003 y patrones de ADR-002: Observer (outbox), Command, Adapter,
/// Decorator y Strategy de reintentos.
/// </summary>
public sealed class NotificacionTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin =
        new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    private readonly Guid _reservaId = Guid.NewGuid();
    private readonly Guid _espacioId = Guid.NewGuid();
    private readonly Guid _solicitanteId = Guid.NewGuid();
    private readonly Mock<IReservaRepository> _reservas = new();
    private readonly Mock<ISpaceRepository> _espacios = new();
    private readonly Mock<IUsuarioIdentityService> _identity = new();
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private NotificacionPendienteDto? _agregada;

    public NotificacionTests()
    {
        _reservas.Setup(x => x.GetByIdAsync(_reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation(
                _reservaId, _espacioId, _solicitanteId, Inicio, Fin, "Clase", 10));
        _espacios.Setup(x => x.GetByIdAsync(_espacioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Space(
                _espacioId, "Laboratorio A-101", EspacioTipo.Laboratorio, 30, "Bloque A",
                new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0))));
        _identity.Setup(x => x.ObtenerPorIdAsync(_solicitanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DatosActorDto?>.Success(
                new DatosActorDto(
                    _solicitanteId, "Ana", Rol.Solicitante, "ana@universidad.edu")));
        _notificaciones.Setup(x => x.AgregarAsync(
                It.IsAny<NotificacionPendienteDto>(), It.IsAny<CancellationToken>()))
            .Callback<NotificacionPendienteDto, CancellationToken>((dto, _) => _agregada = dto)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task TC_NOT_001_Aprobacion_crea_notificacion_PENDIENTE_para_el_solicitante()
    {
        await Handler().HandleAsync(ReservaAprobada());

        _agregada.Should().NotBeNull();
        _agregada!.ReservaId.Should().Be(_reservaId);
        _agregada.Destinatario.Should().Be("ana@universidad.edu");
        _agregada.Asunto.Should().Be("Reserva aprobada");
        _agregada.Intentos.Should().Be(0);
        _agregada.Cuerpo.Should().Contain("Ana")
            .And.Contain("Laboratorio A-101")
            .And.Contain("Reserva aprobada");
    }

    [Fact]
    public async Task TC_NOT_001_Rechazo_incluye_el_motivo_en_el_contenido()
    {
        await Handler().HandleAsync(new ReservaRechazada(
            _reservaId, _espacioId, _solicitanteId, Guid.NewGuid(), Rol.Coordinador,
            "Espacio en mantención", DateTimeOffset.UtcNow));

        _agregada!.Asunto.Should().Be("Reserva rechazada");
        _agregada.Cuerpo.Should().Contain("Espacio en mantención");
    }

    [Fact]
    public async Task TC_NOT_001_Cancelacion_incluye_el_motivo_en_el_contenido()
    {
        await Handler().HandleAsync(new ReservaCancelada(
            _reservaId, _espacioId, _solicitanteId, Guid.NewGuid(), Rol.Solicitante,
            "Cambio de planes", DateTimeOffset.UtcNow));

        _agregada!.Asunto.Should().Be("Reserva cancelada");
        _agregada.Cuerpo.Should().Contain("Cambio de planes");
    }

    [Theory]
    [MemberData(nameof(EventosSinNotificacion))]
    public async Task TC_NOT_001_Creacion_y_completado_no_generan_notificacion(IDomainEvent evento)
    {
        await Handler().HandleAsync(evento);

        _notificaciones.Verify(
            x => x.AgregarAsync(It.IsAny<NotificacionPendienteDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_NOT_001_Sin_destinatario_no_genera_notificacion_ni_falla_la_transicion()
    {
        _identity.Setup(x => x.ObtenerPorIdAsync(_solicitanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DatosActorDto?>.Success(null));

        var actuar = () => Handler().HandleAsync(ReservaAprobada());

        await actuar.Should().NotThrowAsync();
        _notificaciones.Verify(
            x => x.AgregarAsync(It.IsAny<NotificacionPendienteDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_NOT_002_Envio_exitoso_marca_la_notificacion_como_ENVIADA()
    {
        var id = Guid.NewGuid();
        var enviar = new Mock<ICommandHandler<EnviarCorreoCommand>>();
        enviar.Setup(x => x.HandleAsync(It.IsAny<EnviarCorreoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);
        var procesador = new ProcesadorNotificaciones(
            _notificaciones.Object, enviar.Object, new BackoffExponencial());
        var ahora = DateTimeOffset.UtcNow;
        PrepararPendiente(id);

        var procesadas = await procesador.ProcesarAsync(ahora: ahora);

        procesadas.Should().Be(1);
        _notificaciones.Verify(
            x => x.MarcarEnviadaAsync(id, ahora, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_NOT_002_Fallo_temporal_deja_PENDIENTE_con_un_intento_y_el_error()
    {
        var id = Guid.NewGuid();
        var enviar = new Mock<ICommandHandler<EnviarCorreoCommand>>();
        enviar.Setup(x => x.HandleAsync(It.IsAny<EnviarCorreoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMTP_FALLO: conexión rechazada"));
        var procesador = new ProcesadorNotificaciones(
            _notificaciones.Object, enviar.Object, new BackoffExponencial());
        PrepararPendiente(id);

        await procesador.ProcesarAsync();

        _notificaciones.Verify(x => x.RegistrarFalloAsync(
            id, 1, "SMTP_FALLO: conexión rechazada", It.IsAny<DateTimeOffset>(),
            false, It.IsAny<CancellationToken>()), Times.Once);
        _notificaciones.Verify(
            x => x.MarcarEnviadaAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_NOT_002_Quinto_fallo_pasa_a_ERROR_y_deja_de_reintentarse()
    {
        var id = Guid.NewGuid();
        var enviar = new Mock<ICommandHandler<EnviarCorreoCommand>>();
        enviar.Setup(x => x.HandleAsync(It.IsAny<EnviarCorreoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMTP_FALLO"));
        var procesador = new ProcesadorNotificaciones(
            _notificaciones.Object, enviar.Object, new BackoffExponencial());
        PrepararPendiente(id, intentos: 4);

        await procesador.ProcesarAsync();

        _notificaciones.Verify(x => x.RegistrarFalloAsync(
            id, 5, "SMTP_FALLO", It.IsAny<DateTimeOffset>(),
            true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_NOT_002_Proveedor_Sin_host_devuelve_fallo_controlado_sin_red()
    {
        var sender = new SmtpEmailSender(
            Options.Create(new EmailOptions { Host = string.Empty }));

        var resultado = await sender.EnviarAsync(
            new EnviarCorreoCommand("ana@universidad.edu", "Asunto", "Cuerpo"));

        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Be("SMTP_NO_CONFIGURADO");
    }

    [Fact]
    public async Task TC_NOT_002_Decorator_de_logging_no_altera_el_resultado_del_comando()
    {
        var inner = new Mock<ICommandHandler<EnviarCorreoCommand>>();
        inner.Setup(x => x.HandleAsync(It.IsAny<EnviarCorreoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);
        var decorator = new LoggingCommandHandler<EnviarCorreoCommand>(
            inner.Object, Mock.Of<ILogger<LoggingCommandHandler<EnviarCorreoCommand>>>());

        var resultado = await decorator.HandleAsync(
            new EnviarCorreoCommand("ana@universidad.edu", "Asunto", "Cuerpo"));

        resultado.IsSuccess.Should().BeTrue();
        inner.Verify(x => x.HandleAsync(
            It.IsAny<EnviarCorreoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void TC_NOT_002_Strategy_de_reintentos_tiene_limite_de_5_y_backoff_exponencial()
    {
        var backoff = new BackoffExponencial();

        backoff.MaxIntentos.Should().Be(5);
        backoff.Calcular(0).Should().Be(TimeSpan.FromSeconds(1));
        backoff.Calcular(1).Should().Be(TimeSpan.FromSeconds(2));
        backoff.Calcular(3).Should().Be(TimeSpan.FromSeconds(8));
    }

    private NotificacionOutboxHandler Handler() => new(
        _reservas.Object, _espacios.Object, _identity.Object, _notificaciones.Object);

    private ReservaAprobada ReservaAprobada() => new(
        _reservaId, _espacioId, _solicitanteId, Guid.NewGuid(), Rol.Coordinador,
        Inicio, DateTimeOffset.UtcNow);

    private void PrepararPendiente(Guid id, int intentos = 0) =>
        _notificaciones.Setup(x => x.ObtenerPendientesAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new NotificacionPendienteDto(
                    id, _reservaId, "ana@universidad.edu", "Reserva aprobada", "Cuerpo", intentos)
            });

    public static TheoryData<IDomainEvent> EventosSinNotificacion()
    {
        var ahora = DateTimeOffset.UtcNow;
        return new TheoryData<IDomainEvent>
        {
            new ReservaCreada(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Rol.Solicitante, Inicio, Fin, ahora),
            new ReservaCompletada(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Rol.Coordinador, Fin, Automatico: true, ahora)
        };
    }
}
