using AsistenciaNatacion.Domain;
using System.Collections.Generic;

namespace AsistenciaNatacion.Application.Ports
{
    public interface IActividadRepository
    {
        List<Actividad> ObtenerTodas();
        Actividad? ObtenerPorId(int id);
        int Crear(string nombre);
        void Desactivar(int id);
    }
}