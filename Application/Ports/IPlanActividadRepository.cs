using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.Ports
{
    public interface IPlanActividadRepository
    {
        List<PlanActividad> ObtenerPorActividad(int actividadId);
        PlanActividad? ObtenerPorId(int id);

        int Crear(int actividadId, string nombre, string? nivel, int? frecuenciaSemanal, int? clasesMes, decimal precio);
        int Crear(int actividadId, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, decimal precio);

        void Actualizar(int id, string nombre, string? nivel, int? frecuenciaSemanal, int? clasesMes, decimal precio);
        void Actualizar(int id, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, decimal precio);

        void Desactivar(int id);
    }
}
