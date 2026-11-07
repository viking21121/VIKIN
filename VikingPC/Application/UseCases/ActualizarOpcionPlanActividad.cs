using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class ActualizarOpcionPlanActividad
    {
        private readonly IOpcionPlanActividadRepository repository;
        public ActualizarOpcionPlanActividad(IOpcionPlanActividadRepository repository) => this.repository = repository;
        public void Ejecutar(int id, string nombre, int? valorEntero) => repository.Actualizar(id, nombre, valorEntero);
    }
}
