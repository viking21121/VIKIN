using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsistenciaNatacion;

public class ObtenerPagoPorFolio
{
    private readonly IPagoRepository pagoRepository;

    public ObtenerPagoPorFolio(IPagoRepository pagoRepository)
    {
        this.pagoRepository = pagoRepository;
    }

    public Pago? Ejecutar(string folio)
    {
        if (string.IsNullOrWhiteSpace(folio))
            throw new ArgumentException(
                "El folio es obligatorio.");

        folio = folio.Trim();

        return pagoRepository.ObtenerPorFolio(folio);
    }
}
