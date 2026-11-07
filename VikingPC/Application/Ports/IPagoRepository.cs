using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsistenciaNatacion;

public interface IPagoRepository
{
    Pago Crear(Pago pago);

    Pago? ObtenerPorId(int id);

    Pago? ObtenerPorFolio(string folio);

    bool ExisteReferencia(string referencia);

    string GenerarSiguienteFolio(int anio);

    List<Pago> ObtenerPorAlumnoPeriodo(
    int alumnoId,
    int anio,
    int mes);
}
