namespace VikingMobile.Application.Cliente;

/// <summary>
/// Un método de pago activo que el alumno puede usar para pagar
/// (los datos para transferir/enviar ya vienen listos para mostrar).
/// </summary>
public class MetodoPagoClienteResumen
{
    public string Metodo { get; set; } = "";

    public string Clave { get; set; } = "";

    public string Datos { get; set; } = "";
}
