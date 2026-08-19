using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class CrearOpcionPlanActividad
    {
        private readonly IOpcionPlanActividadRepository repository;
        public CrearOpcionPlanActividad(IOpcionPlanActividadRepository repository) => this.repository = repository;
        public int Ejecutar(int actividadId, string tipo, string nombre, int? valorEntero) => repository.Crear(actividadId, tipo, nombre, valorEntero);
    }
}
