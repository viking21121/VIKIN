using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class DesactivarPlanActividad
    {
        private readonly IPlanActividadRepository repository;

        public DesactivarPlanActividad(IPlanActividadRepository repository)
        {
            this.repository = repository;
        }

        public void Ejecutar(int id)
        {
            repository.Desactivar(id);
        }
    }
}