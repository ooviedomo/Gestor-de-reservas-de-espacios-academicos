using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Domain.Espacios;

/// <summary>Entidad del catálogo de espacios (ESP-001, ESP-002).</summary>
public sealed class Space
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public EspacioTipo Tipo { get; private set; }
    public int Capacidad { get; private set; }
    public string Ubicacion { get; private set; } = string.Empty;
    public HorarioAtencion Horario { get; private set; } = null!;
    public bool Activo { get; private set; }

    private Space()
    {
    }

    public Space(Guid id, string nombre, EspacioTipo tipo, int capacidad, string ubicacion,
        HorarioAtencion horario, bool activo = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);

        if (capacidad <= 0)
        {
            throw new DomainException(DomainErrors.CapacidadInvalida, "La capacidad debe ser mayor que cero.");
        }

        Id = id;
        Nombre = nombre.Trim();
        Tipo = tipo;
        Capacidad = capacidad;
        Ubicacion = ubicacion?.Trim() ?? string.Empty;
        Horario = horario ?? throw new DomainException(DomainErrors.HorarioInvalido, "El horario es obligatorio.");
        Activo = activo;
    }

    public void Editar(string nombre, EspacioTipo tipo, int capacidad, string ubicacion, HorarioAtencion horario)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);

        if (capacidad <= 0)
        {
            throw new DomainException(DomainErrors.CapacidadInvalida, "La capacidad debe ser mayor que cero.");
        }

        Nombre = nombre.Trim();
        Tipo = tipo;
        Capacidad = capacidad;
        Ubicacion = ubicacion?.Trim() ?? string.Empty;
        Horario = horario ?? throw new DomainException(DomainErrors.HorarioInvalido, "El horario es obligatorio.");
    }

    /// <summary>
    /// ESP-002: desactivar no elimina reservas; solo oculta el espacio de nuevas consultas.
    /// </summary>
    public void Desactivar() => Activo = false;

    public void Reactivar() => Activo = true;
}
