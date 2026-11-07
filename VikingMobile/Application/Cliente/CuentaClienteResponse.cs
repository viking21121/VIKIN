namespace VikingMobile.Application.Cliente;

/// <summary>
/// Respuesta del servidor Viking al consultar la cuenta de un
/// alumno desde el Modo Cliente de Viking Mobile.
/// </summary>
public class CuentaClienteResponse
{
    public bool Ok { get; set; }

    public string Mensaje { get; set; } = "";

    public string CodigoAlumno { get; set; } = "";

    public string NombreAlumno { get; set; } = "";

    public string Actividad { get; set; } = "";

    public string Plan { get; set; } = "";

    /// <summary>
    /// Estado del plan: "activo", "por_vencer" o "moroso".
    /// Determina el color del indicador en Mi Cuenta.
    /// </summary>
    public string Estado { get; set; } = "activo";

    public string FechaVencimiento { get; set; } = "";

    public string? Cedula { get; set; }

    public string? Telefono { get; set; }

    public string? FotoRuta { get; set; }

    public int? ClasesRestantes { get; set; }

    public decimal SaldoPendiente { get; set; }

    public List<ReciboResumen> Recibos { get; set; } = new();

    public List<AsistenciaMensual> Asistencias { get; set; } = new();

    /// <summary>
    /// No viene del servidor: se marca en tiempo de ejecución cuando
    /// la información mostrada proviene de la caché local (sin conexión).
    /// </summary>
    public bool DesdeCache { get; set; }
}
