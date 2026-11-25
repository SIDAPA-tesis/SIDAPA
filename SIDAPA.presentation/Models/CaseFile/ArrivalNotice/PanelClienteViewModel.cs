using SIDAPA.application.DTOs.Core.Referencias;

namespace SIDAPA.presentation.Models.CaseFile.ArrivalNotice;

/// <summary>Panel "Mis avisos de arribo" del Cliente.</summary>
/// <param name="ReferenciaCreada">Número recién asignado, para confirmar la recepción y resaltar su renglón.</param>
/// <param name="ZonaHoraria">Zona en la que se muestran las horas; la base las guarda en UTC.</param>
public record PanelClienteViewModel(
    IReadOnlyList<ClientReferenciaDto> Referencias, string? ReferenciaCreada, TimeZoneInfo ZonaHoraria)
{
    public IEnumerable<FilaReferenciaViewModel> Filas =>
        Referencias.Select(r => new FilaReferenciaViewModel(
            r,
            r.ReferenciaNumber == ReferenciaCreada,
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc), ZonaHoraria)));
}

/// <param name="Creada">Fecha de creación ya en la hora local del panel.</param>
public record FilaReferenciaViewModel(ClientReferenciaDto Referencia, bool Destacada, DateTime Creada);
