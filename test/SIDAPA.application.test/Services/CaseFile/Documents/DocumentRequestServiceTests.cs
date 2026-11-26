using System.Text;
using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.CaseFile.Documents;
using SIDAPA.application.Services.Core;
using SIDAPA.application.test.Fakes;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;
using SIDAPA.domain.Exceptions.CoreException;
using static SIDAPA.domain.Enums.Core.ReferenciaStatusEnum;

namespace SIDAPA.application.test.Services.CaseFile.Documents;

/// <summary>
/// Pruebas de <see cref="DocumentRequestService"/> (SCRUM-64 · HU-05 "Aviso de regreso al Capturador y cambio de
/// estatus"). Usan el <see cref="DocumentService"/> y el validador reales sobre fakes de BD y bucket. El candado y el
/// rollback reales los prueban las pruebas de infraestructura con PostgreSQL.
/// </summary>
public class DocumentRequestServiceTests
{
    private const int ReferenciaId = 7;
    private const string ReferenciaNumber = "R-000007";
    private const int ClientId = 10;
    private const int OtherClientId = 11;
    private const int ClientUserId = 21;
    private const int CapturadorId = 3;
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\nB/L legible\n%%EOF");
    private static readonly DateTime Now = DateTime.SpecifyKind(FixedTimeProvider.Default.UtcDateTime, DateTimeKind.Unspecified);

    private readonly FakeReferenciaRepository _referencias = new();
    private readonly FakeDocumentRepository _documents = new();
    private readonly FakeCloudStorage _storage = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDocumentRequestRepository _requests;
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ReferenciaEntity _referencia;
    private readonly DocumentRequestService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public DocumentRequestServiceTests()
    {
        _referencia = _referencias.Seed(new ReferenciaEntity
        {
            Id = ReferenciaId,
            ReferenciaNumber = ReferenciaNumber,
            ClientId = ClientId,
            StatusId = (short)EN_ESPERA_DE_DOCUMENTOS
        });
        _documents.ReferenciaNumbers[ReferenciaId] = ReferenciaNumber;
        _requests = new FakeDocumentRequestRepository(_referencias);

        var documentService = new DocumentService(_documents, new DocumentFileValidator(), _storage);
        _service = new DocumentRequestService(
            _requests, _referencias, _documents, documentService, _unitOfWork, _notifications, new FixedTimeProvider());
    }

    private DocumentRequestEntity Request(DocumentTypeEnum type = DocumentTypeEnum.DOCUMENTO_TRANSPORTE) =>
        _requests.Seed(ReferenciaId, type, CapturadorId, note: "El B/L no es legible");

    private static AttendDocumentRequestDto Attend(int requestId, int clientId = ClientId, string fileName = "bl.pdf", byte[]? bytes = null)
    {
        bytes ??= Pdf;
        return new AttendDocumentRequestDto(requestId, clientId, ClientUserId, fileName, bytes.Length, new MemoryStream(bytes));
    }

    private Task CapturadorNotified(string containing) =>
        _notifications.Received(1).NotifyUserAsync(
            CapturadorId, Arg.Is<string>(m => m.Contains(containing)), ReferenciaId, Arg.Any<bool>(), Arg.Any<CancellationToken>());

    // ------------------------------------------------------------------ camino feliz

    [Fact]
    public async Task AttendAsync_UltimaSolicitudAbierta_RegresaAEnRevisionYAvisaAlCapturador()
    {
        var request = Request();

        var result = await _service.AttendAsync(Attend(request.Id), _ct);

        result.ShouldBe(new DocumentRequestAttendedDto(
            ReferenciaNumber, FakeDocumentRequestRepository.NameOf(DocumentTypeEnum.DOCUMENTO_TRANSPORTE), 0, ReturnedToReview: true));
        _referencias.Changes.ShouldHaveSingleItem().ShouldBe((EN_ESPERA_DE_DOCUMENTOS, EN_REVISION, ClientUserId));
        _unitOfWork.Committed.ShouldBe(1);
        await CapturadorNotified("regresó a En revisión");
    }

