using System;
using System.Collections.Generic;

namespace AsistenciaNatacion.Application.Ports;

public interface IAasistenciaRepository
{
    void Marcar(int alumnoId, DateTime fecha, bool presente);

    /// <summary>
    /// Marca la asistencia solo si el identificador de operación no se había
    /// usado antes. Devuelve true si la operación era nueva y false si ya
    /// estaba registrada (en ese caso no cambia nada).
    /// </summary>
    bool MarcarIdempotente(string idOperacion, int alumnoId, DateTime fecha, bool presente);
    int ContarMes(int alumnoId, int anio, int mes);
    HashSet<int> ObtenerIdsAsistieronHoy(int actividadId);
    Alumno? ObtenerAlumnoPorCodigo(string codigo, int actividadId);
    Alumno? ObtenerAlumnoPorCodigoGlobal(string codigo);
    (int exito, int fallidos) CargarDesdeArchivo(string rutaArchivo, DateTime fecha, int actividadId);
}
