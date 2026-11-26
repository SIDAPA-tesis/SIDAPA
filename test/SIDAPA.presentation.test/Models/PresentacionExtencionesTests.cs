using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Models;

namespace SIDAPA.presentation.test.Models;

/// <summary>
/// Panel de pedimentos del Capturador: cada urgencia se pinta con un color contextual de Bootstrap
/// (badge, día del calendario). Un valor fuera del mapa terminaría en una clase inexistente (text-bg-).
/// </summary>
public class PresentacionExtencionesTests
{
    [Theory]
    [InlineData(UrgenciaEnum.Vencido, "danger")]
    [InlineData(UrgenciaEnum.Proximo, "warning")]
    [InlineData(UrgenciaEnum.EnTiempo, "success")]
    public void ColorBootstrap_UrgenciaActiva_DaSuColor(UrgenciaEnum urgencia, string esperado)
    {
        urgencia.ColorBootstrap().ShouldBe(esperado);
    }

    [Fact]
    public void ColorBootstrap_TodasLasUrgencias_TienenColorValido()
    {
        string[] validos = ["danger", "warning", "success", "secondary"];

        foreach (var urgencia in Enum.GetValues<UrgenciaEnum>())
        {
            urgencia.ColorBootstrap().ShouldBeOneOf(validos);
        }
    }
}
