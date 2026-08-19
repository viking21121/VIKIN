using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion;

public class RegistrarPago
{
    private readonly IPagoRepository pagoRepository;
    private readonly IAuditoriaRepository auditoriaRepository;
    private readonly ICicloPagoRepository? cicloPagoRepository;

    public RegistrarPago(IPagoRepository pagoRepository, IAuditoriaRepository auditoriaRepository, ICicloPagoRepository? cicloPagoRepository = null)
    {
        this.pagoRepository = pagoRepository;
        this.auditoriaRepository = auditoriaRepository;
        this.cicloPagoRepository = cicloPagoRepository;
    }

    public Pago Ejecutar(
    int alumnoId,
    int anio,
    int mes,
    decimal montoUsd,
    DateTime fechaPago,
    string metodoPago,
    string referencia,
    decimal tasaBcv,
    decimal montoBs)
    {
        if (alumnoId <= 0)
            throw new ArgumentException(
                "El alumno no es válido.");

        if (montoUsd <= 0)
            throw new ArgumentException(
                "El monto debe ser mayor que cero.");

        if (tasaBcv <= 0)
            throw new ArgumentException(
                "La tasa BCV debe ser mayor que cero.");

        if (montoBs <= 0)
            throw new ArgumentException(
                "El monto en Bolívares debe ser mayor que cero.");

        if (pagoRepository.ExisteReferencia(referencia))
            throw new InvalidOperationException(
                "La referencia de pago ya existe.");

        string folio =
            pagoRepository.GenerarSiguienteFolio(anio);

        var pago = new Pago
        {
            AlumnoId = alumnoId,
            Anio = anio,
            Mes = mes,
            MontoUsd = montoUsd,
            FechaPago = fechaPago,
            MetodoPago = metodoPago,
            Referencia = referencia,
            TasaBcv = tasaBcv,
            MontoBs = montoBs,
            Folio = folio
        };

        var pagoGuardado = pagoRepository.Crear(pago);
        cicloPagoRepository?.RenovarSiCorresponde(alumnoId, fechaPago);

        auditoriaRepository.Registrar(
            "PAGO_ABONO",
            $"Registró abono de ${pagoGuardado.MontoUsd:0.##} " +
            $"para el alumno ID {pagoGuardado.AlumnoId}. " +
            $"Folio: {pagoGuardado.Folio}");

        return pagoGuardado;
    }
}

