using AsistenciaNatacion.Application.Ports;
namespace AsistenciaNatacion.Application.UseCases;
public class ExisteReferenciaPago
{
    private readonly IPagoRepository repository;
    public ExisteReferenciaPago(IPagoRepository repository)=>this.repository=repository;
    public bool Ejecutar(string referencia,out string detalle){ if(!repository.ExisteReferencia(referencia)){detalle="";return false;} detalle="La referencia ya está registrada."; return true; }
}
