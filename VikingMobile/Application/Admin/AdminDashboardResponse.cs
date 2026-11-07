namespace VikingMobile.Application.Admin;

/// <summary>
/// Respuesta del servidor Viking al autenticar a un administrador
/// y traer el resumen de su establecimiento (Modo Administrador).
/// </summary>
public class AdminDashboardResponse
{
    public bool Ok { get; set; }

    public string Mensaje { get; set; } = "";

    public string Establecimiento { get; set; } = "";
    public int ActividadId { get; set; }

    public int TotalAlumnos { get; set; }

    public int Activos { get; set; }

    public int PorVencer { get; set; }

    public int Morosos { get; set; }

    public decimal RecaudadoMes { get; set; }

    public string Moneda { get; set; } = "$";
}
