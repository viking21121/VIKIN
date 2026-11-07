using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsistenciaNatacion;

public class ObtenerPagosAlumnoPeriodo
{
    private readonly IPagoRepository pagoRepository;

    public ObtenerPagosAlumnoPeriodo(IPagoRepository pagoRepository)
    {
        this.pagoRepository = pagoRepository;
    }

    public List<Pago> Ejecutar(
        int alumnoId,
        int anio,
        int mes)
    {
        if (alumnoId <= 0)
            throw new ArgumentException(
                "El alumno no es válido.");

        if (anio <= 0)
            throw new ArgumentException(
                "El año no es válido.");

        if (mes < 1 || mes > 12)
            throw new ArgumentException(
                "El mes no es válido.");

        return pagoRepository.ObtenerPorAlumnoPeriodo(
            alumnoId,
            anio,
            mes);
    }
}
