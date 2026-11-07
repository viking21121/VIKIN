namespace VikingMobile.Application.Cliente;

/// <summary>
/// Una solicitud de pago que el establecimiento le hizo al alumno
/// (Modo Cliente): la ve, sube su comprobante, y luego ve si fue
/// aprobada o rechazada.
/// </summary>
public class SolicitudPagoClienteResumen
{
    public long SolicitudId { get; set; }

    public decimal MontoUsd { get; set; }

    public string Concepto { get; set; } = "";

    /// <summary>"pendiente" | "comprobante_subido" | "aprobado" | "rechazado".</summary>
    public string Estado { get; set; } = "pendiente";

    public string FechaCreacion { get; set; } = "";

    public string? FechaResolucion { get; set; }

    public string ComentarioAdmin { get; set; } = "";

    public string FolioPago { get; set; } = "";
}
