using System.Text;
using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.application.Services.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using Xunit;

namespace SIDAPA.Infraestructure.test;

/// <summary>
/// Pruebas de <see cref="DocumentService"/> (SCRUM-48) con fakes a mano: no tocan la BD ni el bucket.
/// Usan el <see cref="DocumentFileValidator"/> real de SCRUM-50.
/// </summary>
public class DocumentServiceTests
{
    private const int ReferenciaId = 7;
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\nfactura de prueba\n%%EOF");

    private static (DocumentService Service, FakeDocumentRepository Repository, FakeCloudStorage Storage) Create()
    {
        var repository = new FakeDocumentRepository();
        var storage = new FakeCloudStorage();
        return (new DocumentService(repository, new DocumentFileValidator(), storage), repository, storage);
    }

    private static UploadDocumentDto Upload(string fileName, byte[] bytes, int referenciaId = ReferenciaId) =>
        new(referenciaId, DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, fileName, bytes.Length,
            new MemoryStream(bytes), UploadedByUserId: 3);

    [Fact]
    public async Task UploadAsync_SubeElArchivoCompletoYLoRegistra()
    {
        var (service, repository, storage) = Create();

        var result = await service.UploadAsync(Upload("Factura 001.pdf", Pdf));

        var uploaded = Assert.Single(storage.Uploaded);
        Assert.StartsWith("referencias/R-000007/FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR/", uploaded.Key);
        Assert.EndsWith(".pdf", uploaded.Key);
        Assert.Equal(Pdf, uploaded.Value);   // completo: el validador no se "come" los primeros bytes
        Assert.Equal("application/pdf", storage.ContentTypes[uploaded.Key]);

        var saved = Assert.Single(repository.Saved);
        Assert.Equal(uploaded.Key, saved.BucketPath);
        Assert.Equal("Factura 001.pdf", saved.FileName);
        Assert.Equal(nameof(DocumentStatusEnum.RECIBIDO), saved.Status);
        Assert.Equal(3, saved.UploadedByUserId);
        Assert.Equal(DocumentStatusEnum.RECIBIDO, result.Status);
    }

    [Fact]
    public async Task UploadAsync_ReferenciaInexistente_NoSubeNada()
    {
        var (service, repository, storage) = Create();

        await Assert.ThrowsAsync<ReferenciaNotFoundException>(() => service.UploadAsync(Upload("a.pdf", Pdf, referenciaId: 99)));

        Assert.Empty(storage.Uploaded);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task UploadAsync_ArchivoNoPermitido_NoSubeNada()
    {
        var (service, repository, storage) = Create();

        await Assert.ThrowsAsync<FileTypeNotAllowedException>(() => service.UploadAsync(Upload("virus.exe", [0x4D, 0x5A, 0x90])));

        Assert.Empty(storage.Uploaded);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task UploadAsync_SiFallaLaBd_BorraElArchivoDelBucket()
    {
        var (service, repository, storage) = Create();
        repository.FailOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(Upload("a.pdf", Pdf)));

        Assert.Empty(storage.Uploaded);   // se subió y luego se borró
        Assert.Single(storage.Deleted);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_DocumentoInexistente_Lanza()
    {
        var (service, _, _) = Create();

        await Assert.ThrowsAsync<DocumentNotFoundException>(() => service.GetDownloadUrlAsync(123));
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<DocumentEntity> Saved { get; } = [];
        public bool FailOnAdd { get; set; }

        public Task<string?> GetReferenciaNumberAsync(int referenciaId, CancellationToken ct = default) =>
            Task.FromResult(referenciaId == ReferenciaId ? "R-000007" : null);

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
    }

    private sealed class FakeCloudStorage : ICloudStorageService
    {
        public Dictionary<string, byte[]> Uploaded { get; } = [];
        public Dictionary<string, string> ContentTypes { get; } = [];
        public List<string> Deleted { get; } = [];

        public async Task<CloudStorageObject> UploadAsync(CloudStorageUploadRequest request, CancellationToken ct = default)
        {
            using var copy = new MemoryStream();
            await request.Content.CopyToAsync(copy, ct);
            Uploaded[request.ObjectPath] = copy.ToArray();
            ContentTypes[request.ObjectPath] = request.ContentType;
            return new CloudStorageObject(request.ObjectPath, copy.Length, request.ContentType);
        }

        public Task<CloudStorageDownloadResult> DownloadAsync(string objectPath, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectPath, CancellationToken ct = default)
        {
            Uploaded.Remove(objectPath);
            Deleted.Add(objectPath);
            return Task.CompletedTask;
        }

        public Task<string> GenerateSignedUrlAsync(string objectPath, TimeSpan? expiresIn = null, CancellationToken ct = default) =>
            Task.FromResult($"https://example.test/{objectPath}");
    }
}