using System.Text;
using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.application.test.Fakes;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.application.test.Services.CaseFile.ArrivalNotice;

/// <summary>
/// Pruebas de <see cref="ArrivalNoticeService"/> (SCRUM-46 / HU-02): no tocan la BD ni el bucket.
/// Usan el <see cref="DocumentFileValidator"/> real para que un .exe o un archivo de más de 20 MB se rechacen de verdad.
/// </summary>
public class ArrivalNoticeServiceTests
{
    private const int ClientId = 4;
    private const int UserId = 9;
    private const string Aduana = "470";
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\ndocumento de prueba\n%%EOF");
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

    private readonly FakeReferenciaRepository _referencias = new();
    private readonly FakeCloudStorage _storage = new();
    private readonly FakeDocumentService _documents;
    private readonly ICatalogRepository _catalogs = Substitute.For<ICatalogRepository>();
    private readonly FixedTimeProvider _clock = new();
    private readonly ArrivalNoticeService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public ArrivalNoticeServiceTests()
    {
        _documents = new FakeDocumentService(_storage);
        _catalogs.IsActiveAduanaAsync(Aduana, Arg.Any<CancellationToken>()).Returns(true);
        _service = new ArrivalNoticeService(_referencias, _catalogs, new DocumentFileValidator(), _documents, _storage, _clock);
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
        var result = await _service.CreateAsync(Notice([Factura(), Bl()], notes: "  Llega en contenedor  "), _ct);

        result.ReferenciaNumber.ShouldBe("R-000001");
        result.DocumentCount.ShouldBe(2);

        var referencia = _referencias.Added.ShouldHaveSingleItem();
        referencia.StatusId.ShouldBe((short)ReferenciaStatusEnum.RECIBIDA);
        referencia.ClientId.ShouldBe(ClientId);
        referencia.AduanaCode.ShouldBe(Aduana);
        referencia.Notes.ShouldBe("Llega en contenedor");
        _referencias.AddedByUserId.ShouldBe(UserId);

        _storage.Uploaded.Count.ShouldBe(2);
        _documents.Uploads.ShouldAllBe(u => u.ReferenciaId == referencia.Id);
    }

    [Fact]
    public async Task CreateAsync_GuardaLaFechaDelRelojEnUtcSinZona()
    {
        await _service.CreateAsync(Notice([Factura()]), _ct);

        var createdAt = _referencias.Added.ShouldHaveSingleItem().CreatedAt;
        createdAt.ShouldBe(FixedTimeProvider.Default.UtcDateTime);
        createdAt.Kind.ShouldBe(DateTimeKind.Unspecified);   // la columna es "timestamp without time zone"
    }

    [Fact]
    public async Task CreateAsync_ObservacionesEnBlanco_SeGuardanComoNull()
    {
        await _service.CreateAsync(Notice([Factura()], notes: "   "), _ct);

        _referencias.Added.ShouldHaveSingleItem().Notes.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_DosAvisos_NumerosDistintos()
    {
        var first = await _service.CreateAsync(Notice([Factura()]), _ct);
        var second = await _service.CreateAsync(Notice([Bl()]), _ct);

        first.ReferenciaNumber.ShouldNotBe(second.ReferenciaNumber);
    }

    [Fact]
    public async Task CreateAsync_SubeCadaArchivoCompleto()
    {
        await _service.CreateAsync(Notice([Factura()]), _ct);

        // El validador lee el encabezado; el servicio debe regresar el Stream al inicio antes de subir.
        _storage.Uploaded.Values.ShouldHaveSingleItem().ShouldBe(Pdf);
    }

    [Fact]
    public async Task CreateAsync_ConExe_LoRechazaSinCrearNada()
    {
        await Should.ThrowAsync<FileTypeNotAllowedException>(() =>
            _service.CreateAsync(Notice([Factura(), File(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, "setup.exe", Exe)]), _ct));

        _referencias.Added.ShouldBeEmpty();
        _storage.Uploaded.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ExeDisfrazadoDePdf_LoRechaza()
    {
        await Should.ThrowAsync<FileContentMismatchException>(() =>
            _service.CreateAsync(Notice([File(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, "bl.pdf", Exe)]), _ct));

        _referencias.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ArchivoDeMasDe20Mb_LoRechazaSinCrearNada()
    {
        var tooLarge = File(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, "factura.pdf", Pdf,
            size: DocumentFileRule.maxSizeInBytes + 1);

        await Should.ThrowAsync<FileTooLargeException>(() => _service.CreateAsync(Notice([tooLarge]), _ct));

        _referencias.Added.ShouldBeEmpty();
        _storage.Uploaded.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_TipoDeDocumentoInexistente_Lanza()
    {
        var unknown = File((DocumentTypeEnum)999, "factura.pdf", Pdf);

        await Should.ThrowAsync<InvalidDocumentTypeException>(() => _service.CreateAsync(Notice([unknown]), _ct));
        _referencias.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_FallaElSegundoDocumento_BorraElPrimeroYLaReferencia()
    {
        _documents.FailOnUploadNumber = 2;

        await Should.ThrowAsync<InvalidOperationException>(() => _service.CreateAsync(Notice([Factura(), Bl()]), _ct));

        _storage.Uploaded.ShouldBeEmpty();              // el primero se subió y se borró
        _storage.Deleted.ShouldHaveSingleItem();
        _referencias.Deleted.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreateAsync_SinDocumentos_Lanza()
    {
        await Should.ThrowAsync<NoDocumentsException>(() => _service.CreateAsync(Notice([]), _ct));
    }

    [Fact]
    public async Task CreateAsync_ObservacionesDeMasDe500_Lanza()
    {
        await Should.ThrowAsync<NotesTooLongException>(() =>
            _service.CreateAsync(Notice([Factura()], notes: new string('x', ArrivalNoticeRule.MaxNotesLength + 1)), _ct));
    }

    [Theory]
    [InlineData("999")]   // no existe en el catálogo
    [InlineData("")]      // vacía
    [InlineData(null)]
    public async Task CreateAsync_AduanaInvalida_Lanza(string? aduana)
    {
        await Should.ThrowAsync<AduanaNotFoundException>(() => _service.CreateAsync(Notice([Factura()], aduana: aduana), _ct));
        _referencias.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Exportacion_Lanza()
    {
        var notice = Notice([Factura()]) with { OperationType = OperationTypeEnum.EXPORTACION };

        await Should.ThrowAsync<OperationTypeNotAllowedException>(() => _service.CreateAsync(notice, _ct));
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
                upload.SizeInBytes, DocumentStatusEnum.RECIBIDO, upload.UploadedByUserId, FixedTimeProvider.Default.UtcDateTime);
        }

        public Task<IReadOnlyList<DocumentDto>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> GetDownloadUrlAsync(int documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
