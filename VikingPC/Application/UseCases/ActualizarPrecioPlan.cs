using AsistenciaNatacion.Application.Ports;
namespace AsistenciaNatacion.Application.UseCases;
public class ActualizarPrecioPlan
{
    private readonly IPrecioPlanRepository repository;
    public ActualizarPrecioPlan(IPrecioPlanRepository repository)=>this.repository=repository;
    public void Ejecutar(int planId,decimal precio,int anio,int mes,int actividadId)=>repository.Actualizar(planId,precio,anio,mes,actividadId);
}
