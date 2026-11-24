using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.application.test.Fakes;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.application.test.Services.CaseFile.Expediente;

/// <summary>
/// Pruebas de <see cref="ExpedienteService"/> (SCRUM-56): no tocan la BD.
/// Reemplazan la página manual /dev/expediente ("Solo revisar" = CheckAsync, "Evaluar" = EvaluateAsync).
/// </summary>
public class ExpedienteServiceTests
{
    private const int ReferenciaId = 7;
    private const int ClientId = 4;
    private const int CapturadorId = 2;

    private readonly FakeReferenciaRepository _referencias = new()
    {
        MandatoryDocumentTypeIds =
        [
            (short)DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR,
            (short)DocumentTypeEnum.DOCUMENTO_TRANSPORTE
        ]
    };
    private readonly FakeDocumentRepository _documents = new();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ExpedienteService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public ExpedienteServiceTests() => _service = new ExpedienteService(_referencias, _documents, _notifications);

    private void GivenReferencia(ReferenciaStatusEnum status) => _referencias.Seed(new ReferenciaEntity
    {
        Id = ReferenciaId,
        ReferenciaNumber = "R-000007",
        ClientId = ClientId,
        OperationTypeCode = 1,
        AduanaCode = "430",
        StatusId = (short)status
    });

    private void GivenDocuments(DocumentStatusEnum factura, DocumentStatusEnum bl)
    {
        _documents.Seed(ReferenciaId, DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, factura);
        _documents.Seed(ReferenciaId, DocumentTypeEnum.DOCUMENTO_TRANSPORTE, bl);
    }

    [Fact]
    public async Task EvaluateAsync_TodoCorrectoEnRevision_CambiaEstatusYAvisaAlCliente()
    {
        GivenReferencia(ReferenciaStatusEnum.EN_REVISION);
        GivenDocuments(DocumentStatusEnum.CORRECTO, DocumentStatusEnum.CORRECTO);

        var result = await _service.EvaluateAsync(ReferenciaId, CapturadorId, _ct);

        result.IsComplete.ShouldBeTrue();
        result.StatusChanged.ShouldBeTrue();
        result.Status.ShouldBe(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO);
        _referencias.Changes.ShouldHaveSingleItem()
            .ShouldBe((ReferenciaStatusEnum.EN_REVISION, ReferenciaStatusEnum.EXPEDIENTE_COMPLETO, CapturadorId));
        await _notifications.Received(1).NotifyClientAsync(
            ClientId, Arg.Is<string>(m => m.Contains("R-000007")), ReferenciaId, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_FaltaUnDocumento_NoCambiaNada()
    {
        GivenReferencia(ReferenciaStatusEnum.EN_REVISION);
        GivenDocuments(DocumentStatusEnum.CORRECTO, DocumentStatusEnum.FALTANTE);

        var result = await _service.EvaluateAsync(ReferenciaId, CapturadorId, _ct);

        result.IsComplete.ShouldBeFalse();
        result.MissingDocuments.ShouldHaveSingleItem().ShouldBe(DocumentTypeEnum.DOCUMENTO_TRANSPORTE);
        _referencias.Changes.ShouldBeEmpty();
        await NoClientWasNotified();
    }

    [Theory]
    [InlineData(ReferenciaStatusEnum.RECIBIDA)]
    [InlineData(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO)]   // ya estaba completo: no se repite el aviso
    public async Task EvaluateAsync_SiNoEstaEnRevision_NoCambiaNada(ReferenciaStatusEnum status)
    {
        GivenReferencia(status);
        GivenDocuments(DocumentStatusEnum.CORRECTO, DocumentStatusEnum.CORRECTO);

        var result = await _service.EvaluateAsync(ReferenciaId, CapturadorId, _ct);

        result.IsComplete.ShouldBeTrue();
        result.StatusChanged.ShouldBeFalse();
        result.Status.ShouldBe(status);
        _referencias.Changes.ShouldBeEmpty();
        await NoClientWasNotified();
    }

    [Fact]
    public async Task EvaluateAsync_OtroUsuarioLaCambioAntes_NoAvisaDosVeces()
    {
        GivenReferencia(ReferenciaStatusEnum.EN_REVISION);
        GivenDocuments(DocumentStatusEnum.CORRECTO, DocumentStatusEnum.CORRECTO);
        _referencias.SimulateConcurrentChange = true;

        var result = await _service.EvaluateAsync(ReferenciaId, CapturadorId, _ct);

        result.StatusChanged.ShouldBeFalse();
        await NoClientWasNotified();
    }

    [Fact]
    public async Task CheckAsync_SoloConsulta_NoCambiaEstatusNiAvisa()
    {
        GivenReferencia(ReferenciaStatusEnum.EN_REVISION);
        GivenDocuments(DocumentStatusEnum.CORRECTO, DocumentStatusEnum.CORRECTO);

        var result = await _service.CheckAsync(ReferenciaId, _ct);

        result.IsComplete.ShouldBeTrue();
        result.StatusChanged.ShouldBeFalse();
        result.Status.ShouldBe(ReferenciaStatusEnum.EN_REVISION);
        _referencias.Changes.ShouldBeEmpty();
        await NoClientWasNotified();
    }

    [Fact]
    public async Task CheckAsync_ReportaLosObligatoriosYLosFaltantes()
    {
        GivenReferencia(ReferenciaStatusEnum.EN_REVISION);
        GivenDocuments(DocumentStatusEnum.RECIBIDO, DocumentStatusEnum.CORRECTO);

        var result = await _service.CheckAsync(ReferenciaId, _ct);

        result.MandatoryDocuments.ShouldBe(
            [DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentTypeEnum.DOCUMENTO_TRANSPORTE]);
        result.MissingDocuments.ShouldHaveSingleItem().ShouldBe(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR);
    }

    [Fact]
    public async Task EvaluateAsync_ReferenciaInexistente_Lanza()
    {
        await Should.ThrowAsync<ReferenciaNotFoundException>(() => _service.EvaluateAsync(99, CapturadorId, _ct));
    }

    [Fact]
    public async Task CheckAsync_ReferenciaInexistente_Lanza()
    {
        await Should.ThrowAsync<ReferenciaNotFoundException>(() => _service.CheckAsync(99, _ct));
    }

    private Task NoClientWasNotified() =>
        _notifications.DidNotReceiveWithAnyArgs().NotifyClientAsync(default, default!, default, default, default);
}
