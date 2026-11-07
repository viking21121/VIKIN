namespace VikingMobile.Application.Admin;

public class ReportesAdminResponse
{
    public bool Ok { get; set; }

    public string Mensaje { get; set; } = "";

    public int Anio { get; set; }

    public int Mes { get; set; }

    public decimal RecaudadoTotal { get; set; }

    public int CantidadPagos { get; set; }

    public List<PagoMetodoResumen> PagosPorMetodo { get; set; } = new();

    public int AsistenciasTotal { get; set; }

    public int AlumnosConAsistencia { get; set; }

    public List<AsistenciaDiaResumen> AsistenciasPorDia { get; set; } = new();
}

public class PagoMetodoResumen
{
    public string Metodo { get; set; } = "";

    public decimal Total { get; set; }

    public int Cantidad { get; set; }
}

public class AsistenciaDiaResumen
{
    public string Fecha { get; set; } = "";

    public int Cantidad { get; set; }
}
