using System.Collections.Generic;

namespace AsistenciaNatacion.Application.Ports;

public interface IEstadoAlumnoRepository
{
    EstadoAlumnoMes? ObtenerEstado(int alumnoId, int anio, int mes);
    List<EstadoAlumnoMes> ObtenerDashboard(int anio, int mes, int actividadId);
}
