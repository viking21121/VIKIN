using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
using System.Collections.Generic;

namespace AsistenciaNatacion.Application.Mobile
{
    public class ObtenerActividadesMovil : IObtenerActividadesMovil
    {
        private readonly IActividadRepository actividadRepository;

        public ObtenerActividadesMovil(
            IActividadRepository actividadRepository)
        {
            this.actividadRepository =
                actividadRepository;
        }

        public List<Actividad> Ejecutar()
        {
            return actividadRepository.ObtenerTodas();
        }
    }
}