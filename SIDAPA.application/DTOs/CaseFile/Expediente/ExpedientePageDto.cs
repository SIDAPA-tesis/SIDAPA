namespace SIDAPA.application.DTOs.CaseFile.Expediente;

public record ExpedientePageDto(IReadOnlyList<ExpedienteSummaryDto> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
