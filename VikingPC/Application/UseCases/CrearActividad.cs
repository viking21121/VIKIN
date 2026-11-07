using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class CrearActividad
    {
        private readonly IActividadRepository actividadRepository;

        public CrearActividad(IActividadRepository actividadRepository)
        {
            this.actividadRepository = actividadRepository;
        }

        public int Ejecutar(string nombre)
        {
            return actividadRepository.Crear(nombre);
        }
    }
}