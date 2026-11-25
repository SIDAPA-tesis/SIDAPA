using Microsoft.EntityFrameworkCore;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.Repositories.CaseFile.Documents;
using SIDAPA.infraestructure.test.Persistence;
using static SIDAPA.domain.Enums.Core.ReferenciaStatusEnum;

namespace SIDAPA.infraestructure.test.Repositories.CaseFile.Documents;

/// <summary>
/// Pruebas de <see cref="DocumentRequestRepository"/>, <see cref="UnitOfWork"/> y del candado de
/// <see cref="ReferenciaRepository.LockStatusAsync"/> contra PostgreSQL real (SCRUM-64 · HU-05): cierre condicional
/// de la solicitud, aislamiento por cliente, rollback de todo junto y la carrera de dos subidas al mismo tiempo.
/// </summary>
[Collection(PostgresCollection.Name)]
public class DocumentRequestRepositoryTests(PostgresFixture db)
{
    private const string Comment = "El cliente entregó los documentos solicitados.";
    private static readonly DateTime AttendedAt = new(2026, 11, 24, 9, 15, 0, DateTimeKind.Unspecified);

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private sealed record Scenario(ClientEntity Client, UserEntity Capturador, ReferenciaEntity Referencia);

    private static async Task<Scenario> ScenarioAsync(SidapaDbContext context, ReferenciaStatusEnum status = EN_ESPERA_DE_DOCUMENTOS)
    {
        var client = await TestData.ClientAsync(context);
        var capturador = await TestData.UserAsync(context);
        var referencia = await TestData.ReferenciaAsync(context, client.Id, status);
        return new Scenario(client, capturador, referencia);
    }

    // ------------------------------------------------------------------ MarkAttendedAsync

    [Fact]
    public async Task MarkAttendedAsync_SolicitudAbierta_LaCierraConElDocumentoYLaFecha()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var request = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        var document = await TestData.DocumentAsync(arrange, s.Referencia.Id, s.Capturador.Id);

        await using var context = db.CreateContext();
        var closed = await new DocumentRequestRepository(context).MarkAttendedAsync(request.Id, document.Id, AttendedAt, _ct);

