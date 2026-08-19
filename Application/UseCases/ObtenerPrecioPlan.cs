using AsistenciaNatacion.Application.Ports;
namespace AsistenciaNatacion.Application.UseCases;
public class ObtenerPrecioPlan
{
    private readonly IPrecioPlanRepository repository;
    public ObtenerPrecioPlan(IPrecioPlanRepository repository)=>this.repository=repository;
    public decimal Ejecutar(int planId,int anio,int mes)=>repository.Obtener(planId,anio,mes);
}
