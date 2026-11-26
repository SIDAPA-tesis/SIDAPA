using SIDAPA.application.DTOs.Core.Notifications;
using SIDAPA.presentation.Models.Core.Notifications;

namespace SIDAPA.presentation.test.Models.Core.Notifications;

/// <summary>
/// Tarjetas de "Novedades" de la página de inicio: salen de los avisos del usuario (NotificationService, SCRUM-59),
/// con la hora en hora local y marcadas como nuevas mientras no se lean.
/// </summary>
public class NovedadesViewModelTests
{
    private static readonly TimeZoneInfo Mexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
    private static readonly DateTime CreatedUtc = new(2026, 11, 24, 18, 0, 0);

    private static NotificationDto Aviso(int id, bool leido) =>
        new(id, $"Aviso {id}", ReferenciaId: null, leido, CreatedUtc);

    [Fact]
    public void Desde_HoraEnUtc_LaMuestraEnHoraDeMexico()
    {
        var vm = NovedadesViewModel.Desde([Aviso(1, leido: false)], Mexico);

        vm.Novedades.ShouldHaveSingleItem().Fecha
            .ShouldBe(new DateTime(2026, 11, 24, 12, 0, 0));   // 18:00 UTC = 12:00 en CDMX (UTC-6)
    }

    [Fact]
    public void Desde_AvisosLeidosYNoLeidos_SoloMarcaComoNuevosLosNoLeidos()
    {
        var vm = NovedadesViewModel.Desde([Aviso(1, leido: false), Aviso(2, leido: true)], Mexico);

        vm.Novedades.Select(n => (n.Mensaje, n.Nueva)).ShouldBe([("Aviso 1", true), ("Aviso 2", false)]);
    }

    [Fact]
    public void Desde_SinAvisos_QuedaVacio()
    {
        var vm = NovedadesViewModel.Desde([], Mexico);

        vm.Novedades.ShouldBeEmpty();
    }
}
