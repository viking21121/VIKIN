using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.UseCases
{
    public class ObtenerOpcionesPlanActividad
    {
        private readonly IOpcionPlanActividadRepository repository;
        public ObtenerOpcionesPlanActividad(IOpcionPlanActividadRepository repository) => this.repository = repository;
        public List<OpcionPlanActividad> Ejecutar(int actividadId, string tipo) => repository.ObtenerPorActividad(actividadId, tipo);
    }
}
