using SIDAPA.application.DTOs.Core.Notifications;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using Xunit;

namespace SIDAPA.Infraestructure.test;

/// <summary>Pruebas de <see cref="ExpedienteService"/> (SCRUM-56) con fakes a mano: no tocan la BD.</summary>
public class ExpedienteServiceTests
{
    private const int ReferenciaId = 7;
    private const int ClientId = 4;
    private const int CapturadorId = 2;

    private static readonly short[] Mandatory =
    [
        (short)DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR,
        (short)DocumentTypeEnum.DOCUMENTO_TRANSPORTE
    ];

    private static (ExpedienteService Service, FakeReferenciaRepository Referencias, FakeDocumentRepository Documents, FakeNotificationService Notifications)
        Create(ReferenciaStatusEnum status)
    {
        var referencias = new FakeReferenciaRepository(status);
        var documents = new FakeDocumentRepository();
        var notifications = new FakeNotificationService();
        return (new ExpedienteService(referencias, documents, notifications), referencias, documents, notifications);
    }

    [Fact]
    public async Task EvaluateAsync_TodoCorrectoEnRevision_CambiaEstatusYAvisa()
    {
        var (service, referencias, documents, notifications) = Create(ReferenciaStatusEnum.EN_REVISION);
        documents.Add(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentStatusEnum.CORRECTO);
        documents.Add(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, DocumentStatusEnum.CORRECTO);

        var result = await service.EvaluateAsync(ReferenciaId, CapturadorId);

        Assert.True(result.IsComplete);
        Assert.True(result.StatusChanged);
        Assert.Equal(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO, result.Status);
        var change = Assert.Single(referencias.Changes);
        Assert.Equal((ReferenciaStatusEnum.EN_REVISION, ReferenciaStatusEnum.EXPEDIENTE_COMPLETO, CapturadorId), change);
        var notice = Assert.Single(notifications.Sent);
        Assert.Equal(ClientId, notice.ClientId);
        Assert.Contains("R-000007", notice.Message);
    }

    [Fact]
    public async Task EvaluateAsync_FaltaUnDocumento_NoCambiaNada()
    {
        var (service, referencias, documents, notifications) = Create(ReferenciaStatusEnum.EN_REVISION);
        documents.Add(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentStatusEnum.CORRECTO);
        documents.Add(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, DocumentStatusEnum.FALTANTE);

        var result = await service.EvaluateAsync(ReferenciaId, CapturadorId);

        Assert.False(result.IsComplete);
        Assert.Equal(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, Assert.Single(result.MissingDocuments));
        Assert.Empty(referencias.Changes);
        Assert.Empty(notifications.Sent);
    }

    [Fact]
    public async Task EvaluateAsync_SiNoEstaEnRevision_NoCambiaNada()
    {
        var (service, referencias, documents, notifications) = Create(ReferenciaStatusEnum.RECIBIDA);
        documents.Add(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentStatusEnum.CORRECTO);
        documents.Add(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, DocumentStatusEnum.CORRECTO);

        var result = await service.EvaluateAsync(ReferenciaId, CapturadorId);

        Assert.True(result.IsComplete);
        Assert.False(result.StatusChanged);
        Assert.Empty(referencias.Changes);
        Assert.Empty(notifications.Sent);
    }

    [Fact]
    public async Task EvaluateAsync_OtroUsuarioLaCambioAntes_NoAvisaDosVeces()
    {
        var (service, referencias, documents, notifications) = Create(ReferenciaStatusEnum.EN_REVISION);
        referencias.SimulateConcurrentChange = true;
        documents.Add(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentStatusEnum.CORRECTO);
        documents.Add(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, DocumentStatusEnum.CORRECTO);

        var result = await service.EvaluateAsync(ReferenciaId, CapturadorId);

        Assert.False(result.StatusChanged);
        Assert.Empty(notifications.Sent);
    }

    [Fact]
    public async Task CheckAsync_SoloConsulta()
    {
        var (service, referencias, documents, notifications) = Create(ReferenciaStatusEnum.EN_REVISION);
        documents.Add(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DocumentStatusEnum.CORRECTO);
        documents.Add(DocumentTypeEnum.DOCUMENTO_TRANSPORTE, DocumentStatusEnum.CORRECTO);

        var result = await service.CheckAsync(ReferenciaId);

        Assert.True(result.IsComplete);
        Assert.False(result.StatusChanged);
        Assert.Empty(referencias.Changes);
        Assert.Empty(notifications.Sent);
    }

    [Fact]
    public async Task EvaluateAsync_ReferenciaInexistente_Lanza()
    {
        var (service, _, _, _) = Create(ReferenciaStatusEnum.EN_REVISION);

        await Assert.ThrowsAsync<ReferenciaNotFoundException>(() => service.EvaluateAsync(99, CapturadorId));
    }

    private sealed class FakeReferenciaRepository(ReferenciaStatusEnum status) : IReferenciaRepository
    {
        public List<(ReferenciaStatusEnum From, ReferenciaStatusEnum To, int UserId)> Changes { get; } = [];
        public bool SimulateConcurrentChange { get; set; }

        public Task<ReferenciaEntity?> GetByIdAsync(int referenciaId, CancellationToken ct = default) =>
            Task.FromResult(referenciaId != ReferenciaId
                ? null
                : new ReferenciaEntity
                {
                    Id = ReferenciaId,
                    ReferenciaNumber = "R-000007",
                    ClientId = ClientId,
                    OperationTypeCode = 1,
                    AduanaCode = "430",
                    StatusId = (short)status
                });

        public Task<IReadOnlyList<short>> GetMandatoryDocumentTypeIdsAsync(short operationTypeCode, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<short>>(Mandatory);

        public Task<bool> ChangeStatusAsync(int referenciaId, ReferenciaStatusEnum from, ReferenciaStatusEnum to,
            int userId, string? comment, DateTime changedAt, CancellationToken ct = default)
        {
            if (SimulateConcurrentChange)
                return Task.FromResult(false);

            Changes.Add((from, to, userId));
            return Task.FromResult(true);
        }
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly List<DocumentEntity> _documents = [];

        public void Add(DocumentTypeEnum type, DocumentStatusEnum status) => _documents.Add(new DocumentEntity
        {
            Id = _documents.Count + 1,
            ReferenciaId = ReferenciaId,
            DocumentTypeId = (short)type,
            FileName = "archivo.pdf",
            BucketPath = "referencias/R-000007/archivo.pdf",
            Status = status.ToString()
        });

        public Task<IReadOnlyList<DocumentEntity>> GetByReferenciaAsync(int referenciaId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DocumentEntity>>(_documents.Where(d => d.ReferenciaId == referenciaId).ToList());

        public Task<string?> GetReferenciaNumberAsync(int referenciaId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(DocumentEntity document, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DocumentEntity?> GetByIdAsync(int documentId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public List<(int ClientId, string Message)> Sent { get; } = [];

        public Task NotifyClientAsync(int clientId, string message, int? referenciaId = null, bool sendEmail = true, CancellationToken ct = default)
        {
            Sent.Add((clientId, message));
            return Task.CompletedTask;
        }

        public Task NotifyUserAsync(int userId, string message, int? referenciaId = null, bool sendEmail = true, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<NotificationDto>> GetRecentAsync(int userId, int take = 20, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}