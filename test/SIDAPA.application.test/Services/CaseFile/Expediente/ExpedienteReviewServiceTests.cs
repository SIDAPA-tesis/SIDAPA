using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Expediente;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.application.Services.CaseFile.Expediente;
using SIDAPA.application.Services.Core;
using SIDAPA.application.test.Fakes;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.CaseFile;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;
using SIDAPA.domain.Exceptions.CoreException;
using static SIDAPA.domain.Enums.Core.DocumentStatusEnum;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;
using static SIDAPA.domain.Enums.Core.ReferenciaStatusEnum;

namespace SIDAPA.application.test.Services.CaseFile.Expediente;

/// <summary>
/// <see cref="ExpedienteReviewService"/> (SCRUM-55 · HU-03) con un <see cref="ExpedienteService"/> real sobre las
/// fakes: así se prueba el recorrido completo Recibida → En revisión → Expediente completo.
/// </summary>
public class ExpedienteReviewServiceTests
{
    private const int ReferenciaId = 7;
    private const int OtraReferenciaId = 8;
    private const int ClientId = 4;
    private const int CapturadorId = 2;

    private readonly FakeReferenciaRepository _referencias = new()
    {
        MandatoryDocumentTypeIds = [(short)FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, (short)DOCUMENTO_TRANSPORTE]
    };
    private readonly FakeDocumentRepository _documents = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly IExpedienteRepository _expedientes = Substitute.For<IExpedienteRepository>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ICloudStorageService _storage = Substitute.For<ICloudStorageService>();
    private readonly ISpreadsheetPreviewReader _spreadsheets = Substitute.For<ISpreadsheetPreviewReader>();
    private readonly ExpedienteReviewService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public ExpedienteReviewServiceTests() => _service = new ExpedienteReviewService(
        _expedientes, _referencias, _documents, _unitOfWork,
        new ExpedienteService(_referencias, _documents, _notifications),
        _storage, _spreadsheets, new FixedTimeProvider());

    private void GivenReferencia(ReferenciaStatusEnum status, int id = ReferenciaId) => _referencias.Seed(new ReferenciaEntity
    {
        Id = id,
        ReferenciaNumber = $"R-00000{id}",
        ClientId = ClientId,
        OperationTypeCode = 1,
        AduanaCode = "470",
        StatusId = (short)status
    });

    private Task<ExpedienteStatusDto> Mark(DocumentEntity document, DocumentStatusEnum status) =>
        _service.MarkAsync(new ReviewDocumentDto(ReferenciaId, document.Id, status, CapturadorId), _ct);

    // ------------------------------------------------------------------ MarkAsync

    [Fact]
    public async Task MarkAsync_PrimerDocumentoDeUnaReferenciaRecibida_LaPasaAEnRevisionYGuardaLaMarca()
    {
        GivenReferencia(RECIBIDA);
        var factura = _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, RECIBIDO);

        var result = await Mark(factura, ERROR);

