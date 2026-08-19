namespace AsistenciaNatacion.Application.Ports;

public interface IPrecioPlanRepository
{
    decimal Obtener(int planId, int anio, int mes);
    void Actualizar(int planId, decimal precio, int anio, int mes, int actividadId);
}
