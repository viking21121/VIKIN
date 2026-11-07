using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases
{
    public class CrearPlanActividad
    {
        private readonly IPlanActividadRepository repository;
        public CrearPlanActividad(IPlanActividadRepository repository) => this.repository = repository;
        public int Ejecutar(int actividadId, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, decimal precio)
            => repository.Crear(actividadId, nombre, nivelOpcionId, frecuenciaOpcionId, clasesOpcionId, precio);
    }
}
