using AsistenciaNatacion;
using AsistenciaNatacion.Application.UseCases;

namespace Viking.Tests;

public class RenovarMembresiaTests
{
    [Fact]
    public void AlumnoInvalido_Lanza()
    {
        var caso = new RenovarMembresia(new CicloPagoRepositoryFake());

        Assert.Throws<ArgumentException>(() => caso.Ejecutar(0, new DateTime(2026, 11, 1)));
    }

    [Fact]
    public void AlumnoValido_DelegaEnElRepositorio()
    {
        var repo = new CicloPagoRepositoryFake();
        var fecha = new DateTime(2026, 11, 1);

        new RenovarMembresia(repo).Ejecutar(5, fecha);

        Assert.Equal((5, fecha), Assert.Single(repo.Renovaciones));
    }
}

public class ActualizarCicloPagoTests
{
    [Fact]
    public void AlumnoInvalido_Lanza()
    {
        var caso = new ActualizarCicloPago(new CicloPagoRepositoryFake());

        Assert.Throws<ArgumentException>(() =>
            caso.Ejecutar(0, new DateTime(2026, 11, 1), new DateTime(2026, 12, 1)));
    }

    [Fact]
    public void ProximoPago_NoPosteriorAlInicio_Lanza()
    {
        var caso = new ActualizarCicloPago(new CicloPagoRepositoryFake());
        var inicio = new DateTime(2026, 11, 1);

        Assert.Throws<ArgumentException>(() => caso.Ejecutar(1, inicio, inicio));
        Assert.Throws<ArgumentException>(() => caso.Ejecutar(1, inicio, inicio.AddDays(-1)));
    }

    [Fact]
    public void FechasValidas_ConfiguranElCicloSinHoras()
    {
        var repo = new CicloPagoRepositoryFake();
        var caso = new ActualizarCicloPago(repo);

        caso.Ejecutar(3, new DateTime(2026, 11, 1, 15, 30, 0), new DateTime(2026, 12, 1, 8, 0, 0));

        var ciclo = Assert.Single(repo.Ciclos);
        Assert.Equal(3, ciclo.AlumnoId);
        Assert.Equal(new DateTime(2026, 11, 1), ciclo.Inicio);
        Assert.Equal(new DateTime(2026, 12, 1), ciclo.Proximo);
    }

    [Fact]
    public void SiLaAuditoriaFalla_ElCicloIgualSeActualiza()
    {
        var repo = new CicloPagoRepositoryFake();
        var auditoria = new AuditoriaRepositoryFake { Falla = true };
        var caso = new ActualizarCicloPago(repo, auditoria);

        caso.Ejecutar(3, new DateTime(2026, 11, 1), new DateTime(2026, 12, 1));

        Assert.Single(repo.Ciclos);
    }
}
