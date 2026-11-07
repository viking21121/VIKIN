using AsistenciaNatacion.Domain;
using System.Collections.Generic;

namespace AsistenciaNatacion.Application.Mobile
{
    public interface IObtenerActividadesMovil
    {
        List<Actividad> Ejecutar();
    }
}