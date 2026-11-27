using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.Repositories.CaseFile.Expediente;
using SIDAPA.infraestructure.test.Persistence;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;

namespace SIDAPA.infraestructure.test.Repositories.CaseFile.Expediente;

/// <summary>
/// <see cref="ExpedienteRepository"/> y <see cref="DocumentRepository.UpdateStatusAsync"/> contra PostgreSQL real
/// (SCRUM-55 · HU-03). La lista de verificación sale de la semilla <c>SeedRequiredDocuments</c>.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ExpedienteRepositoryTests(PostgresFixture db)
{
    private const int Limit = 50;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private async Task<(int ReferenciaId, string Number, string ClientName, int UserId)> GivenReferenciaAsync(
        ReferenciaStatusEnum status = ReferenciaStatusEnum.RECIBIDA)
    {
        await using var context = db.CreateContext();
        var client = await TestData.ClientAsync(context);
        var user = await TestData.UserAsync(context);
        var referencia = await TestData.ReferenciaAsync(context, client.Id, status);
        return (referencia.Id, referencia.ReferenciaNumber, client.BusinessName, user.Id);
    }

    // ------------------------------------------------------------------ GetDetailAsync

    [Fact]
    public async Task GetDetailAsync_TraeEncabezadoDocumentosYLaListaDeVerificacionDeImportacion()
    {
        var (referenciaId, number, clientName, userId) = await GivenReferenciaAsync();
        await using (var arrange = db.CreateContext())
        {
            await TestData.DocumentAsync(arrange, referenciaId, userId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR);
            await TestData.DocumentAsync(arrange, referenciaId, userId, LISTA_EMPAQUE);
        }

        await using var context = db.CreateContext();
        var detail = (await new ExpedienteRepository(context).GetDetailAsync(referenciaId, _ct)).ShouldNotBeNull();

        detail.ReferenciaNumber.ShouldBe(number);
        detail.ClientName.ShouldBe(clientName);
        detail.OperationTypeName.ShouldBe("Importación");
        detail.Status.ShouldBe(ReferenciaStatusEnum.RECIBIDA);
        detail.Documents.Select(d => d.Type).ShouldBe([FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, LISTA_EMPAQUE]);
        detail.Documents.ShouldAllBe(d => d.Status == DocumentStatusEnum.RECIBIDO && d.UploadedByName == "Usuario de prueba");

        // obligatorios primero, sin tipos repetidos
        detail.RequiredDocuments.Where(r => r.IsMandatory).Select(r => r.Type)
            .ShouldBe([FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, LISTA_EMPAQUE, DOCUMENTO_TRANSPORTE]);
        detail.RequiredDocuments.Take(3).ShouldAllBe(r => r.IsMandatory);
        detail.RequiredDocuments.Select(r => r.Type).ShouldBeUnique();
        detail.RequiredDocuments.ShouldNotContain(r => r.Type == CONSTANCIA_SITUACION_FISCAL);
    }

    [Fact]
    public async Task GetDetailAsync_ReferenciaQueNoExiste_RegresaNull()
    {
        await using var context = db.CreateContext();

        (await new ExpedienteRepository(context).GetDetailAsync(int.MaxValue, _ct)).ShouldBeNull();
    }

    // ------------------------------------------------------------------ SearchAsync

    [Fact]
    public async Task SearchAsync_PorNumeroOClienteSinDistinguirMayusculas()
    {
        var (referenciaId, number, clientName, userId) = await GivenReferenciaAsync();
        await using (var arrange = db.CreateContext())
            await TestData.DocumentAsync(arrange, referenciaId, userId);

        await using var context = db.CreateContext();
        var repository = new ExpedienteRepository(context);

        var byNumber = await repository.SearchAsync(new ExpedienteFilterDto(number.ToLowerInvariant(), false), Limit, _ct);
        var byClient = await repository.SearchAsync(new ExpedienteFilterDto(clientName.ToUpperInvariant(), false), Limit, _ct);

        var row = byNumber.ShouldHaveSingleItem();
        row.Id.ShouldBe(referenciaId);
        row.DocumentCount.ShouldBe(1);
        row.PendingReviewCount.ShouldBe(1);
        byClient.ShouldHaveSingleItem().Id.ShouldBe(referenciaId);
    }

    [Fact]
    public async Task SearchAsync_ComodinesDelUsuario_SeBuscanTalCual()
    {
        await GivenReferenciaAsync();
        await using var context = db.CreateContext();

        var rows = await new ExpedienteRepository(context).SearchAsync(new ExpedienteFilterDto("%", false), Limit, _ct);

        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_SoloPorRevisar_DejaFueraLasQueYaPasaronLaRevision()
    {
        var recibida = await GivenReferenciaAsync(ReferenciaStatusEnum.RECIBIDA);
        var enRevision = await GivenReferenciaAsync(ReferenciaStatusEnum.EN_REVISION);
        var completa = await GivenReferenciaAsync(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO);

        await using var context = db.CreateContext();
        var rows = await new ExpedienteRepository(context).SearchAsync(new ExpedienteFilterDto(null, true), int.MaxValue, _ct);
        var ids = rows.Select(r => r.Id).ToList();

        ids.ShouldContain(recibida.ReferenciaId);
        ids.ShouldContain(enRevision.ReferenciaId);
        ids.ShouldNotContain(completa.ReferenciaId);
    }

    [Fact]
    public async Task SearchAsync_RespetaElTope()
    {
        await GivenReferenciaAsync();
        await GivenReferenciaAsync();
        await using var context = db.CreateContext();

        var rows = await new ExpedienteRepository(context).SearchAsync(new ExpedienteFilterDto(null, false), 1, _ct);

        rows.Count.ShouldBe(1);
    }

    // ------------------------------------------------------------------ DocumentRepository.UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_DocumentoDeLaReferencia_GuardaElNombreDelEstado()
    {
        var (referenciaId, _, _, userId) = await GivenReferenciaAsync();
        int documentId;
        await using (var arrange = db.CreateContext())
            documentId = (await TestData.DocumentAsync(arrange, referenciaId, userId)).Id;

        await using var context = db.CreateContext();
        var updated = await new DocumentRepository(context).UpdateStatusAsync(documentId, referenciaId, DocumentStatusEnum.ERROR, _ct);

        updated.ShouldBeTrue();
        await using var assert = db.CreateContext();
        (await assert.Documents.SingleAsync(d => d.Id == documentId, _ct)).Status.ShouldBe("ERROR");
    }

    [Fact]
    public async Task UpdateStatusAsync_DocumentoDeOtraReferencia_NoCambiaNada()
    {
        var (referenciaId, _, _, userId) = await GivenReferenciaAsync();
        var otra = await GivenReferenciaAsync();
        int documentId;
        await using (var arrange = db.CreateContext())
            documentId = (await TestData.DocumentAsync(arrange, referenciaId, userId)).Id;

        await using var context = db.CreateContext();
        var updated = await new DocumentRepository(context).UpdateStatusAsync(documentId, otra.ReferenciaId, DocumentStatusEnum.CORRECTO, _ct);

        updated.ShouldBeFalse();
        await using var assert = db.CreateContext();
        (await assert.Documents.SingleAsync(d => d.Id == documentId, _ct)).Status.ShouldBe("RECIBIDO");
    }
}
