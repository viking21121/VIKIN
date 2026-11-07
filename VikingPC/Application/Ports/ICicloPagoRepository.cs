namespace AsistenciaNatacion.Application.Ports;

public interface ICicloPagoRepository
{
    void ConfigurarCiclo(int alumnoId, DateTime fechaInicio, DateTime fechaProximoPago);
    void RenovarSiCorresponde(int alumnoId, DateTime fechaPago);
}
