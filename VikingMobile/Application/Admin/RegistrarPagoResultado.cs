namespace VikingMobile.Application.Admin;

public class RegistrarPagoResultado
{
    public bool Ok { get; set; }

    public string Mensaje { get; set; } = "";

    public string Folio { get; set; } = "";

    public decimal MontoUsd { get; set; }

    public DateTime FechaPago { get; set; }
}
