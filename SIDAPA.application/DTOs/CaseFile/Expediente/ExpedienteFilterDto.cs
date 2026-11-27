namespace SIDAPA.application.DTOs.CaseFile.Expediente;

/// <param name="PendingReviewOnly">Solo referencias Recibidas o En revisión.</param>
public record ExpedienteFilterDto(string? Query, bool PendingReviewOnly);
