namespace VikingMobile.Infrastructure.Offline;

public class OfflineAttendance
{
    public int Id { get; set; }

    /// <summary>
    /// Identificador único de esta asistencia, generado al escanear.
    /// Se envía a Viking PC para que reconozca un reenvío y no lo repita.
    /// Los registros guardados por versiones anteriores lo reciben al cargarse.
    /// </summary>
    public string IdOperacion { get; set; } = "";

    public string CodigoAlumno { get; set; } = "";

    public string Qr { get; set; } = "";

    public int? ActividadId { get; set; }

    public DateTime FechaHora { get; set; }

    public bool Sincronizada { get; set; }

    public DateTime? FechaSincronizacion { get; set; }

    public string? UltimoError { get; set; }
}
