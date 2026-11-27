using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.Repositories.CaseFile.Expediente;
using SIDAPA.infraestructure.test.Persistence;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;

namespace SIDAPA.infraestructure.test.Repositories.CaseFile.Expediente;

/// <summary>
/// <see cref="ExpedienteRepository"/>, <see cref="DocumentRepository.UpdateStatusAsync"/> y
/// <see cref="ReferenciaRepository.AssignCapturadorAsync"/> contra PostgreSQL real (SCRUM-55 · HU-03). La lista de
/// verificación sale de las semillas <c>SeedRequiredDocuments</c> y <c>SeedExportRequiredDocuments</c>.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ExpedienteRepositoryTests(PostgresFixture db)
{
    private const int PageSize = 50;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private async Task<(int ReferenciaId, string Number, string ClientName, int UserId)> GivenReferenciaAsync(
        ReferenciaStatusEnum status = ReferenciaStatusEnum.RECIBIDA, OperationTypeEnum operation = OperationTypeEnum.IMPORTACION)
    {
        await using var context = db.CreateContext();
        var client = await TestData.ClientAsync(context);
        var user = await TestData.UserAsync(context);
        var referencia = await TestData.ReferenciaAsync(context, client.Id, status, operation);
        return (referencia.Id, referencia.ReferenciaNumber, client.BusinessName, user.Id);
    }

    private async Task<ExpedientePageDto> SearchAsync(ExpedienteFilterDto filter, int pageSize = PageSize)
    {
        await using var context = db.CreateContext();
        return await new ExpedienteRepository(context).SearchAsync(filter, pageSize, _ct);
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
        detail.CapturadorName.ShouldBeNull();
        detail.Documents.Select(d => d.Type).ShouldBe([FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, LISTA_EMPAQUE]);
        detail.Documents.ShouldAllBe(d => d.Status == DocumentStatusEnum.RECIBIDO && d.UploadedByName == "Usuario de prueba");

        // obligatorios primero, sin tipos repetidos
        detail.RequiredDocuments.Where(r => r.IsMandatory).Select(r => r.Type)
            .ShouldBe([FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, LISTA_EMPAQUE, DOCUMENTO_TRANSPORTE]);
        detail.RequiredDocuments.Take(3).ShouldAllBe(r => r.IsMandatory);
        detail.RequiredDocuments.Select(r => r.Type).ShouldBeUnique();
        detail.RequiredDocuments.Select(r => r.Type).ShouldContain(MANIFESTACION_VALOR_HOJA_CALCULO);
        detail.RequiredDocuments.ShouldNotContain(r => r.Type == CONSTANCIA_SITUACION_FISCAL);
    }

    [Fact]
    public async Task GetDetailAsync_Exportacion_NoPideLosDocumentosSoloDeImportacion()
    {
        var (referenciaId, _, _, _) = await GivenReferenciaAsync(operation: OperationTypeEnum.EXPORTACION);

        await using var context = db.CreateContext();
        var detail = (await new ExpedienteRepository(context).GetDetailAsync(referenciaId, _ct)).ShouldNotBeNull();

        detail.OperationTypeName.ShouldBe("Exportación");
        detail.RequiredDocuments.Where(r => r.IsMandatory).Select(r => r.Type)
            .ShouldBe([FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, LISTA_EMPAQUE, DOCUMENTO_TRANSPORTE]);
        detail.RequiredDocuments.Select(r => r.Type).ShouldNotContain(PADRON_IMPORTADORES_SECTORES_ESPECIFICOS);
        detail.RequiredDocuments.Select(r => r.Type).ShouldNotContain(MANIFESTACION_VALOR_HOJA_CALCULO);
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

        var byNumber = await SearchAsync(new ExpedienteFilterDto(number.ToLowerInvariant(), false));
        var byClient = await SearchAsync(new ExpedienteFilterDto(clientName.ToUpperInvariant(), false));

        var row = byNumber.Items.ShouldHaveSingleItem();
        row.Id.ShouldBe(referenciaId);
        row.DocumentCount.ShouldBe(1);
        row.PendingReviewCount.ShouldBe(1);
        row.CapturadorName.ShouldBeNull();
        byNumber.TotalCount.ShouldBe(1);
        byClient.Items.ShouldHaveSingleItem().Id.ShouldBe(referenciaId);
    }

    [Fact]
    public async Task SearchAsync_ComodinesDelUsuario_SeBuscanTalCual()
    {
        await GivenReferenciaAsync();

        var page = await SearchAsync(new ExpedienteFilterDto("%", false));

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
        page.TotalPages.ShouldBe(1);
    }

    [Fact]
    public async Task SearchAsync_SoloPorRevisar_DejaFueraLasQueYaPasaronLaRevision()
    {
        var recibida = await GivenReferenciaAsync(ReferenciaStatusEnum.RECIBIDA);
        var enRevision = await GivenReferenciaAsync(ReferenciaStatusEnum.EN_REVISION);
        var completa = await GivenReferenciaAsync(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO);

        var ids = (await SearchAsync(new ExpedienteFilterDto(null, true), int.MaxValue)).Items.Select(r => r.Id).ToList();

        ids.ShouldContain(recibida.ReferenciaId);
        ids.ShouldContain(enRevision.ReferenciaId);
        ids.ShouldNotContain(completa.ReferenciaId);
    }

    [Fact]
    public async Task SearchAsync_Paginas_CuentaElTotalYUnaPaginaFueraDeRangoRegresaLaUltima()
    {
        // tres referencias del mismo cliente: la búsqueda por su razón social las aísla de las demás pruebas
        string clientName;
        await using (var arrange = db.CreateContext())
        {
            var client = await TestData.ClientAsync(arrange);
            clientName = client.BusinessName;
            for (var i = 0; i < 3; i++)
                await TestData.ReferenciaAsync(arrange, client.Id);
        }

        var first = await SearchAsync(new ExpedienteFilterDto(clientName, false, 1), pageSize: 2);
        var second = await SearchAsync(new ExpedienteFilterDto(clientName, false, 2), pageSize: 2);
        var beyond = await SearchAsync(new ExpedienteFilterDto(clientName, false, 99), pageSize: 2);

        first.Items.Count.ShouldBe(2);
        first.TotalCount.ShouldBe(3);
        first.TotalPages.ShouldBe(2);
        second.Items.ShouldHaveSingleItem();
        first.Items.Select(r => r.Id).ShouldNotContain(second.Items[0].Id);
        beyond.Page.ShouldBe(2);
        beyond.Items.Select(r => r.Id).ShouldBe(second.Items.Select(r => r.Id));
    }

    // ------------------------------------------------------------------ DocumentRepository.UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_DocumentoDeLaReferencia_GuardaEstadoYObservacion()
    {
        var (referenciaId, _, _, userId) = await GivenReferenciaAsync();
        int documentId;
        await using (var arrange = db.CreateContext())
            documentId = (await TestData.DocumentAsync(arrange, referenciaId, userId)).Id;

        await using var context = db.CreateContext();
        var updated = await new DocumentRepository(context)
            .UpdateStatusAsync(documentId, referenciaId, DocumentStatusEnum.ERROR, "Ilegible", _ct);

        updated.ShouldBeTrue();
        await using var assert = db.CreateContext();
        var saved = await assert.Documents.SingleAsync(d => d.Id == documentId, _ct);
        (saved.Status, saved.Note).ShouldBe(("ERROR", "Ilegible"));
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
        var updated = await new DocumentRepository(context)
            .UpdateStatusAsync(documentId, otra.ReferenciaId, DocumentStatusEnum.CORRECTO, null, _ct);

        updated.ShouldBeFalse();
        await using var assert = db.CreateContext();
        (await assert.Documents.SingleAsync(d => d.Id == documentId, _ct)).Status.ShouldBe("RECIBIDO");
    }

    // ------------------------------------------------------------------ ReferenciaRepository.AssignCapturadorAsync

    [Fact]
    public async Task AssignCapturadorAsync_SoloLaPrimeraVez_YSeVeEnLaBandejaYElDetalle()
    {
        var (referenciaId, number, _, primero) = await GivenReferenciaAsync();
        int segundo;
        await using (var arrange = db.CreateContext())
            segundo = (await TestData.UserAsync(arrange)).Id;

        await using (var context = db.CreateContext())
        {
            var repository = new ReferenciaRepository(context);
            (await repository.AssignCapturadorAsync(referenciaId, primero, _ct)).ShouldBeTrue();
            (await repository.AssignCapturadorAsync(referenciaId, segundo, _ct)).ShouldBeFalse();
        }

        await using var assert = db.CreateContext();
        (await assert.Referencias.SingleAsync(r => r.Id == referenciaId, _ct)).CapturadorId.ShouldBe(primero);
        (await new ExpedienteRepository(assert).GetDetailAsync(referenciaId, _ct))!.CapturadorName.ShouldBe("Usuario de prueba");
        (await SearchAsync(new ExpedienteFilterDto(number, false))).Items.ShouldHaveSingleItem()
            .CapturadorName.ShouldBe("Usuario de prueba");
    }
}
