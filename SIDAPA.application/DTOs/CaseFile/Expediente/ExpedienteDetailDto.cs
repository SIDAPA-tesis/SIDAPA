using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

public record ExpedienteDetailDto(
    int Id,
    string ReferenciaNumber,
    string ClientName,
    string AduanaName,
    string OperationTypeName,
    ReferenciaStatusEnum Status,
    string StatusName,
    DateOnly? ArrivalDate,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<ExpedienteDocumentDto> Documents,
    IReadOnlyList<RequiredDocumentDto> RequiredDocuments);
