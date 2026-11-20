namespace ReservaEspacios.Domain.Common;

/// <summary>Códigos de error observables por el cliente, según los specs OpenSpec.</summary>
public static class DomainErrors
{
    public const string NombreDuplicado = "NOMBRE_DUPLICADO";
    public const string CapacidadInvalida = "CAPACIDAD_INVALIDA";
    public const string HorarioInvalido = "HORARIO_INVALIDO";
    public const string NoAutorizado = "NO_AUTORIZADO";
    public const string TransicionInvalida = "TRANSICION_INVALIDA";
    /// <summary>Error operativo (no escenario de spec): el id no existe en el catálogo.</summary>
    public const string EspacioNoEncontrado = "ESPACIO_NO_ENCONTRADO";

    // add-reservas-y-aprobaciones: USR-*, AUTH-*, RES-*, APR-* y COM-*.
    public const string EmailDuplicado = "EMAIL_DUPLICADO";
    public const string ContrasenaDebil = "CONTRASENA_DEBIL";
    public const string CredencialesInvalidas = "CREDENCIALES_INVALIDAS";
    public const string CuentaDesactivada = "CUENTA_DESACTIVADA";
    public const string NoAutenticado = "NO_AUTENTICADO";
    public const string SolapeDetectado = "SOLAPE_DETECTADO";
    public const string RangoInvalido = "RANGO_INVALIDO";
    public const string CapacidadExcedida = "CAPACIDAD_EXCEDIDA";
    public const string MotivoObligatorio = "MOTIVO_OBLIGATORIO";
    public const string ReservaNoEncontrada = "RESERVA_NO_ENCONTRADA";
    public const string UsuarioNoEncontrado = "USUARIO_NO_ENCONTRADO";
}
