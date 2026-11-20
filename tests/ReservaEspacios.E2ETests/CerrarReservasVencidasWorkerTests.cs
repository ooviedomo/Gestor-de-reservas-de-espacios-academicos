using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Web.BackgroundServices;

namespace ReservaEspacios.E2ETests;

/// <summary>
/// COM-001 (tarea 4.2): el hosted service resuelve el caso de uso en un scope nuevo
/// en cada tick. Se prueba con un intervalo de un segundo y un caso de uso simulado.
/// </summary>
public sealed class CerrarReservasVencidasWorkerTests
{
    [Fact]
    public async Task TC_COM_001_Worker_dispara_el_cierre_automatico_en_cada_tick()
    {
        var senal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cierre = new Mock<ICerrarReservasVencidas>();
        cierre.Setup(x => x.EjecutarAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => senal.TrySetResult());

        var servicios = new ServiceCollection();
        servicios.AddSingleton(cierre.Object);
        using var proveedor = servicios.BuildServiceProvider();
        var worker = new CerrarReservasVencidasWorker(
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new CierreAutomaticoOptions { IntervaloSegundos = 1 }),
            NullLogger<CerrarReservasVencidasWorker>.Instance);

        using var cancellation = new CancellationTokenSource();
        await worker.StartAsync(cancellation.Token);
        try
        {
            var ganador = await Task.WhenAny(senal.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            ganador.Should().Be(senal.Task, "el worker debe ejecutar el cierre en el primer tick");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        cierre.Verify(x => x.EjecutarAsync(null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
