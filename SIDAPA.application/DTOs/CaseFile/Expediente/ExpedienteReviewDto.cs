namespace SIDAPA.application.DTOs.CaseFile.Expediente;

/// <param name="CanBeReviewed">El estatus todavía permite marcar documentos (no dice nada del rol del usuario).</param>
public record ExpedienteReviewDto(ExpedienteDetailDto Detail, IReadOnlyList<ChecklistItemDto> Checklist, bool CanBeReviewed);
