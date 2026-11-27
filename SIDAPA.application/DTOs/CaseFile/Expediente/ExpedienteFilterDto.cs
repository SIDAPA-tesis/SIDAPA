namespace SIDAPA.application.DTOs.CaseFile.Expediente;

/// <param name="PendingReviewOnly">Solo referencias Recibidas o En revisión.</param>
/// <param name="Page">Desde 1; una página fuera de rango regresa la última.</param>
public record ExpedienteFilterDto(string? Query, bool PendingReviewOnly, int Page = 1);
