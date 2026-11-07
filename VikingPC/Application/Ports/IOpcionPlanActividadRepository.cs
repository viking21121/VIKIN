using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.Ports
{
    public interface IOpcionPlanActividadRepository
    {
        List<OpcionPlanActividad> ObtenerPorActividad(int actividadId, string tipo);
        OpcionPlanActividad? ObtenerPorId(int id);
        int Crear(int actividadId, string tipo, string nombre, int? valorEntero);
        void Actualizar(int id, string nombre, int? valorEntero);
        void Desactivar(int id);
        void AsegurarCatalogoBase(int actividadId);
    }
}
