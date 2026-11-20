using Microsoft.AspNetCore.Identity;
using ReservaEspacios.Application;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Infrastructure;
using ReservaEspacios.Infrastructure.Identity;
using ReservaEspacios.Infrastructure.Notificaciones;
using ReservaEspacios.Infrastructure.Persistence;
using ReservaEspacios.Web;
using ReservaEspacios.Web.BackgroundServices;
using ReservaEspacios.Web.Components;
using ReservaEspacios.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ReservaEspacios")
    ?? throw new InvalidOperationException(
        "Defina ConnectionStrings__ReservaEspacios antes de iniciar la aplicación (ADR-003).");

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

// NOT-002: configuración SMTP desde variables de entorno sin secretos versionados.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Correo:Smtp"));

// USR-001/003: el email es el nombre de usuario y la directiva de contraseña
// coincide con CONTRASENA_DEBIL (8 caracteres, mayúscula y número).
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<ReservaEspaciosDbContext>()
    .AddRoles<ApplicationRole>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUsuarioActual, IdentityRoleAdapter>();
builder.Services.AddScoped<ISesionWeb, SesionWeb>();

// COM-001: cierre diario de reservas APROBADA vencidas. El intervalo se configura con
// Reservas__CierreAutomatico__IntervaloSegundos (por defecto, 24 h).
builder.Services.Configure<CierreAutomaticoOptions>(
    builder.Configuration.GetSection("Reservas:CierreAutomatico"));
builder.Services.AddHostedService<CerrarReservasVencidasWorker>();

// NOT-001/002/003: envío diferido del outbox. Los tick's no procesan notificaciones
// al arrancar, igual que el cierre automático.
builder.Services.Configure<NotificacionesOptions>(
    builder.Configuration.GetSection("Notificaciones"));
builder.Services.AddHostedService<NotificacionesWorker>();

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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapApiEndpoints();

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

