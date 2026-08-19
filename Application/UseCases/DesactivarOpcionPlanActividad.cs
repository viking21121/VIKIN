using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class DesactivarOpcionPlanActividad
    {
        private readonly IOpcionPlanActividadRepository repository;
        public DesactivarOpcionPlanActividad(IOpcionPlanActividadRepository repository) => this.repository = repository;
        public void Ejecutar(int id) => repository.Desactivar(id);
    }
}
