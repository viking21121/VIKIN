namespace VikingMobile.Application.Admin;

public class PagoAdminResumen
{
    public string Folio { get; set; } = "";

    public DateTime FechaPago { get; set; }

    public decimal MontoUsd { get; set; }

    public string MetodoPago { get; set; } = "";

    public string Referencia { get; set; } = "";
}
