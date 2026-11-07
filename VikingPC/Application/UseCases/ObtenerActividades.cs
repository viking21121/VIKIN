using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
using System.Collections.Generic;

namespace AsistenciaNatacion.Application.UseCases
{
    public class ObtenerActividades
    {
        private readonly IActividadRepository actividadRepository;

        public ObtenerActividades(IActividadRepository actividadRepository)
        {
            this.actividadRepository = actividadRepository;
        }

        public List<Actividad> Ejecutar()
        {
            return actividadRepository.ObtenerTodas();
        }
    }
}