using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
using System.Collections.Generic;

namespace AsistenciaNatacion.Application.UseCases
{
    public class ObtenerPlanesPorActividad
    {
        private readonly IPlanActividadRepository repository;

        public ObtenerPlanesPorActividad(IPlanActividadRepository repository)
        {
            this.repository = repository;
        }

        public List<PlanActividad> Ejecutar(int actividadId)
        {
            return repository.ObtenerPorActividad(actividadId);
        }
    }
}