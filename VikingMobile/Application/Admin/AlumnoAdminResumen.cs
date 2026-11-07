using VikingMobile.Application.Communication;

namespace VikingMobile.Application.Admin;

/// <summary>
/// Una fila del listado de alumnos del Modo Administrador
/// (api/admin/alumnos). Nombres de propiedad iguales, letra por
/// letra salvo mayúsculas, a las claves JSON que envía Viking PC.
/// </summary>
public class AlumnoAdminResumen
{
    public int AlumnoId { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Cedula { get; set; } = "";

    public string Telefono { get; set; } = "";

    public string? FotoRuta { get; set; }

    public bool TieneFoto { get; set; }

    public string? FotoUrl { get; set; }

    public bool NoTieneFoto => !TieneFoto;

    public string UrlFotoCompleta =>
        TieneFoto
            ? $"{AppState.CommunicationServerUrl}/api/alumnos/{Uri.EscapeDataString(Codigo)}/foto?t={DateTime.UtcNow.Ticks}"
            : "";

    public string CedulaTexto =>
        string.IsNullOrWhiteSpace(Cedula) ? "Sin C.I." : $"C.I. {Cedula}";

    public string TelefonoTexto =>
        string.IsNullOrWhiteSpace(Telefono) ? "Sin teléfono" : Telefono;

    public string Iniciales
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Nombre)) return "?";
            var partes = Nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpperInvariant();
            return $"{partes[0][0]}{partes[1][0]}".ToUpperInvariant();
        }
    }

    public string PlanNombre { get; set; } = "";

    public bool ControlaClases { get; set; }

    public int ClasesMes { get; set; }

    public int Tomadas { get; set; }

    public int Restantes { get; set; }

    public DateTime? FechaVencimientoPlan { get; set; }

    public int DiasRestantes { get; set; }

    public decimal CuotaTotal { get; set; }

    public decimal MontoAbonado { get; set; }

    public decimal MontoPendiente { get; set; }

    /// <summary>"activo" | "por_vencer" | "moroso".</summary>
    public string EstadoMembresia { get; set; } = "activo";
}
