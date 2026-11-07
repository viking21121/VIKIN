using AsistenciaNatacion;

namespace Viking.Tests;

public class RegistrarPagoTests
{
    private readonly PagoRepositoryFake pagos = new();
    private readonly AuditoriaRepositoryFake auditoria = new();
    private readonly CicloPagoRepositoryFake ciclos = new();

    private RegistrarPago CrearCasoDeUso() => new(pagos, auditoria, ciclos);

    private static readonly DateTime Fecha = new(2026, 11, 1);

    [Fact]
    public void AlumnoInvalido_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            CrearCasoDeUso().Ejecutar(0, 2026, 11, 40m, Fecha, "Zelle", "REF-1", 0m, 0m));
    }

    [Theory]
    [InlineData("Efectivo $")]
    [InlineData("Zelle")]
    [InlineData("Binance Pay")]
    public void MetodosEnDolares_ConMontoCero_Lanzan(string metodo)
    {
        Assert.Throws<ArgumentException>(() =>
            CrearCasoDeUso().Ejecutar(1, 2026, 11, 0m, Fecha, metodo, "REF-1", 0m, 0m));
    }

    [Theory]
    [InlineData("Efectivo bs")]
    [InlineData("Transferencia")]
    [InlineData("Pagomovil")]
    public void MetodosEnBolivares_SinTasa_Lanzan(string metodo)
    {
        Assert.Throws<ArgumentException>(() =>
            CrearCasoDeUso().Ejecutar(1, 2026, 11, 0m, Fecha, metodo, "REF-1", 0m, 1000m));
    }

    [Theory]
    [InlineData("Efectivo bs")]
    [InlineData("Transferencia")]
    [InlineData("Pagomovil")]
    public void MetodosEnBolivares_SinMontoBs_Lanzan(string metodo)
    {
        Assert.Throws<ArgumentException>(() =>
            CrearCasoDeUso().Ejecutar(1, 2026, 11, 0m, Fecha, metodo, "REF-1", 50m, 0m));
    }

    [Fact]
    public void ReferenciaDuplicada_Lanza_YNoGuardaNiRenuevaNiAudita()
    {
        pagos.ReferenciaExiste = true;

        Assert.Throws<InvalidOperationException>(() =>
            CrearCasoDeUso().Ejecutar(1, 2026, 11, 40m, Fecha, "Zelle", "REF-1", 0m, 0m));

        Assert.Empty(pagos.Guardados);
        Assert.Empty(ciclos.Renovaciones);
        Assert.Empty(auditoria.Acciones);
    }

    [Fact]
    public void PagoValido_GuardaConFolio_RenuevaCiclo_YAudita()
    {
        var pago = CrearCasoDeUso().Ejecutar(7, 2026, 11, 40m, Fecha, "Zelle", "REF-1", 0m, 0m);

        Assert.Equal("2026-0001", pago.Folio);
        Assert.Equal(7, pago.AlumnoId);
        Assert.Equal(40m, pago.MontoUsd);

        Assert.Single(pagos.Guardados);
        Assert.Equal((7, Fecha), Assert.Single(ciclos.Renovaciones));
        Assert.Equal("PAGO_ABONO", Assert.Single(auditoria.Acciones));
    }

    [Fact]
    public void PagoEnBolivares_ConTasaYMonto_EsValido()
    {
        var pago = CrearCasoDeUso().Ejecutar(1, 2026, 11, 10m, Fecha, "Pagomovil", "REF-2", 50m, 500m);

        Assert.Equal(500m, pago.MontoBs);
        Assert.Equal(50m, pago.TasaBcv);
    }
}
