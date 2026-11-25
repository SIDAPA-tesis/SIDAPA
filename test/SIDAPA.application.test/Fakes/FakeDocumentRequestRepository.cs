using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.test.Fakes;

/// <summary>
/// Tabla ope_solicitud_documento en memoria. Lee la referencia (cliente, número, estatus) del
/// <see cref="FakeReferenciaRepository"/>, así el estatus que ve es el que dejó el último cambio.
/// <see cref="MarkAttendedAsync"/> respeta la condición "solo si sigue abierta", igual que el repositorio real.
/// </summary>
public sealed class FakeDocumentRequestRepository(FakeReferenciaRepository referencias) : IDocumentRequestRepository
{
    public List<DocumentRequestEntity> Requests { get; } = [];

    /// <summary>Simula que otro envío cerró la solicitud entre la lectura y el UPDATE.</summary>
    public bool SimulateAlreadyClosed { get; set; }

    /// <summary>Nombre que muestra el catálogo para un tipo (en la BD sale de cat_tipo_documento).</summary>
    public static string NameOf(DocumentTypeEnum type) => $"Documento {type}";

    public DocumentRequestEntity Seed(int referenciaId, DocumentTypeEnum type, int requestedByUserId,
        string? note = null, DateTime? attendedAt = null)
    {
        var request = new DocumentRequestEntity
        {
            Id = Requests.Count + 1,
            ReferenciaId = referenciaId,
            DocumentTypeId = (short)type,
            Note = note,
            RequestedByUserId = requestedByUserId,
            RequestedAt = new DateTime(2026, 11, 22, 9, 0, 0),
            AttendedAt = attendedAt
        };
        Requests.Add(request);
        return request;
    }

    public Task CreateAsync(DocumentRequestEntity request, CancellationToken ct = default)
    {
        request.Id = Requests.Count + 1;
        Requests.Add(request);
        return Task.CompletedTask;
    }

    public async Task<DocumentRequestDetailDto?> GetForClientAsync(int requestId, int clientId, CancellationToken ct = default)
    {
        var request = Requests.FirstOrDefault(x => x.Id == requestId);
        if (request is null)
            return null;

        var referencia = await referencias.GetByIdAsync(request.ReferenciaId, ct);
        if (referencia is null || referencia.ClientId != clientId)
            return null;

        return new DocumentRequestDetailDto(
            request.Id, request.ReferenciaId, referencia.ReferenciaNumber, (ReferenciaStatusEnum)referencia.StatusId,
            (DocumentTypeEnum)request.DocumentTypeId, NameOf((DocumentTypeEnum)request.DocumentTypeId), request.Note,
            request.RequestedByUserId, request.RequestedAt, request.AttendedAt is not null);
    }

    public async Task<IReadOnlyList<PendingDocumentRequestDto>> GetOpenByClientAsync(int clientId, CancellationToken ct = default)
    {
        var result = new List<PendingDocumentRequestDto>();
        foreach (var request in Requests.Where(x => x.AttendedAt is null))
        {
            var referencia = await referencias.GetByIdAsync(request.ReferenciaId, ct);
            if (referencia?.ClientId != clientId || referencia.StatusId != (short)ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS)
                continue;

            result.Add(new PendingDocumentRequestDto(request.Id, request.ReferenciaId, referencia.ReferenciaNumber,
                (DocumentTypeEnum)request.DocumentTypeId, NameOf((DocumentTypeEnum)request.DocumentTypeId), request.Note,
                request.RequestedAt));
        }
        return result;
    }

    public Task<bool> MarkAttendedAsync(int requestId, int documentId, DateTime attendedAt, CancellationToken ct = default)
    {
        var request = Requests.FirstOrDefault(x => x.Id == requestId);
        if (SimulateAlreadyClosed || request is null || request.AttendedAt is not null)
            return Task.FromResult(false);

        request.ResponseDocumentId = documentId;
        request.AttendedAt = attendedAt;
        return Task.FromResult(true);
    }

    public Task<int> CountOpenAsync(int referenciaId, CancellationToken ct = default) =>
        Task.FromResult(Requests.Count(x => x.ReferenciaId == referenciaId && x.AttendedAt is null));
}
