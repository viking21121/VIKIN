using AsistenciaNatacion;
using AsistenciaNatacion.Application.Ports;

namespace Viking.Tests;

/// <summary>Repositorio de pagos en memoria para probar los casos de uso.</summary>
internal sealed class PagoRepositoryFake : IPagoRepository
{
    public bool ReferenciaExiste { get; set; }
    public List<Pago> Guardados { get; } = new();

    public Pago Crear(Pago pago)
    {
        pago.Id = Guardados.Count + 1;
        Guardados.Add(pago);
        return pago;
    }

    public Pago? ObtenerPorId(int id) => Guardados.FirstOrDefault(x => x.Id == id);

    public Pago? ObtenerPorFolio(string folio) => Guardados.FirstOrDefault(x => x.Folio == folio);

    public bool ExisteReferencia(string referencia) => ReferenciaExiste;

    public string GenerarSiguienteFolio(int anio) => $"{anio}-0001";

    public List<Pago> ObtenerPorAlumnoPeriodo(int alumnoId, int anio, int mes) =>
        Guardados.Where(x => x.AlumnoId == alumnoId && x.Anio == anio && x.Mes == mes).ToList();
}

internal sealed class AuditoriaRepositoryFake : IAuditoriaRepository
{
    public bool Falla { get; set; }
    public List<string> Acciones { get; } = new();

    public void Registrar(string accion, string detalle, string? usuario = null)
    {
        if (Falla)
            throw new InvalidOperationException("La auditoría falló.");

        Acciones.Add(accion);
    }
}

internal sealed class CicloPagoRepositoryFake : ICicloPagoRepository
{
    public List<(int AlumnoId, DateTime Inicio, DateTime Proximo)> Ciclos { get; } = new();
    public List<(int AlumnoId, DateTime FechaPago)> Renovaciones { get; } = new();

    public void ConfigurarCiclo(int alumnoId, DateTime fechaInicio, DateTime fechaProximoPago) =>
        Ciclos.Add((alumnoId, fechaInicio, fechaProximoPago));

    public void RenovarSiCorresponde(int alumnoId, DateTime fechaPago) =>
        Renovaciones.Add((alumnoId, fechaPago));
}

internal sealed class AsistenciaRepositoryFake : IAasistenciaRepository
{
    public List<(string IdOperacion, int AlumnoId, DateTime Fecha, bool Presente)> Operaciones { get; } = new();
    public bool OperacionNueva { get; set; } = true;

    public void Marcar(int alumnoId, DateTime fecha, bool presente) { }

    public bool MarcarIdempotente(string idOperacion, int alumnoId, DateTime fecha, bool presente)
    {
        Operaciones.Add((idOperacion, alumnoId, fecha, presente));
        return OperacionNueva;
    }

    public int ContarMes(int alumnoId, int anio, int mes) => 0;
    public HashSet<int> ObtenerIdsAsistieronHoy(int actividadId) => new();
    public Alumno? ObtenerAlumnoPorCodigo(string codigo, int actividadId) => null;
    public Alumno? ObtenerAlumnoPorCodigoGlobal(string codigo) => null;
    public (int exito, int fallidos) CargarDesdeArchivo(string rutaArchivo, DateTime fecha, int actividadId) => (0, 0);
}
