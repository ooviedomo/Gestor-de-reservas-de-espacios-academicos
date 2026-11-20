using ReservaEspacios.Application;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Infrastructure;
using ReservaEspacios.Web.Components;
using ReservaEspacios.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ReservaEspacios")
    ?? throw new InvalidOperationException(
        "Defina ConnectionStrings__ReservaEspacios antes de iniciar la aplicación (ADR-003).");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<IUsuarioActual, UsuarioActualStub>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/disponibilidad", async (
    Guid spaceId,
    DateTimeOffset inicio,
    DateTimeOffset fin,
    IConsultarDisponibilidad consultar,
    CancellationToken cancellationToken) =>
{
    var resultado = await consultar.HandleAsync(
        new ConsultarDisponibilidadCommand(spaceId, inicio, fin), cancellationToken);

    return resultado.IsSuccess
        ? Results.Ok(resultado.Value)
        : Results.NotFound(new { error = resultado.ErrorCode });
});

app.Run();

/// <summary>Expone el host para WebApplicationFactory en pruebas de integración.</summary>
public partial class Program { }

