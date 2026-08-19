using System;
using System.Collections.Generic;

namespace AsistenciaNatacion.Application.Ports;

public interface IAasistenciaRepository
{
    void Marcar(int alumnoId, DateTime fecha, bool presente);
    int ContarMes(int alumnoId, int anio, int mes);
    HashSet<int> ObtenerIdsAsistieronHoy(int actividadId);
    Alumno? ObtenerAlumnoPorCodigo(string codigo, int actividadId);
    (int exito, int fallidos) CargarDesdeArchivo(string rutaArchivo, DateTime fecha, int actividadId);
}
