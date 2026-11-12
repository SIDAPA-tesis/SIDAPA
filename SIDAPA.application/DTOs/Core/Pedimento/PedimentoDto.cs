using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.Core.Pedimento
{
    public record PedimentoDto(
        int Id,
        string Numero,
        string Empresa,
    string? ImagenUrl,
    int Avance,
    DateOnly FechaLimite,
    UrgenciaEnum Urgencia);

    /// <param name="Mes">Primer día del mes que se muestra en el calendario.</param>
    /// <param name="Hoy">Fecha actual según el servidor.</param>
    public record PanelPedimentosDto(IReadOnlyList<PedimentoDto> Pedimentos, DateOnly Mes, DateOnly Hoy);
}
