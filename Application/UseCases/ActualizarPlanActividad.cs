using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class ActualizarPlanActividad
    {
        private readonly IPlanActividadRepository repository;
        public ActualizarPlanActividad(IPlanActividadRepository repository) => this.repository = repository;
        public void Ejecutar(int id, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, decimal precio)
            => repository.Actualizar(id, nombre, nivelOpcionId, frecuenciaOpcionId, clasesOpcionId, precio);
    }
}
