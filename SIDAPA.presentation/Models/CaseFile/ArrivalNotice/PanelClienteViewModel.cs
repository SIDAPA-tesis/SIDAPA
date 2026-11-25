using SIDAPA.application.DTOs.Core.Referencias;

namespace SIDAPA.presentation.Models.CaseFile.ArrivalNotice;

/// <summary>Panel "Mis avisos de arribo" del Cliente.</summary>
/// <param name="ReferenciaCreada">Número recién asignado, para confirmar la recepción y resaltar su renglón.</param>
public record PanelClienteViewModel(IReadOnlyList<ClientReferenciaDto> Referencias, string? ReferenciaCreada)
{
    public IEnumerable<FilaReferenciaViewModel> Filas =>
        Referencias.Select(r => new FilaReferenciaViewModel(r, r.ReferenciaNumber == ReferenciaCreada));
}

public record FilaReferenciaViewModel(ClientReferenciaDto Referencia, bool Destacada);
