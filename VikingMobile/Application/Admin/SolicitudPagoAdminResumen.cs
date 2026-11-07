namespace VikingMobile.Application.Admin;

/// <summary>
/// Una solicitud de pago pendiente de revisión (el administrador la
/// creó, y el alumno puede haber subido ya su comprobante).
/// </summary>
public class SolicitudPagoAdminResumen
{
    public long SolicitudId { get; set; }

    public string CodigoAlumno { get; set; } = "";

    public string NombreAlumno { get; set; } = "";

    public decimal MontoUsd { get; set; }

    public string Concepto { get; set; } = "";

    /// <summary>"pendiente" | "comprobante_subido" | "aprobado" | "rechazado".</summary>
    public string Estado { get; set; } = "pendiente";

    public string FechaCreacion { get; set; } = "";

    public string? FechaComprobante { get; set; }

    public bool TieneComprobante { get; set; }
}
