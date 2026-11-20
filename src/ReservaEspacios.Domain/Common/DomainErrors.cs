namespace ReservaEspacios.Domain.Common;

/// <summary>Códigos de error observables por el cliente, según los specs OpenSpec.</summary>
public static class DomainErrors
{
    public const string NombreDuplicado = "NOMBRE_DUPLICADO";
    public const string CapacidadInvalida = "CAPACIDAD_INVALIDA";
    public const string HorarioInvalido = "HORARIO_INVALIDO";
    public const string NoAutorizado = "NO_AUTORIZADO";
    public const string TransicionInvalida = "TRANSICION_INVALIDA";
}
