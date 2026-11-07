using AsistenciaNatacion.Application.Ports;
namespace AsistenciaNatacion.Application.UseCases;
public class ObtenerAuditoria
{
    private readonly IAuditoriaConsultaRepository repository;
    public ObtenerAuditoria(IAuditoriaConsultaRepository repository)=>this.repository=repository;
    public List<EventoAuditoria> Ejecutar(DateTime? desde,DateTime? hasta,string? filtro,int actividadId)=>repository.Obtener(desde,hasta,filtro,actividadId);
}
