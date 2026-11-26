using SIDAPA.application.DTOs.Core.Pedimento;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Models.Dashboard;

namespace SIDAPA.presentation.test.Models.Dashboard;

/// <summary>Resumen de pedimentos en el Inicio del capturador: conteo por urgencia y los pendientes que vencen primero.</summary>
public class ResumenPedimentosViewModelTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 26);

    private static PedimentoDto Pedimento(int id, int dias, UrgenciaEnum urgencia) =>
        new(id, $"P-{id}", "Empresa", null, 50, Hoy.AddDays(dias), urgencia);

    [Fact]
    public void Desde_CuentaPorUrgenciaEnOrdenFijo()
    {
        var resumen = ResumenPedimentosViewModel.Desde(
        [
            Pedimento(1, -1, UrgenciaEnum.Vencido),
            Pedimento(2, 2, UrgenciaEnum.Proximo),
            Pedimento(3, 3, UrgenciaEnum.Proximo),
            Pedimento(4, -5, UrgenciaEnum.Completado),
        ]);

        resumen.Total.ShouldBe(4);
        resumen.Indicadores.Select(i => (i.Titulo, i.Total, i.Color)).ShouldBe(
        [
            ("Vencidos", 1, "danger"),
            ("Próximos a vencer", 2, "warning"),
            ("En tiempo", 0, "success"),
            ("Completados", 1, "secondary"),
        ]);
    }

    [Fact]
    public void Desde_ProximosAVencer_SinCompletadosPorFechaYHastaElMaximo()
    {
        var pedimentos = Enumerable.Range(1, 8).Select(i => Pedimento(i, 10 - i, UrgenciaEnum.EnTiempo))
            .Append(Pedimento(99, -30, UrgenciaEnum.Completado))
            .ToList();

        var proximos = ResumenPedimentosViewModel.Desde(pedimentos).ProximosAVencer;

        proximos.Count.ShouldBe(ResumenPedimentosViewModel.MaximoProximos);
        proximos.ShouldNotContain(p => p.Urgencia == UrgenciaEnum.Completado);
        proximos.Select(p => p.FechaLimite).ShouldBeInOrder(SortDirection.Ascending);
        proximos[0].Id.ShouldBe(8);   // la fecha más cercana
    }

    [Fact]
    public void Desde_SinPedimentos_IndicadoresEnCeroYListaVacia()
    {
        var resumen = ResumenPedimentosViewModel.Desde([]);

        resumen.Indicadores.ShouldAllBe(i => i.Total == 0);
        resumen.ProximosAVencer.ShouldBeEmpty();
    }
}
