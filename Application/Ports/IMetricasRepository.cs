using AsistenciaNatacion;

namespace AsistenciaNatacion.Application.Ports
{
    public interface IMetricasRepository
    {
        MetricasResumen ObtenerMetricas(int anio, int mes, int actividadId);
    }
}