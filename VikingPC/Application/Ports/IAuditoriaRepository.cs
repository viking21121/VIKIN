using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsistenciaNatacion;

public interface IAuditoriaRepository
{
    void Registrar(
        string accion,
        string detalle,
        string? usuario = null);
}