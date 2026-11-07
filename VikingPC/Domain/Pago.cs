namespace AsistenciaNatacion;

public class Pago
{
    public int Id { get; set; }

    public int AlumnoId { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public decimal MontoUsd { get; set; }

    public decimal TasaBcv { get; set; }

    public decimal MontoBs { get; set; }

    public DateTime FechaPago { get; set; }

    public string MetodoPago { get; set; } = "";

    public string Referencia { get; set; } = "";

    public string Folio { get; set; } = "";
}