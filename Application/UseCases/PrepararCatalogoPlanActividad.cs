using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class PrepararCatalogoPlanActividad
    {
        private readonly IOpcionPlanActividadRepository repository;

        public PrepararCatalogoPlanActividad(IOpcionPlanActividadRepository repository)
        {
            this.repository = repository;
        }

        public void Ejecutar(int actividadId)
        {
            if (actividadId <= 0)
                return;

            repository.AsegurarCatalogoBase(actividadId);
        }
    }
}
