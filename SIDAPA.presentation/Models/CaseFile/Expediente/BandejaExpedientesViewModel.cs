using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.presentation.Models.CaseFile.Documents;

namespace SIDAPA.presentation.Models.CaseFile.Expediente;

/// <param name="Recortada">Llegó al tope del servicio: hay más referencias que no se muestran.</param>
public record BandejaExpedientesViewModel(
    IReadOnlyList<FilaExpedienteViewModel> Filas, string? Busqueda, bool PorRevisar, bool Recortada)
{
    public static BandejaExpedientesViewModel Desde(
        IReadOnlyList<ExpedienteSummaryDto> referencias, string? busqueda, bool porRevisar, int tope, TimeZoneInfo zonaHoraria) =>
        new(
            referencias.Select(r => new FilaExpedienteViewModel(r, SolicitudesPendientesViewModel.HoraLocal(r.CreatedAt, zonaHoraria))).ToList(),
            busqueda,
            porRevisar,
            referencias.Count >= tope);
}
