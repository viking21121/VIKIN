using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class ObtenerMetricasRapidas
    {
        private readonly IMetricasRepository repository;

        public ObtenerMetricasRapidas(IMetricasRepository repository)
        {
            this.repository = repository;
        }

        public MetricasResumen Ejecutar(int anio, int mes, int actividadId)
        {
            return repository.ObtenerMetricas(anio, mes, actividadId);
        }
    }
}