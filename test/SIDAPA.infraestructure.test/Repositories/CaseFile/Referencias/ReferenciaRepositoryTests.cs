using Microsoft.EntityFrameworkCore;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.test.Persistence;

namespace SIDAPA.infraestructure.test.Repositories.CaseFile.Referencias;

/// <summary>
/// Pruebas de <see cref="ReferenciaRepository"/> contra PostgreSQL real (Testcontainers) del aviso de arribo
/// (SCRUM-46) y del cambio de estatus del expediente (SCRUM-56): folio sin repetidos, cambio de estatus
/// condicionado al estatus de origen con su bitácora, y borrado completo para la compensación.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ReferenciaRepositoryTests(PostgresFixture db)
{
    private const string Comment = "Expediente completo";
    private const int UnknownReferenciaId = int.MaxValue;
    private static readonly DateTime ChangedAt = new(2026, 11, 23, 15, 30, 0, DateTimeKind.Unspecified);

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // ------------------------------------------------------------------ NextNumberAsync

    [Fact]
    public async Task NextNumberAsync_LlamadasSeguidas_RegresaNumerosDistintosYCrecientes()
    {
        await using var context = db.CreateContext();
        var repository = new ReferenciaRepository(context);

        var first = await repository.NextNumberAsync(_ct);
        var second = await repository.NextNumberAsync(_ct);
        var third = await repository.NextNumberAsync(_ct);

        // la secuencia es global (otras pruebas también la consumen): no se afirma un valor exacto
        first.ShouldBeGreaterThan(0);
        second.ShouldBeGreaterThan(first);
        third.ShouldBeGreaterThan(second);
    }

    [Fact]
    public async Task NextNumberAsync_VariosAvisosAlMismoTiempo_NuncaRepiteNumero()
    {
        db.EnsureAvailable();
        const int SimultaneousNotices = 25;

        // cada llamada con su propio contexto y conexión, como peticiones HTTP distintas
        var numbers = await Task.WhenAll(Enumerable.Range(0, SimultaneousNotices).Select(async _ =>
        {
            await using var context = db.CreateContext();
            return await new ReferenciaRepository(context).NextNumberAsync(_ct);
        }));

        numbers.ShouldBeUnique();
        numbers.ShouldAllBe(n => n > 0);
    }

    // ------------------------------------------------------------------ ChangeStatusAsync

    [Fact]
    public async Task ChangeStatusAsync_SigueEnElEstatusDeOrigen_CambiaElEstatusYDejaRegistroEnLaBitacora()
    {
        await using var arrange = db.CreateContext();
        var (referencia, capturador) = await ReferenciaAsync(arrange, ReferenciaStatusEnum.EN_REVISION);

        await using var context = db.CreateContext();
        var changed = await new ReferenciaRepository(context).ChangeStatusAsync(referencia.Id,
            ReferenciaStatusEnum.EN_REVISION, ReferenciaStatusEnum.EXPEDIENTE_COMPLETO,
            capturador.Id, Comment, ChangedAt, _ct);

        changed.ShouldBeTrue();
        await using var verify = db.CreateContext();
        (await StatusOfAsync(verify, referencia.Id)).ShouldBe(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO);
        var history = (await HistoryOfAsync(verify, referencia.Id)).ShouldHaveSingleItem();
        history.StatusId.ShouldBe((short)ReferenciaStatusEnum.EXPEDIENTE_COMPLETO);
        history.UserId.ShouldBe(capturador.Id);
        history.Comment.ShouldBe(Comment);
        history.ChangedAt.ShouldBe(ChangedAt);   // timestamp without time zone: se guarda tal cual, sin desfase
    }

    [Theory]
    [InlineData(ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS)]   // otro usuario la mandó a pedir documentos
    [InlineData(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO)]       // ya estaba en el estatus destino
    public async Task ChangeStatusAsync_YaNoEstaEnElEstatusDeOrigen_RegresaFalseYNoTocaNada(
        ReferenciaStatusEnum currentStatus)
    {
        await using var arrange = db.CreateContext();
        var (referencia, capturador) = await ReferenciaAsync(arrange, currentStatus);

        await using var context = db.CreateContext();
        var changed = await new ReferenciaRepository(context).ChangeStatusAsync(referencia.Id,
            ReferenciaStatusEnum.EN_REVISION, ReferenciaStatusEnum.EXPEDIENTE_COMPLETO,
            capturador.Id, Comment, ChangedAt, _ct);

        changed.ShouldBeFalse();
        await using var verify = db.CreateContext();
        (await StatusOfAsync(verify, referencia.Id)).ShouldBe(currentStatus);
        (await HistoryOfAsync(verify, referencia.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ChangeStatusAsync_ReferenciaQueNoExiste_RegresaFalseSinEscribirBitacora()
    {
        await using var arrange = db.CreateContext();
        var capturador = await TestData.UserAsync(arrange);

        await using var context = db.CreateContext();
        var changed = await new ReferenciaRepository(context).ChangeStatusAsync(UnknownReferenciaId,
            ReferenciaStatusEnum.EN_REVISION, ReferenciaStatusEnum.EXPEDIENTE_COMPLETO,
            capturador.Id, Comment, ChangedAt, _ct);

        changed.ShouldBeFalse();
        await using var verify = db.CreateContext();
        (await verify.StatusHistories.AnyAsync(x => x.UserId == capturador.Id, _ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeStatusAsync_OtroUsuarioLaCambiaMientrasSeEscribe_RegresaFalseYNoPisaSuCambio()
    {
        await using var arrange = db.CreateContext();
        var (referencia, capturador) = await ReferenciaAsync(arrange, ReferenciaStatusEnum.EN_REVISION);

        // Otro Capturador manda la referencia a "en espera de documentos" y todavía no confirma:
        // su transacción tiene el candado de la fila.
        await using var otherUser = db.CreateContext();
        await using var otherTransaction = await otherUser.Database.BeginTransactionAsync(_ct);
        await otherUser.Referencias
            .Where(x => x.Id == referencia.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.StatusId, (short)ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS), _ct);

        // El expediente se completa al mismo tiempo (ExpedienteService: EN_REVISION -> EXPEDIENTE_COMPLETO).
        await using var context = db.CreateContext();
        var change = new ReferenciaRepository(context).ChangeStatusAsync(referencia.Id,
            ReferenciaStatusEnum.EN_REVISION, ReferenciaStatusEnum.EXPEDIENTE_COMPLETO,
            capturador.Id, Comment, ChangedAt, _ct);
        await WaitUntilSomeoneWaitsForLockOfAsync(otherUser, change);
        await otherTransaction.CommitAsync(_ct);
        var changed = await change;

        changed.ShouldBeFalse();
        await using var verify = db.CreateContext();
        (await StatusOfAsync(verify, referencia.Id)).ShouldBe(ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS);
        (await HistoryOfAsync(verify, referencia.Id)).ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ DeleteAsync

    [Fact]
    public async Task DeleteAsync_ReferenciaConBitacoraYDocumentos_BorraTodo()
    {
        await using var arrange = db.CreateContext();
        var (referencia, capturador) = await ReferenciaAsync(arrange, ReferenciaStatusEnum.RECIBIDA);
        await TestData.StatusHistoryAsync(arrange, referencia.Id, capturador.Id);
        await TestData.DocumentAsync(arrange, referencia.Id, capturador.Id, DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR);
        await TestData.DocumentAsync(arrange, referencia.Id, capturador.Id, DocumentTypeEnum.DOCUMENTO_TRANSPORTE);

        await using var context = db.CreateContext();
        await new ReferenciaRepository(context).DeleteAsync(referencia.Id, _ct);

        await using var verify = db.CreateContext();
        (await verify.Referencias.AnyAsync(x => x.Id == referencia.Id, _ct)).ShouldBeFalse();
        (await verify.StatusHistories.AnyAsync(x => x.ReferenciaId == referencia.Id, _ct)).ShouldBeFalse();
        (await verify.Documents.AnyAsync(x => x.ReferenciaId == referencia.Id, _ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_OtraReferenciaDelMismoCliente_NoLaToca()
    {
        await using var arrange = db.CreateContext();
        var (deleted, capturador) = await ReferenciaAsync(arrange, ReferenciaStatusEnum.RECIBIDA);
        await TestData.StatusHistoryAsync(arrange, deleted.Id, capturador.Id);
        await TestData.DocumentAsync(arrange, deleted.Id, capturador.Id);
        var kept = await TestData.ReferenciaAsync(arrange, deleted.ClientId);
        var keptHistory = await TestData.StatusHistoryAsync(arrange, kept.Id, capturador.Id);
        var keptDocument = await TestData.DocumentAsync(arrange, kept.Id, capturador.Id);

        await using var context = db.CreateContext();
        await new ReferenciaRepository(context).DeleteAsync(deleted.Id, _ct);

        await using var verify = db.CreateContext();
        (await verify.Referencias.AnyAsync(x => x.Id == kept.Id, _ct)).ShouldBeTrue();
        (await verify.StatusHistories.Where(x => x.ReferenciaId == kept.Id).Select(x => x.Id).ToListAsync(_ct))
            .ShouldBe([keptHistory.Id]);
        (await verify.Documents.Where(x => x.ReferenciaId == kept.Id).Select(x => x.Id).ToListAsync(_ct))
            .ShouldBe([keptDocument.Id]);
    }

    [Fact]
    public async Task DeleteAsync_CompensacionDelAvisoEnElMismoContexto_NoDejaNadaGuardado()
    {
        // Como en ArrivalNoticeService: un solo DbContext por petición; AddAsync, un documento que sí se
        // guardó, otro que truena al guardar (queda rastreado en el contexto) y luego la compensación.
        await using var arrange = db.CreateContext();
        var client = await TestData.ClientAsync(arrange);
        var capturador = await TestData.UserAsync(arrange);

        await using var context = db.CreateContext();
        var repository = new ReferenciaRepository(context);
        var referencia = NewReferencia(client.Id);
        await repository.AddAsync(referencia, capturador.Id, _ct);
        await new DocumentRepository(context).AddAsync(NewDocument(referencia.Id, capturador.Id), _ct);
        context.Documents.Add(NewDocument(referencia.Id, capturador.Id, documentTypeId: short.MaxValue));
        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync(_ct));   // tipo inexistente: FK

        await repository.DeleteAsync(referencia.Id, _ct);

        await using var verify = db.CreateContext();
        (await verify.Referencias.AnyAsync(x => x.Id == referencia.Id, _ct)).ShouldBeFalse();
        (await verify.StatusHistories.AnyAsync(x => x.ReferenciaId == referencia.Id, _ct)).ShouldBeFalse();
        (await verify.Documents.AnyAsync(x => x.ReferenciaId == referencia.Id, _ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReferenciaQueNoExiste_NoLanza()
    {
        await using var context = db.CreateContext();

        // la compensación puede correr dos veces o después de un fallo parcial: no debe tronar
        await Should.NotThrowAsync(() => new ReferenciaRepository(context).DeleteAsync(UnknownReferenciaId, _ct));
    }

    // ------------------------------------------------------------------ Ayudantes

    private static async Task<(ReferenciaEntity Referencia, UserEntity Capturador)> ReferenciaAsync(
        SidapaDbContext context, ReferenciaStatusEnum status)
    {
        var client = await TestData.ClientAsync(context);
        var capturador = await TestData.UserAsync(context);
        var referencia = await TestData.ReferenciaAsync(context, client.Id, status);
        return (referencia, capturador);
    }

    private static ReferenciaEntity NewReferencia(int clientId) => new()
    {
        ReferenciaNumber = $"T-{Guid.NewGuid():N}"[..12],
        ClientId = clientId,
        OperationTypeCode = (short)OperationTypeEnum.IMPORTACION,
        AduanaCode = "470",
        StatusId = (short)ReferenciaStatusEnum.RECIBIDA,
        CreatedAt = ChangedAt
    };

    private static DocumentEntity NewDocument(int referenciaId, int userId,
        short documentTypeId = (short)DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR) => new()
    {
        ReferenciaId = referenciaId,
        DocumentTypeId = documentTypeId,
        FileName = "factura.pdf",
        BucketPath = $"referencias/{referenciaId}/{Guid.NewGuid():N}.pdf",
        SizeInBytes = 1024,
        Status = nameof(DocumentStatusEnum.RECIBIDO),
        UploadedByUserId = userId,
        UploadedAt = ChangedAt
    };

    private async Task<ReferenciaStatusEnum> StatusOfAsync(SidapaDbContext context, int referenciaId) =>
        (ReferenciaStatusEnum)await context.Referencias
            .Where(x => x.Id == referenciaId)
            .Select(x => x.StatusId)
            .SingleAsync(_ct);

    private async Task<List<StatusHistoryEntity>> HistoryOfAsync(SidapaDbContext context, int referenciaId) =>
        await context.StatusHistories
            .AsNoTracking()
            .Where(x => x.ReferenciaId == referenciaId)
            .ToListAsync(_ct);

    /// <summary>
    /// Espera (consultando a PostgreSQL, sin dormir) a que alguna sesión quede bloqueada por el candado que
    /// tiene la transacción de <paramref name="lockHolder"/>. Así el orden "el SUT ya está esperando, ahora
    /// confirma el otro usuario" es determinista. Si el SUT termina sin bloquearse, deja de esperar.
    /// Se consulta pg_locks y no pg_stat_activity: esta última se congela en la primera lectura de la
    /// transacción de <paramref name="lockHolder"/>, y si la conexión del SUT se abría después nunca aparecía.
    /// </summary>
    private async Task WaitUntilSomeoneWaitsForLockOfAsync(SidapaDbContext lockHolder, Task sut)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (!sut.IsCompleted)
        {
            var blocked = await lockHolder.Database
                .SqlQueryRaw<int>(
                    "SELECT count(*)::int AS \"Value\" FROM pg_locks WHERE NOT granted AND pg_backend_pid() = ANY(pg_blocking_pids(pid))")
                .SingleAsync(timeout.Token);
            if (blocked > 0)
                return;
        }
    }
}