        _referencias.Changes.ShouldHaveSingleItem().ShouldBe((RECIBIDA, EN_REVISION, CapturadorId));
        factura.Status.ShouldBe(nameof(ERROR));
        result.Status.ShouldBe(EN_REVISION);
        result.StatusChanged.ShouldBeFalse();
        _unitOfWork.Committed.ShouldBe(1);
        _referencias.Locked.ShouldContain(ReferenciaId);
    }

    [Fact]
    public async Task MarkAsync_UltimoObligatorioCorrecto_PasaAExpedienteCompletoYAvisaAlCliente()
    {
        GivenReferencia(EN_REVISION);
        _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO);
        var bl = _documents.Seed(ReferenciaId, DOCUMENTO_TRANSPORTE, RECIBIDO);

        var result = await Mark(bl, CORRECTO);

        result.StatusChanged.ShouldBeTrue();
        result.Status.ShouldBe(EXPEDIENTE_COMPLETO);
        _referencias.Changes.ShouldHaveSingleItem().ShouldBe((EN_REVISION, EXPEDIENTE_COMPLETO, CapturadorId));
        await _notifications.Received(1).NotifyClientAsync(ClientId, Arg.Is<string>(m => m.Contains("R-000007")),
            ReferenciaId, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsync_TodosCorrectosEnUnaReferenciaRecibida_RecorreAmbosCambios()
    {
        GivenReferencia(RECIBIDA);
        _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO);
        var bl = _documents.Seed(ReferenciaId, DOCUMENTO_TRANSPORTE, RECIBIDO);

        var result = await Mark(bl, CORRECTO);

        result.Status.ShouldBe(EXPEDIENTE_COMPLETO);
        _referencias.Changes.Select(c => c.To).ShouldBe([EN_REVISION, EXPEDIENTE_COMPLETO]);
    }

    [Fact]
    public async Task MarkAsync_ObligatorioMarcadoComoFaltante_NoCompletaElExpediente()
    {
        GivenReferencia(EN_REVISION);
        _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO);
        var bl = _documents.Seed(ReferenciaId, DOCUMENTO_TRANSPORTE, RECIBIDO);

        var result = await Mark(bl, FALTANTE);

        result.StatusChanged.ShouldBeFalse();
        result.MissingDocuments.ShouldBe([DOCUMENTO_TRANSPORTE]);
        _referencias.Changes.ShouldBeEmpty();
        await _notifications.DidNotReceiveWithAnyArgs().NotifyClientAsync(default, default!);
    }

    [Fact]
    public async Task MarkAsync_ComoRecibido_SeRechazaSinAbrirTransaccion()
    {
        GivenReferencia(EN_REVISION);
        var factura = _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO);

        await Should.ThrowAsync<InvalidDocumentReviewStatusException>(() => Mark(factura, RECIBIDO));

        factura.Status.ShouldBe(nameof(CORRECTO));
        _unitOfWork.Begun.ShouldBe(0);
    }

    [Theory]
    [InlineData(EN_ESPERA_DE_DOCUMENTOS)]
    [InlineData(EXPEDIENTE_COMPLETO)]
    public async Task MarkAsync_ReferenciaFueraDeRevision_SeRechazaSinCambiarNada(ReferenciaStatusEnum status)
    {
        GivenReferencia(status);
        var factura = _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO);

        await Should.ThrowAsync<ExpedienteNotInReviewException>(() => Mark(factura, ERROR));

        factura.Status.ShouldBe(nameof(CORRECTO));
        _referencias.Changes.ShouldBeEmpty();
        _unitOfWork.RolledBack.ShouldBe(1);
    }

    [Fact]
    public async Task MarkAsync_DocumentoDeOtraReferencia_LanzaNoEncontradoYDeshaceLaTransaccion()
    {
        GivenReferencia(EN_REVISION);
        GivenReferencia(EN_REVISION, OtraReferenciaId);
        var ajeno = _documents.Seed(OtraReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, RECIBIDO);

        await Should.ThrowAsync<DocumentNotFoundException>(() => Mark(ajeno, CORRECTO));

        ajeno.Status.ShouldBe(nameof(RECIBIDO));
        _unitOfWork.Committed.ShouldBe(0);
        _unitOfWork.RolledBack.ShouldBe(1);
    }

    [Fact]
    public async Task MarkAsync_ReferenciaQueNoExiste_LanzaNoEncontrada()
    {
        await Should.ThrowAsync<ReferenciaNotFoundException>(() =>
            _service.MarkAsync(new ReviewDocumentDto(ReferenciaId, 1, CORRECTO, CapturadorId), _ct));
    }

    // ------------------------------------------------------------------ GetAsync

    [Fact]
    public async Task GetAsync_ArmaLaListaDeVerificacionContraLosDocumentosRecibidos()
    {
        _expedientes.GetDetailAsync(ReferenciaId, Arg.Any<CancellationToken>()).Returns(Detail(EN_REVISION,
            [Document(1, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO), Document(2, DOCUMENTO_TRANSPORTE, ERROR)],
            [Required(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, true), Required(DOCUMENTO_TRANSPORTE, true), Required(LISTA_EMPAQUE, false)]));

        var review = (await _service.GetAsync(ReferenciaId, _ct)).ShouldNotBeNull();

        review.CanBeReviewed.ShouldBeTrue();
        review.Checklist.Select(c => (c.Type, c.Status)).ShouldBe(
            [(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO), (DOCUMENTO_TRANSPORTE, ERROR), (LISTA_EMPAQUE, FALTANTE)]);
    }

    [Fact]
    public async Task GetAsync_ExpedienteCompleto_YaNoSePuedeRevisar()
    {
        _expedientes.GetDetailAsync(ReferenciaId, Arg.Any<CancellationToken>()).Returns(Detail(EXPEDIENTE_COMPLETO, [], []));

        var review = (await _service.GetAsync(ReferenciaId, _ct)).ShouldNotBeNull();

        review.CanBeReviewed.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_ReferenciaQueNoExiste_RegresaNull() =>
        (await _service.GetAsync(ReferenciaId, _ct)).ShouldBeNull();

    // ------------------------------------------------------------------ Vista previa

    [Fact]
    public async Task GetPreviewAsync_Pdf_NoBajaElArchivo()
    {
        var pdf = _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, RECIBIDO, "factura.pdf");

        var preview = (await _service.GetPreviewAsync(pdf.Id, _ct)).ShouldNotBeNull();

        preview.Kind.ShouldBe(DocumentPreviewKindEnum.Pdf);
        preview.Spreadsheet.ShouldBeNull();
        preview.ReferenciaId.ShouldBe(ReferenciaId);
        await _storage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default);
    }

    [Fact]
    public async Task GetPreviewAsync_Excel_LoBajaYLoConvierteEnTabla()
    {
        var xlsx = _documents.Seed(ReferenciaId, LISTA_EMPAQUE, RECIBIDO, "lista.xlsx");
        var content = new MemoryStream([1, 2, 3]);
        _storage.DownloadAsync(xlsx.BucketPath, Arg.Any<CancellationToken>())
            .Returns(new CloudStorageDownloadResult(content, "application/octet-stream", 3));
        var table = new SpreadsheetPreviewDto([new SheetPreviewDto("Hoja1", [["Caja", "10"]])], Truncated: false);
        _spreadsheets.Read(content).Returns(table);

        var preview = (await _service.GetPreviewAsync(xlsx.Id, _ct)).ShouldNotBeNull();

        preview.Kind.ShouldBe(DocumentPreviewKindEnum.Spreadsheet);
        preview.Spreadsheet.ShouldBe(table);
        content.CanRead.ShouldBeFalse();   // el stream descargado se libera
    }

    [Fact]
    public async Task OpenFileAsync_Excel_NoSeSirveParaElIframe()
    {
        var xlsx = _documents.Seed(ReferenciaId, LISTA_EMPAQUE, RECIBIDO, "lista.xlsx");

        (await _service.OpenFileAsync(xlsx.Id, _ct)).ShouldBeNull();
        await _storage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default);
    }

    [Fact]
    public async Task OpenFileAsync_Xml_RegresaElContenidoComoTexto()
    {
        var xml = _documents.Seed(ReferenciaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, RECIBIDO, "cfdi.xml");
        var content = new MemoryStream("<cfdi/>"u8.ToArray());
        _storage.DownloadAsync(xml.BucketPath, Arg.Any<CancellationToken>())
            .Returns(new CloudStorageDownloadResult(content, "application/xml", 7));

        var file = (await _service.OpenFileAsync(xml.Id, _ct)).ShouldNotBeNull();

        file.Kind.ShouldBe(DocumentPreviewKindEnum.Text);
        file.Content.ShouldBeSameAs(content);
        file.FileName.ShouldBe("cfdi.xml");
    }

    // ------------------------------------------------------------------ Datos

    private static ExpedienteDetailDto Detail(ReferenciaStatusEnum status,
        IReadOnlyList<ExpedienteDocumentDto> documents, IReadOnlyList<RequiredDocumentDto> required) => new(
        ReferenciaId, "R-000007", "Importadora Norte", "AICM", "Importación", status, status.ToString(),
        new DateOnly(2026, 11, 30), null, new DateTime(2026, 11, 23, 10, 0, 0), documents, required);

    private static ExpedienteDocumentDto Document(int id, DocumentTypeEnum type, DocumentStatusEnum status) =>
        new(id, type, type.ToString(), $"{type}.pdf", 1024, status, "Cliente demo", new DateTime(2026, 11, 23, 10, 5, 0));

    private static RequiredDocumentDto Required(DocumentTypeEnum type, bool mandatory) => new(type, type.ToString(), mandatory);
}
