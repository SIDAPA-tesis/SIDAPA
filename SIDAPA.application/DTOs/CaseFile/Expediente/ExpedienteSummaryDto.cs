using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

/// <param name="PendingReviewCount">Documentos todavía en RECIBIDO.</param>
public record ExpedienteSummaryDto(
    int Id,
    string ReferenciaNumber,
    string ClientName,
    string AduanaName,
    ReferenciaStatusEnum Status,
    string StatusName,
    DateTime CreatedAt,
    int DocumentCount,
    int PendingReviewCount,
    string? CapturadorName);
