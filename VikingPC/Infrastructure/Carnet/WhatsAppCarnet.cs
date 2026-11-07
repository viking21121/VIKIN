using System;
using System.Diagnostics;
using System.Linq;

namespace AsistenciaNatacion.Infrastructure.Carnet;

/// <summary>
/// WhatsApp no permite adjuntar un archivo desde un enlace. Por eso el flujo es:
/// 1) se copia el PDF al portapapeles, 2) se abre el chat con el número ya puesto,
/// 3) la persona pega con Ctrl+V y envía.
/// </summary>
public static class WhatsAppCarnet
{
    /// <summary>Código de país por defecto para números locales (58 = Venezuela).</summary>
    public const string CodigoPaisPorDefecto = "58";

    /// <summary>Devuelve el número en formato internacional solo con dígitos, o null si no sirve.</summary>
    public static string? NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;
        string d = new string(telefono.Where(char.IsDigit).ToArray());
        if (d.StartsWith("00")) d = d.Substring(2);
        else if (d.StartsWith("0")) d = CodigoPaisPorDefecto + d.Substring(1);
        else if (d.Length == 10) d = CodigoPaisPorDefecto + d;
        return d.Length >= 11 && d.Length <= 15 ? d : null;
    }

    public static void AbrirChat(string telefonoInternacional, string mensaje)
    {
        string url = $"https://wa.me/{telefonoInternacional}?text={Uri.EscapeDataString(mensaje)}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
