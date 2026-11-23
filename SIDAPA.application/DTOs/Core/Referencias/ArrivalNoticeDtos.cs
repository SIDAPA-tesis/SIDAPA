using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.Core.Referencias;

/// <summary>Un archivo del aviso de arribo. <see cref="Content"/> debe permitir Seek (se valida y luego se sube).</summary>
public record ArrivalNoticeFileDto(DocumentTypeEnum DocumentType, string FileName, long SizeInBytes, Stream Content);

/// <summary>
/// Aviso de arribo que captura el Cliente (HU-02). <see cref="ClientId"/> y <see cref="UserId"/> salen del token,
/// nunca del formulario.
/// </summary>
public record CreateArrivalNoticeDto(
    int ClientId,
    int UserId,
    OperationTypeEnum OperationType,
    DateOnly? ArrivalDate,
    string? AduanaCode,
    string? Notes,
    IReadOnlyList<ArrivalNoticeFileDto> Files);

public record ArrivalNoticeCreatedDto(int ReferenciaId, string ReferenciaNumber, int DocumentCount);

/// <summary>Renglón del panel del cliente.</summary>
public record ClientReferenciaDto(
    int Id,
    string ReferenciaNumber,
    string AduanaName,
    DateOnly? ArrivalDate,
    ReferenciaStatusEnum Status,
    string StatusName,
    DateTime CreatedAt,
    int DocumentCount);

/// <summary>Opción del catálogo de aduanas (Apéndice 1).</summary>
public record AduanaOptionDto(string Code, string Name);
