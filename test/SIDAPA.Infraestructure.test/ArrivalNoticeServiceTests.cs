using System.Text;
using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.application.Services.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;
using Xunit;

namespace SIDAPA.Infraestructure.test;

/// <summary>
/// Pruebas de <see cref="ArrivalNoticeService"/> (SCRUM-46 / HU-02) con fakes a mano: no tocan la BD ni el bucket.
/// Usan el <see cref="DocumentFileValidator"/> real para que un .exe o un archivo de más de 20 MB se rechacen de verdad.
/// </summary>
public class ArrivalNoticeServiceTests
{
    private const int ClientId = 4;
    private const int UserId = 9;
    private const string Aduana = "470";
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\ndocumento de prueba\n%%EOF");
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

    private sealed record Sut(
        ArrivalNoticeService Service,
        FakeReferenciaRepository Referencias,
        FakeDocumentService Documents,
        FakeCloudStorage Storage);

    private static Sut Create()
    {
        var referencias = new FakeReferenciaRepository();
        var storage = new FakeCloudStorage();
        var documents = new FakeDocumentService(storage);
        var service = new ArrivalNoticeService(referencias, new FakeCatalogRepository(), new DocumentFileValidator(), documents, storage,
            TimeProvider.System);
        return new Sut(service, referencias, documents, storage);
    }

    private static ArrivalNoticeFileDto File(DocumentTypeEnum type, string name, byte[] bytes, long? size = null) =>
        new(type, name, size ?? bytes.Length, new MemoryStream(bytes));

    private static ArrivalNoticeFileDto Factura() => File(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, "factura.pdf", Pdf);
    private static ArrivalNoticeFileDto Bl() => File(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, "bl.pdf", Pdf);

    private static CreateArrivalNoticeDto Notice(IReadOnlyList<ArrivalNoticeFileDto> files, string? notes = null, string? aduana = Aduana) =>
        new(ClientId, UserId, OperationTypeEnum.IMPORTACION, new DateOnly(2026, 12, 1), aduana, notes, files);

    [Fact]
    public async Task CreateAsync_FacturaYBl_CreaReferenciaRecibidaConNumeroUnico()
    {
        var sut = Create();

        var result = await sut.Service.CreateAsync(Notice([Factura(), Bl()], notes: "  Llega en contenedor  "));

        Assert.Equal("R-000001", result.ReferenciaNumber);
        Assert.Equal(2, result.DocumentCount);

        var referencia = Assert.Single(sut.Referencias.Added);
        Assert.Equal((short)ReferenciaStatusEnum.RECIBIDA, referencia.StatusId);
        Assert.Equal(ClientId, referencia.ClientId);
        Assert.Equal(Aduana, referencia.AduanaCode);
        Assert.Equal("Llega en contenedor", referencia.Notes);
        Assert.Equal(UserId, sut.Referencias.AddedByUserId);

        Assert.Equal(2, sut.Storage.Uploaded.Count);
        Assert.All(sut.Documents.Uploads, u => Assert.Equal(referencia.Id, u.ReferenciaId));
    }

    [Fact]
    public async Task CreateAsync_DosAvisos_NumerosDistintos()
    {
        var sut = Create();

        var first = await sut.Service.CreateAsync(Notice([Factura()]));
        var second = await sut.Service.CreateAsync(Notice([Bl()]));

        Assert.NotEqual(first.ReferenciaNumber, second.ReferenciaNumber);
    }

    [Fact]
    public async Task CreateAsync_ConExe_LoRechazaSinCrearNada()
    {
        var sut = Create();

        await Assert.ThrowsAsync<FileTypeNotAllowedException>(() =>
            sut.Service.CreateAsync(Notice([Factura(), File(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, "setup.exe", Exe)])));

        Assert.Empty(sut.Referencias.Added);
        Assert.Empty(sut.Storage.Uploaded);
    }

    [Fact]
    public async Task CreateAsync_PdfDisfrazadoDeExe_LoRechaza()
    {
        var sut = Create();

        await Assert.ThrowsAsync<FileContentMismatchException>(() =>
            sut.Service.CreateAsync(Notice([File(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, "bl.pdf", Exe)])));

        Assert.Empty(sut.Referencias.Added);
    }

    [Fact]
    public async Task CreateAsync_ArchivoDeMasDe20Mb_LoRechazaSinCrearNada()
    {
        var sut = Create();
        var tooLarge = File(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, "factura.pdf", Pdf,
            size: DocumentFileRule.maxSizeInBytes + 1);

        await Assert.ThrowsAsync<FileTooLargeException>(() => sut.Service.CreateAsync(Notice([tooLarge])));

        Assert.Empty(sut.Referencias.Added);
        Assert.Empty(sut.Storage.Uploaded);
    }

    [Fact]
    public async Task CreateAsync_FallaElSegundoDocumento_BorraElPrimeroYLaReferencia()
    {
        var sut = Create();
        sut.Documents.FailOnUploadNumber = 2;

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Service.CreateAsync(Notice([Factura(), Bl()])));

