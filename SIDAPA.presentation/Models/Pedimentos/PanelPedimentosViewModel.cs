using SIDAPA.presentation.Models.Pedimentos;
using SIDAPA.application.DTOs.Core.Pedimento;

namespace SIDAPA.presentation.Models.Pedimentos
{
    public record PanelPedimentosViewModel(
    string? Busqueda,
    IReadOnlyList<PedimentoDto> Pedimentos,
    CalendarioViewModel Calendario)
    {
        public static PanelPedimentosViewModel Desde(PanelPedimentosDto panel, string? busqueda) =>
            new(busqueda, panel.Pedimentos, CalendarioViewModel.Crear(panel.Mes, panel.Hoy, panel.Pedimentos, busqueda));
    }

}
