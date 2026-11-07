namespace VikingMobile.Application.Admin;

/// <summary>
/// Un método de pago del establecimiento, tal como lo configura el
/// administrador (dónde le pueden pagar: banco, teléfono, email, etc.
/// como texto libre en <see cref="Datos"/>).
/// </summary>
public class MetodoPagoAdminResumen
{
    public string Metodo { get; set; } = "";

    /// <summary>Clave corta y estable para enviar de vuelta al guardar (p. ej. "pagomovil").</summary>
    public string Clave { get; set; } = "";

    /// <summary>Texto libre con los datos para pagar por este método.</summary>
    public string Datos { get; set; } = "";

    public bool Activo { get; set; }
}
