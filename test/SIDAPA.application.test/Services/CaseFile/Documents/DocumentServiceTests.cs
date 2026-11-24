using System.Text;
using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.Services.Core;
using SIDAPA.application.test.Fakes;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.application.test.Services.CaseFile.Documents;

/// <summary>
/// Pruebas de <see cref="DocumentService"/> (SCRUM-48): no tocan la BD ni el bucket.
/// Usan el <see cref="DocumentFileValidator"/> real de SCRUM-50. Reemplazan la página manual /dev/documentos.
/// </summary>
public class DocumentServiceTests
{
    private const int ReferenciaId = 7;
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\nfactura de prueba\n%%EOF");

    private readonly FakeDocumentRepository _repository = new();
    private readonly FakeCloudStorage _storage = new();
    private readonly DocumentService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public DocumentServiceTests()
    {
        _repository.ReferenciaNumbers[ReferenciaId] = "R-000007";
        _service = new DocumentService(_repository, new DocumentFileValidator(), _storage);
    }

    private static UploadDocumentDto Upload(string fileName, byte[] bytes, int referenciaId = ReferenciaId,
        DocumentTypeEnum type = DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR) =>
        new(referenciaId, type, fileName, bytes.Length, new MemoryStream(bytes), UploadedByUserId: 3);

    [Fact]
    public async Task UploadAsync_SubeElArchivoCompletoYLoRegistra()
    {
        var result = await _service.UploadAsync(Upload("Factura 001.pdf", Pdf), _ct);

        var (path, bytes) = _storage.Uploaded.ShouldHaveSingleItem();
        path.ShouldStartWith("referencias/R-000007/FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR/");
        path.ShouldEndWith(".pdf");
        bytes.ShouldBe(Pdf);   // completo: el validador no se "come" los primeros bytes
        _storage.ContentTypes[path].ShouldBe("application/pdf");

        var saved = _repository.Saved.ShouldHaveSingleItem();
        saved.BucketPath.ShouldBe(path);
        saved.FileName.ShouldBe("Factura 001.pdf");
        saved.Status.ShouldBe(nameof(DocumentStatusEnum.RECIBIDO));
        saved.UploadedByUserId.ShouldBe(3);
        result.Status.ShouldBe(DocumentStatusEnum.RECIBIDO);
    }

    [Fact]
    public async Task UploadAsync_ReferenciaInexistente_NoSubeNada()
    {
        await Should.ThrowAsync<ReferenciaNotFoundException>(() => _service.UploadAsync(Upload("a.pdf", Pdf, referenciaId: 99), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        _repository.Saved.ShouldBeEmpty();
    }

    [Fact]
    public async Task UploadAsync_ArchivoNoPermitido_NoSubeNada()
    {
        await Should.ThrowAsync<FileTypeNotAllowedException>(() => _service.UploadAsync(Upload("virus.exe", [0x4D, 0x5A, 0x90]), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        _repository.Saved.ShouldBeEmpty();
    }

    [Fact]
    public async Task UploadAsync_TipoDeDocumentoInexistente_Lanza()
    {
        await Should.ThrowAsync<InvalidDocumentTypeException>(() =>
            _service.UploadAsync(Upload("a.pdf", Pdf, type: (DocumentTypeEnum)999), _ct));

        _storage.Uploaded.ShouldBeEmpty();
    }

    [Fact]
    public async Task UploadAsync_StreamSinSeek_Lanza()
    {
        var upload = Upload("a.pdf", Pdf) with { Content = new NonSeekableStream(Pdf) };

        await Should.ThrowAsync<ArgumentException>(() => _service.UploadAsync(upload, _ct));
    }

    [Fact]
    public async Task UploadAsync_SiFallaLaBd_BorraElArchivoDelBucket()
    {
        _repository.FailOnAdd = true;

        await Should.ThrowAsync<InvalidOperationException>(() => _service.UploadAsync(Upload("a.pdf", Pdf), _ct));

        _storage.Uploaded.ShouldBeEmpty();   // se subió y luego se borró
        _storage.Deleted.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetByReferenciaAsync_RegresaSoloLosDeEsaReferencia()
    {
        _repository.Seed(ReferenciaId, DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentStatusEnum.CORRECTO, "factura.pdf");
        _repository.Seed(ReferenciaId, DocumentTypeEnum.DOCUMENTO_TRANSPORTE, DocumentStatusEnum.ERROR, "bl.pdf");
        _repository.Seed(referenciaId: 8, DocumentTypeEnum.LISTA_EMPAQUE, DocumentStatusEnum.RECIBIDO);

        var documents = await _service.GetByReferenciaAsync(ReferenciaId, _ct);

        documents.Select(d => d.FileName).ShouldBe(["factura.pdf", "bl.pdf"]);
        documents.Select(d => d.Status).ShouldBe([DocumentStatusEnum.CORRECTO, DocumentStatusEnum.ERROR]);
    }

    [Fact]
    public async Task GetByReferenciaAsync_SinDocumentos_RegresaListaVacia()
    {
        (await _service.GetByReferenciaAsync(ReferenciaId, _ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDownloadUrlAsync_FirmaLaRutaGuardadaConLaExpiracionPorDefecto()
    {
        var document = _repository.Seed(ReferenciaId, DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentStatusEnum.RECIBIDO);

        var url = await _service.GetDownloadUrlAsync(document.Id, _ct);

        url.ShouldContain(document.BucketPath);
        _storage.SignedUrls.ShouldHaveSingleItem().ShouldBe((document.BucketPath, (TimeSpan?)null));
    }

    [Fact]
    public async Task GetDownloadUrlAsync_DocumentoInexistente_Lanza()
    {
        await Should.ThrowAsync<DocumentNotFoundException>(() => _service.GetDownloadUrlAsync(123, _ct));
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }
}