        closed.ShouldBeTrue();
        await using var verify = db.CreateContext();
        var saved = await verify.DocumentRequests.AsNoTracking().SingleAsync(x => x.Id == request.Id, _ct);
        saved.ResponseDocumentId.ShouldBe(document.Id);
        saved.AttendedAt.ShouldBe(AttendedAt);   // timestamp without time zone: sin desfase
    }

    [Fact]
    public async Task MarkAttendedAsync_SegundoEnvio_RegresaFalseYNoPisaElPrimerDocumento()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var request = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        var first = await TestData.DocumentAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        var second = await TestData.DocumentAsync(arrange, s.Referencia.Id, s.Capturador.Id);

        await using var context = db.CreateContext();
        var repository = new DocumentRequestRepository(context);
        await repository.MarkAttendedAsync(request.Id, first.Id, AttendedAt, _ct);
        var closedAgain = await repository.MarkAttendedAsync(request.Id, second.Id, AttendedAt.AddMinutes(1), _ct);

        closedAgain.ShouldBeFalse();
        await using var verify = db.CreateContext();
        (await verify.DocumentRequests.AsNoTracking().SingleAsync(x => x.Id == request.Id, _ct))
            .ResponseDocumentId.ShouldBe(first.Id);
    }

    // ------------------------------------------------------------------ consultas

    [Fact]
    public async Task GetForClientAsync_SolicitudDelCliente_RegresaElDetalleConNombresDelCatalogo()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var request = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id, DocumentTypeEnum.LISTA_EMPAQUE);

        await using var context = db.CreateContext();
        var detail = await new DocumentRequestRepository(context).GetForClientAsync(request.Id, s.Client.Id, _ct);

        detail.ShouldNotBeNull();
        detail.ReferenciaId.ShouldBe(s.Referencia.Id);
        detail.ReferenciaNumber.ShouldBe(s.Referencia.ReferenciaNumber);
        detail.ReferenciaStatus.ShouldBe(EN_ESPERA_DE_DOCUMENTOS);
        detail.DocumentType.ShouldBe(DocumentTypeEnum.LISTA_EMPAQUE);
        detail.DocumentTypeName.ShouldNotBeNullOrWhiteSpace();   // viene de cat_tipo_documento
        detail.RequestedByUserId.ShouldBe(s.Capturador.Id);
        detail.IsAttended.ShouldBeFalse();
    }

    [Fact]
    public async Task GetForClientAsync_SolicitudDeOtroCliente_RegresaNull()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var other = await TestData.ClientAsync(arrange);
        var request = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);

        await using var context = db.CreateContext();
        var detail = await new DocumentRequestRepository(context).GetForClientAsync(request.Id, other.Id, _ct);

        detail.ShouldBeNull();
    }

    [Fact]
    public async Task GetOpenByClientAsync_SoloAbiertasDeReferenciasEnEsperaDelCliente()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var open = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id, DocumentTypeEnum.LISTA_EMPAQUE, attendedAt: AttendedAt);
        var inReview = await TestData.ReferenciaAsync(arrange, s.Client.Id, EN_REVISION);   // ya no espera documentos
        await TestData.DocumentRequestAsync(arrange, inReview.Id, s.Capturador.Id);
        var otherClient = await ScenarioAsync(arrange);
        await TestData.DocumentRequestAsync(arrange, otherClient.Referencia.Id, otherClient.Capturador.Id);

        await using var context = db.CreateContext();
        var pending = await new DocumentRequestRepository(context).GetOpenByClientAsync(s.Client.Id, _ct);

        var item = pending.ShouldHaveSingleItem();
        item.RequestId.ShouldBe(open.Id);
        item.ReferenciaNumber.ShouldBe(s.Referencia.ReferenciaNumber);
        item.Note.ShouldBe("Nota de prueba");
    }

    [Fact]
    public async Task CountOpenAsync_CuentaSoloLasAbiertasDeLaReferencia()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id, DocumentTypeEnum.LISTA_EMPAQUE);
        await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id, DocumentTypeEnum.POLIZA_SEGURO_TRANSPORTE, attendedAt: AttendedAt);

        await using var context = db.CreateContext();
        var open = await new DocumentRequestRepository(context).CountOpenAsync(s.Referencia.Id, _ct);

        open.ShouldBe(2);
    }

    [Fact]
    public async Task CreateAsync_GuardaLaSolicitudAbierta()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);

        await using var context = db.CreateContext();
        var request = new DocumentRequestEntity
        {
            ReferenciaId = s.Referencia.Id,
            DocumentTypeId = (short)DocumentTypeEnum.DOCUMENTO_TRANSPORTE,
            RequestedByUserId = s.Capturador.Id,
            RequestedAt = AttendedAt
        };
        await new DocumentRequestRepository(context).CreateAsync(request, _ct);

        await using var verify = db.CreateContext();
        (await new DocumentRequestRepository(verify).CountOpenAsync(s.Referencia.Id, _ct)).ShouldBe(1);
    }

    // ------------------------------------------------------------------ UnitOfWork + ChangeStatusAsync + LockStatusAsync

    [Fact]
    public async Task LockStatusAsync_RegresaElEstatusActual_YNullSiNoExiste()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);

        await using var context = db.CreateContext();
        await using var transaction = await new UnitOfWork(context).BeginTransactionAsync(_ct);
        var repository = new ReferenciaRepository(context);
        var status = await repository.LockStatusAsync(s.Referencia.Id, _ct);
        var unknown = await repository.LockStatusAsync(int.MaxValue, _ct);

        status.ShouldBe(EN_ESPERA_DE_DOCUMENTOS);
        unknown.ShouldBeNull();
    }

    [Fact]
    public async Task UnitOfWork_SinCommit_DeshaceCierreEstatusYBitacora()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var request = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        var document = await TestData.DocumentAsync(arrange, s.Referencia.Id, s.Capturador.Id);

        await using (var context = db.CreateContext())
        await using (await new UnitOfWork(context).BeginTransactionAsync(_ct))
        {
            // ChangeStatusAsync se une a la transacción abierta en vez de abrir (y confirmar) la suya
            await new DocumentRequestRepository(context).MarkAttendedAsync(request.Id, document.Id, AttendedAt, _ct);
            (await new ReferenciaRepository(context).ChangeStatusAsync(s.Referencia.Id, EN_ESPERA_DE_DOCUMENTOS, EN_REVISION,
                s.Capturador.Id, Comment, AttendedAt, _ct)).ShouldBeTrue();
        }   // se libera sin Commit: rollback

        await using var verify = db.CreateContext();
        (await StatusOfAsync(verify, s.Referencia.Id)).ShouldBe(EN_ESPERA_DE_DOCUMENTOS);
        (await verify.StatusHistories.AnyAsync(x => x.ReferenciaId == s.Referencia.Id, _ct)).ShouldBeFalse();
        (await new DocumentRequestRepository(verify).CountOpenAsync(s.Referencia.Id, _ct)).ShouldBe(1);
    }

    [Fact]
    public async Task UnitOfWork_ConCommit_GuardaCierreEstatusYBitacoraJuntos()
    {
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var request = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        var document = await TestData.DocumentAsync(arrange, s.Referencia.Id, s.Capturador.Id);

        await using (var context = db.CreateContext())
        {
            await using var transaction = await new UnitOfWork(context).BeginTransactionAsync(_ct);
            await new DocumentRequestRepository(context).MarkAttendedAsync(request.Id, document.Id, AttendedAt, _ct);
            await new ReferenciaRepository(context).ChangeStatusAsync(s.Referencia.Id, EN_ESPERA_DE_DOCUMENTOS, EN_REVISION,
                s.Capturador.Id, Comment, AttendedAt, _ct);
            await transaction.CommitAsync(_ct);
        }

        await using var verify = db.CreateContext();
        (await StatusOfAsync(verify, s.Referencia.Id)).ShouldBe(EN_REVISION);
        (await verify.StatusHistories.SingleAsync(x => x.ReferenciaId == s.Referencia.Id, _ct)).Comment.ShouldBe(Comment);
        (await new DocumentRequestRepository(verify).CountOpenAsync(s.Referencia.Id, _ct)).ShouldBe(0);
    }

    [Fact]
    public async Task DosSubidasAlMismoTiempo_ConCandado_LaReferenciaRegresaAEnRevisionUnaSolaVez()
    {
        // El bug que evita el candado: sin él, cada subida ve abierta la solicitud de la otra (aún sin confirmar),
        // ninguna cambia el estatus y la referencia se queda en "En espera de documentos" para siempre.
        await using var arrange = db.CreateContext();
        var s = await ScenarioAsync(arrange);
        var blRequest = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id);
        var packingRequest = await TestData.DocumentRequestAsync(arrange, s.Referencia.Id, s.Capturador.Id, DocumentTypeEnum.LISTA_EMPAQUE);
        var bl = await TestData.DocumentAsync(arrange, s.Referencia.Id, s.Capturador.Id, DocumentTypeEnum.DOCUMENTO_TRANSPORTE);
        var packing = await TestData.DocumentAsync(arrange, s.Referencia.Id, s.Capturador.Id, DocumentTypeEnum.LISTA_EMPAQUE);

        // Subida 1: cierra la suya, ve que queda otra abierta y todavía no confirma (tiene el candado).
        await using var first = db.CreateContext();
        var firstTransaction = await new UnitOfWork(first).BeginTransactionAsync(_ct);
        (await new ReferenciaRepository(first).LockStatusAsync(s.Referencia.Id, _ct)).ShouldBe(EN_ESPERA_DE_DOCUMENTOS);
        await new DocumentRequestRepository(first).MarkAttendedAsync(blRequest.Id, bl.Id, AttendedAt, _ct);
        (await new DocumentRequestRepository(first).CountOpenAsync(s.Referencia.Id, _ct)).ShouldBe(1);

        // Subida 2 al mismo tiempo: debe esperar el candado y, al entrar, ya ver la solicitud 1 cerrada.
        await using var second = db.CreateContext();
        var secondUpload = AttendAsync(second, s.Referencia.Id, packingRequest.Id, packing.Id, s.Capturador.Id);
        await PostgresLocks.WaitUntilSomeoneWaitsForAsync(first, secondUpload, _ct);
        secondUpload.IsCompleted.ShouldBeFalse("la segunda subida debió esperar el candado de la primera");

        await firstTransaction.CommitAsync(_ct);
        await firstTransaction.DisposeAsync();
        var returnedToReview = await secondUpload;

        returnedToReview.ShouldBeTrue();
        await using var verify = db.CreateContext();
        (await StatusOfAsync(verify, s.Referencia.Id)).ShouldBe(EN_REVISION);
        (await verify.StatusHistories.CountAsync(x => x.ReferenciaId == s.Referencia.Id, _ct)).ShouldBe(1);
        (await new DocumentRequestRepository(verify).CountOpenAsync(s.Referencia.Id, _ct)).ShouldBe(0);
    }

    // ------------------------------------------------------------------ Ayudantes

    /// <summary>La parte de BD de <c>DocumentRequestService.AttendAsync</c>: candado → cierre → conteo → estatus → commit.</summary>
    private async Task<bool> AttendAsync(SidapaDbContext context, int referenciaId, int requestId, int documentId, int userId)
    {
        await using var transaction = await new UnitOfWork(context).BeginTransactionAsync(_ct);
        var referencias = new ReferenciaRepository(context);
        var requests = new DocumentRequestRepository(context);

        await referencias.LockStatusAsync(referenciaId, _ct);
        await requests.MarkAttendedAsync(requestId, documentId, AttendedAt, _ct);
        var changed = await requests.CountOpenAsync(referenciaId, _ct) == 0
            && await referencias.ChangeStatusAsync(referenciaId, EN_ESPERA_DE_DOCUMENTOS, EN_REVISION, userId, Comment, AttendedAt, _ct);

        await transaction.CommitAsync(_ct);
        return changed;
    }

    private async Task<ReferenciaStatusEnum> StatusOfAsync(SidapaDbContext context, int referenciaId) =>
        (ReferenciaStatusEnum)await context.Referencias
            .Where(x => x.Id == referenciaId)
            .Select(x => x.StatusId)
            .SingleAsync(_ct);
}