    [Fact]
    public async Task AttendAsync_SubeElArchivo_LoAgregaAlExpedienteYCierraLaSolicitud()
    {
        var request = Request();

        await _service.AttendAsync(Attend(request.Id), _ct);

        var (path, bytes) = _storage.Uploaded.ShouldHaveSingleItem();
        bytes.ShouldBe(Pdf);
        var saved = _documents.Saved.ShouldHaveSingleItem();
        saved.ReferenciaId.ShouldBe(ReferenciaId);
        saved.BucketPath.ShouldBe(path);
        saved.Status.ShouldBe(nameof(DocumentStatusEnum.RECIBIDO));
        saved.UploadedByUserId.ShouldBe(ClientUserId);
        request.ResponseDocumentId.ShouldBe(saved.Id);
        request.AttendedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task AttendAsync_ElTipoDeDocumentoSaleDeLaSolicitud()
    {
        // el cliente no elige el tipo: aunque el archivo se llame "factura", se guarda como lo que se pidió
        var request = Request(DocumentTypeEnum.LISTA_EMPAQUE);

        await _service.AttendAsync(Attend(request.Id, fileName: "factura.pdf"), _ct);

        _documents.Saved.ShouldHaveSingleItem().DocumentTypeId.ShouldBe((short)DocumentTypeEnum.LISTA_EMPAQUE);
        _storage.Uploaded.Keys.ShouldHaveSingleItem().ShouldContain(nameof(DocumentTypeEnum.LISTA_EMPAQUE));
    }

    [Fact]
    public async Task AttendAsync_QuedaOtraSolicitudAbierta_SigueEnEsperaYAvisaCuantasFaltan()
    {
        var first = Request(DocumentTypeEnum.DOCUMENTO_TRANSPORTE);
        Request(DocumentTypeEnum.LISTA_EMPAQUE);

        var result = await _service.AttendAsync(Attend(first.Id), _ct);

        result.Remaining.ShouldBe(1);
        result.ReturnedToReview.ShouldBeFalse();
        _referencias.Changes.ShouldBeEmpty();
        ((ReferenciaStatusEnum)_referencia.StatusId).ShouldBe(EN_ESPERA_DE_DOCUMENTOS);
        _unitOfWork.Committed.ShouldBe(1);
        await CapturadorNotified("Falta 1 documento");
    }

    [Fact]
    public async Task AttendAsync_DosSolicitudesUnaTrasOtra_SoloLaSegundaRegresaAEnRevision()
    {
        var first = Request(DocumentTypeEnum.DOCUMENTO_TRANSPORTE);
        var second = Request(DocumentTypeEnum.LISTA_EMPAQUE);

        await _service.AttendAsync(Attend(first.Id), _ct);
        var result = await _service.AttendAsync(Attend(second.Id), _ct);

        result.ReturnedToReview.ShouldBeTrue();
        _referencias.Changes.ShouldHaveSingleItem();
        _documents.Saved.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AttendAsync_BloqueaLaReferenciaDentroDeLaTransaccion()
    {
        // sin el candado, dos subidas paralelas dejan la referencia en espera para siempre
        var request = Request();

        await _service.AttendAsync(Attend(request.Id), _ct);

        _referencias.Locked.ShouldBe([ReferenciaId]);
        _unitOfWork.Begun.ShouldBe(1);
    }

    // ------------------------------------------------------------------ seguridad

    [Fact]
    public async Task AttendAsync_SolicitudDeOtroCliente_NoLaEncuentraYNoSubeNada()
    {
        var request = Request();

        await Should.ThrowAsync<DocumentRequestNotFoundException>(() => _service.AttendAsync(Attend(request.Id, OtherClientId), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        request.AttendedAt.ShouldBeNull();
        _unitOfWork.Begun.ShouldBe(0);
    }

    [Fact]
    public async Task AttendAsync_SolicitudInexistente_NoLaEncuentra()
    {
        await Should.ThrowAsync<DocumentRequestNotFoundException>(() => _service.AttendAsync(Attend(requestId: 999), _ct));

        _storage.Uploaded.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ estado y doble envío

    [Fact]
    public async Task AttendAsync_SolicitudYaAtendida_LanzaSinSubirNada()
    {
        var request = _requests.Seed(ReferenciaId, DocumentTypeEnum.DOCUMENTO_TRANSPORTE, CapturadorId, attendedAt: Now);

        await Should.ThrowAsync<DocumentRequestAlreadyAttendedException>(() => _service.AttendAsync(Attend(request.Id), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        _documents.Saved.ShouldBeEmpty();
    }

    [Fact]
    public async Task AttendAsync_OtroEnvioLaCerroPrimero_DeshaceYBorraElArchivo()
    {
        // doble clic: los dos envíos pasan la primera revisión, pero el UPDATE condicional solo deja ganar a uno
        var request = Request();
        _requests.SimulateAlreadyClosed = true;

        await Should.ThrowAsync<DocumentRequestAlreadyAttendedException>(() => _service.AttendAsync(Attend(request.Id), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        _storage.Deleted.ShouldHaveSingleItem();
        _unitOfWork.Committed.ShouldBe(0);
        _unitOfWork.RolledBack.ShouldBe(1);
        _referencias.Changes.ShouldBeEmpty();
        _notifications.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(EN_REVISION)]
    [InlineData(RECIBIDA)]
    [InlineData(EXPEDIENTE_COMPLETO)]
    public async Task AttendAsync_ReferenciaYaNoEstaEnEspera_LanzaSinSubirNada(ReferenciaStatusEnum status)
    {
        var request = Request();
        _referencia.StatusId = (short)status;

        await Should.ThrowAsync<ReferenciaNotAwaitingDocumentsException>(() => _service.AttendAsync(Attend(request.Id), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        request.AttendedAt.ShouldBeNull();
    }

    [Fact]
    public async Task AttendAsync_OtroUsuarioCambioElEstatusAlMismoTiempo_ConfirmaSinDecirQueRegreso()
    {
        var request = Request();
        _referencias.SimulateConcurrentChange = true;

        var result = await _service.AttendAsync(Attend(request.Id), _ct);

        result.ReturnedToReview.ShouldBeFalse();
        _unitOfWork.Committed.ShouldBe(1);   // el documento y el cierre sí se guardan
        await CapturadorNotified("Ya no hay documentos pendientes.");
        await _notifications.DidNotReceive().NotifyUserAsync(
            Arg.Any<int>(), Arg.Is<string>(m => m.Contains("regresó")), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ atomicidad

    [Fact]
    public async Task AttendAsync_ArchivoNoPermitido_NoSubeNadaNiAbreTransaccion()
    {
        var request = Request();

        await Should.ThrowAsync<FileTypeNotAllowedException>(() =>
            _service.AttendAsync(Attend(request.Id, fileName: "virus.exe", bytes: [0x4D, 0x5A, 0x90]), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        request.AttendedAt.ShouldBeNull();
        _unitOfWork.Begun.ShouldBe(0);
    }

    [Fact]
    public async Task AttendAsync_FallaLaBdAlGuardarElDocumento_BorraElArchivoYNoAvisa()
    {
        var request = Request();
        _documents.FailOnAdd = true;

        await Should.ThrowAsync<InvalidOperationException>(() => _service.AttendAsync(Attend(request.Id), _ct));

        _storage.Uploaded.ShouldBeEmpty();
        _storage.Deleted.ShouldHaveSingleItem();
        _unitOfWork.RolledBack.ShouldBe(1);
        request.AttendedAt.ShouldBeNull();
        _notifications.ReceivedCalls().ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ consultas

    [Fact]
    public async Task GetForUploadAsync_SolicitudAbiertaDelCliente_RegresaElDetalle()
    {
        var request = Request();

        var detail = await _service.GetForUploadAsync(request.Id, ClientId, _ct);

        detail.ReferenciaNumber.ShouldBe(ReferenciaNumber);
        detail.DocumentType.ShouldBe(DocumentTypeEnum.DOCUMENTO_TRANSPORTE);
        detail.Note.ShouldBe("El B/L no es legible");
    }

    [Fact]
    public async Task GetForUploadAsync_SolicitudDeOtroCliente_NoLaEncuentra()
    {
        var request = Request();

        await Should.ThrowAsync<DocumentRequestNotFoundException>(() => _service.GetForUploadAsync(request.Id, OtherClientId, _ct));
    }

    [Fact]
    public async Task GetPendingAsync_SoloLasAbiertasDelCliente()
    {
        var open = Request(DocumentTypeEnum.DOCUMENTO_TRANSPORTE);
        _requests.Seed(ReferenciaId, DocumentTypeEnum.LISTA_EMPAQUE, CapturadorId, attendedAt: Now);

        var pending = await _service.GetPendingAsync(ClientId, _ct);
        var otherClient = await _service.GetPendingAsync(OtherClientId, _ct);

        pending.ShouldHaveSingleItem().RequestId.ShouldBe(open.Id);
        otherClient.ShouldBeEmpty();
    }
}
