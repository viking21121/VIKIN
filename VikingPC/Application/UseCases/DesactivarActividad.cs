using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class DesactivarActividad
    {
        private readonly IActividadRepository actividadRepository;

        public DesactivarActividad(IActividadRepository actividadRepository)
        {
            this.actividadRepository = actividadRepository;
        }

        public void Ejecutar(int actividadId)
        {
            actividadRepository.Desactivar(actividadId);
        }
    }
}