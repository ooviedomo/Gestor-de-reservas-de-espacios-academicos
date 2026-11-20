using Microsoft.AspNetCore.Identity;

namespace ReservaEspacios.Infrastructure.Identity;

/// <summary>
/// El `Name` guarda el código del plan (`SOLICITANTE`, `COORDINADOR`, `ADMINISTRADOR`);
/// `Etiqueta` guarda el nombre en español que muestran los specs y la UI (design.md).
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string Etiqueta { get; set; } = string.Empty;
}
