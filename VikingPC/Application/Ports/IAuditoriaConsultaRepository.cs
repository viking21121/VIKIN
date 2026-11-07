namespace AsistenciaNatacion.Application.Ports;

public interface IAuditoriaConsultaRepository
{
    List<EventoAuditoria> Obtener(DateTime? desde, DateTime? hasta, string? filtroTexto, int actividadId);
}