        Assert.Empty(sut.Storage.Uploaded);              // el primero se subió y se borró
        Assert.Single(sut.Storage.Deleted);
        Assert.Single(sut.Referencias.Deleted);
    }

    [Fact]
    public async Task CreateAsync_SinDocumentos_Lanza()
    {
        var sut = Create();

        await Assert.ThrowsAsync<NoDocumentsException>(() => sut.Service.CreateAsync(Notice([])));
    }

    [Fact]
    public async Task CreateAsync_ObservacionesDeMasDe500_Lanza()
    {
        var sut = Create();

        await Assert.ThrowsAsync<NotesTooLongException>(() =>
            sut.Service.CreateAsync(Notice([Factura()], notes: new string('x', ArrivalNoticeRule.MaxNotesLength + 1))));
    }

    [Fact]
    public async Task CreateAsync_AduanaInexistente_Lanza()
    {
        var sut = Create();

        await Assert.ThrowsAsync<AduanaNotFoundException>(() => sut.Service.CreateAsync(Notice([Factura()], aduana: "999")));
        Assert.Empty(sut.Referencias.Added);
    }

    [Fact]
    public async Task CreateAsync_Exportacion_Lanza()
    {
        var sut = Create();
        var notice = Notice([Factura()]) with { OperationType = OperationTypeEnum.EXPORTACION };

        await Assert.ThrowsAsync<OperationTypeNotAllowedException>(() => sut.Service.CreateAsync(notice));
    }

    [Theory]
    [InlineData(1, "R-000001")]
    [InlineData(123, "R-000123")]
    [InlineData(1234567, "R-1234567")]
    public void ReferenciaNumberRule_Format(long consecutive, string expected) =>
        Assert.Equal(expected, ReferenciaNumberRule.Format(consecutive));

    private sealed class FakeReferenciaRepository : IReferenciaRepository
    {
        private long _sequence;

        public List<ReferenciaEntity> Added { get; } = [];
        public List<int> Deleted { get; } = [];
        public int AddedByUserId { get; private set; }

        public Task<long> NextNumberAsync(CancellationToken ct = default) => Task.FromResult(++_sequence);

        public Task AddAsync(ReferenciaEntity referencia, int userId, CancellationToken ct = default)
        {
            referencia.Id = Added.Count + 1;
            Added.Add(referencia);
            AddedByUserId = userId;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int referenciaId, CancellationToken ct = default)
        {
            Deleted.Add(referenciaId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ClientReferenciaDto>> GetByClientAsync(int clientId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ClientReferenciaDto>>([]);

        public Task<ReferenciaEntity?> GetByIdAsync(int referenciaId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<short>> GetMandatoryDocumentTypeIdsAsync(short operationTypeCode, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ChangeStatusAsync(int referenciaId, ReferenciaStatusEnum from, ReferenciaStatusEnum to,
            int userId, string? comment, DateTime changedAt, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCatalogRepository : ICatalogRepository
    {
        public Task<IReadOnlyList<AduanaOptionDto>> GetActiveAduanasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AduanaOptionDto>>([new AduanaOptionDto(Aduana, "AICM")]);

        public Task<bool> IsActiveAduanaAsync(string aduanaCode, CancellationToken ct = default) =>
            Task.FromResult(aduanaCode == Aduana);

        public Task<IReadOnlyList<DocumentTypeOptionDto>> GetDocumentTypesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DocumentTypeOptionDto>>([]);
    }

    /// <summary>Sube al <see cref="FakeCloudStorage"/>; puede fallar en el N-ésimo archivo.</summary>
    private sealed class FakeDocumentService(FakeCloudStorage storage) : IDocumentService
    {
        public List<UploadDocumentDto> Uploads { get; } = [];
        public int? FailOnUploadNumber { get; set; }

        public async Task<DocumentDto> UploadAsync(UploadDocumentDto upload, CancellationToken ct = default)
        {
            if (Uploads.Count + 1 == FailOnUploadNumber)
                throw new InvalidOperationException("Falla simulada al subir");

            Uploads.Add(upload);
            var path = $"referencias/{upload.ReferenciaId}/{upload.DocumentType}/{Uploads.Count}";
            await storage.UploadAsync(new CloudStorageUploadRequest(upload.Content, path, "application/pdf"), ct);
            return new DocumentDto(Uploads.Count, upload.ReferenciaId, upload.DocumentType, upload.FileName, path,
                upload.SizeInBytes, DocumentStatusEnum.RECIBIDO, upload.UploadedByUserId, DateTime.UtcNow);
        }

        public Task<IReadOnlyList<DocumentDto>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> GetDownloadUrlAsync(int documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCloudStorage : ICloudStorageService
    {
        public HashSet<string> Uploaded { get; } = [];
        public List<string> Deleted { get; } = [];

        public Task<CloudStorageObject> UploadAsync(CloudStorageUploadRequest request, CancellationToken ct = default)
        {
            Uploaded.Add(request.ObjectPath);
            return Task.FromResult(new CloudStorageObject(request.ObjectPath, request.Content.Length, request.ContentType));
        }

        public Task DeleteAsync(string objectPath, CancellationToken ct = default)
        {
            Uploaded.Remove(objectPath);
            Deleted.Add(objectPath);
            return Task.CompletedTask;
        }

        public Task<CloudStorageDownloadResult> DownloadAsync(string objectPath, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> GenerateSignedUrlAsync(string objectPath, TimeSpan? expiresIn = null, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}

/// <summary>Avance y urgencia del panel de pedimentos.</summary>
public class PedimentoUrgencyRuleTests
{
    [Theory]
    [InlineData(ReferenciaStatusEnum.RECIBIDA, 0)]
    [InlineData(ReferenciaStatusEnum.CERRADO, 100)]
    [InlineData(ReferenciaStatusEnum.RECHAZADO_POR_VALIDADOR, 57)]   // retrabajo = avance de EN_GLOSA
    [InlineData(ReferenciaStatusEnum.CON_ANOTACIONES, 57)]
    public void ProgressFor(ReferenciaStatusEnum status, int expected) =>
        Assert.Equal(expected, PedimentoUrgencyRule.ProgressFor(status));

    [Fact]
    public void RechazadoNoAvanzaMasQueGlosado() =>
        Assert.True(PedimentoUrgencyRule.ProgressFor(ReferenciaStatusEnum.RECHAZADO_POR_VALIDADOR)
                    < PedimentoUrgencyRule.ProgressFor(ReferenciaStatusEnum.GLOSADO));
}
