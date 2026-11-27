using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.test.Fakes;

/// <summary>Tabla ope_documento en memoria.</summary>
public sealed class FakeDocumentRepository : IDocumentRepository
{
    /// <summary>Referencias que "existen": id → número (R-000007…).</summary>
    public Dictionary<int, string> ReferenciaNumbers { get; } = [];
    public List<DocumentEntity> Saved { get; } = [];
    public bool FailOnAdd { get; set; }

    /// <summary>Atajo para preparar un documento ya guardado.</summary>
    public DocumentEntity Seed(int referenciaId, DocumentTypeEnum type, DocumentStatusEnum status, string fileName = "archivo.pdf")
    {
        var document = new DocumentEntity
        {
            Id = Saved.Count + 1,
            ReferenciaId = referenciaId,
            DocumentTypeId = (short)type,
            FileName = fileName,
            BucketPath = $"referencias/{referenciaId}/{type}/{Saved.Count + 1}.pdf",
            SizeInBytes = 1024,
            Status = status.ToString(),
            UploadedByUserId = 1,
            UploadedAt = new DateTime(2026, 11, 1, 10, 0, 0)
        };
        Saved.Add(document);
        return document;
    }

    public Task<string?> GetReferenciaNumberAsync(int referenciaId, CancellationToken ct = default) =>
        Task.FromResult(ReferenciaNumbers.GetValueOrDefault(referenciaId));

    public Task AddAsync(DocumentEntity document, CancellationToken ct = default)
    {
        if (FailOnAdd)
            throw new InvalidOperationException("Falla simulada de la BD");

        document.Id = Saved.Count + 1;
        Saved.Add(document);
        return Task.CompletedTask;
    }

    public Task<DocumentEntity?> GetByIdAsync(int documentId, CancellationToken ct = default) =>
        Task.FromResult(Saved.FirstOrDefault(x => x.Id == documentId));

    public Task<IReadOnlyList<DocumentEntity>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DocumentEntity>>(Saved.Where(x => x.ReferenciaId == referenciaId).ToList());

    public Task<bool> UpdateStatusAsync(int documentId, int referenciaId, DocumentStatusEnum status, string? note, CancellationToken ct = default)
    {
        var document = Saved.FirstOrDefault(x => x.Id == documentId && x.ReferenciaId == referenciaId);
        if (document is null)
            return Task.FromResult(false);

        document.Status = status.ToString();
        document.Note = note;
        return Task.FromResult(true);
    }
}
