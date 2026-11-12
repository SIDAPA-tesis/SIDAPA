using Microsoft.EntityFrameworkCore;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories;

public class DocumentRepository(SidapaDbContext context) : IDocumentRepository
{
    public Task<string?> GetReferenciaNumberAsync(int referenciaId, CancellationToken ct = default) =>
        context.Referencias
            .Where(x => x.Id == referenciaId)
            .Select(x => (string?)x.ReferenciaNumber)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(DocumentEntity document, CancellationToken ct = default)
    {
        context.Documents.Add(document);
        await context.SaveChangesAsync(ct);
    }

    public Task<DocumentEntity?> GetByIdAsync(int documentId, CancellationToken ct = default) =>
        context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentId, ct);

    public async Task<IReadOnlyList<DocumentEntity>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default) =>
        await context.Documents
            .AsNoTracking()
            .Where(x => x.ReferenciaId == referenciaId)
            .OrderByDescending(x => x.UploadedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);
}