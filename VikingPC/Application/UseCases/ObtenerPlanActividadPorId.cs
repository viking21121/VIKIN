using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
namespace AsistenciaNatacion.Application.UseCases;
public class ObtenerPlanActividadPorId
{
    private readonly IPlanActividadRepository repository;
    public ObtenerPlanActividadPorId(IPlanActividadRepository repository)=>this.repository=repository;
    public PlanActividad? Ejecutar(int planId)=>repository.ObtenerPorId(planId);
}
