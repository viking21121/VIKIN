namespace VikingMobile.Application.Communication;

public class SyncAttendanceRequest
{
    public string IdOperacion { get; set; } = "";
    public string CodigoAlumno { get; set; } = "";
    public string Qr { get; set; } = "";
    public int? ActividadId { get; set; }
    public DateTime FechaHora { get; set; }
}
