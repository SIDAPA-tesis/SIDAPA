using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.Interface.CaseFile.Expediente;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.CaseFile;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;
using SIDAPA.presentation.Controllers.Web.CaseFile.Expediente;
using SIDAPA.presentation.Security;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;

namespace SIDAPA.presentation.test.Controllers.CaseFile.Expediente;

/// <summary>
/// <see cref="ExpedientesController"/> (SCRUM-55 · HU-03): quién consulta y quién marca, que el usuario sale del token,
/// y cómo se sirve cada archivo de la vista previa. Las reglas de negocio se prueban en application.
/// </summary>
public class ExpedientesControllerTests(SidapaWebFactory factory) : IClassFixture<SidapaWebFactory>, IDisposable
{
    private const int ReferenciaId = 7;
    private const int FacturaId = 11;
    private const int UserId = 21;
    private const string DetalleUrl = "/Expedientes/Detalle/7";

    private readonly IExpedienteReviewService _service = Substitute.For<IExpedienteReviewService>();
    private readonly INotificationService _notificaciones = Substitute.For<INotificationService>();
    private readonly List<WebApplicationFactory<Program>> _apps = [];
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private HttpClient ClientAs(RolesEnum? role)
    {
        _notificaciones.GetRecentAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        var app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IExpedienteReviewService>();
            s.AddSingleton(_service);
            s.RemoveAll<INotificationService>();
            s.AddSingleton(_notificaciones);
        }));
        _apps.Add(app);

        var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        if (role is { } r)
            client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={factory.CreateToken(r, UserId).AccessToken}");
        return client;
    }

    public void Dispose()
    {
        foreach (var app in _apps)
            app.Dispose();
    }

    private void GivenReferencia(ReferenciaStatusEnum status = ReferenciaStatusEnum.EN_REVISION) =>
        _service.GetAsync(ReferenciaId, Arg.Any<CancellationToken>()).Returns(new ExpedienteReviewDto(
            new ExpedienteDetailDto(ReferenciaId, "R-000007", "Importadora Norte", "AICM", "Importación", status, "En revisión",
                new DateOnly(2026, 11, 30), null, "Capturador Uno", new DateTime(2026, 11, 23, 16, 0, 0),
                [new ExpedienteDocumentDto(FacturaId, FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, "Factura comercial", "factura.pdf",
                    2048, DocumentStatusEnum.RECIBIDO, "Cliente demo", new DateTime(2026, 11, 23, 16, 5, 0), "Sin firma del proveedor")],
                [new RequiredDocumentDto(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, "Factura comercial", true),
                 new RequiredDocumentDto(POLIZA_SEGURO_TRANSPORTE, "Póliza de seguro", false)]),
            [new ChecklistItemDto(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, "Factura comercial", true, DocumentStatusEnum.RECIBIDO),
             new ChecklistItemDto(POLIZA_SEGURO_TRANSPORTE, "Póliza de seguro", false, DocumentStatusEnum.FALTANTE)],
            CanBeReviewed: status is ReferenciaStatusEnum.RECIBIDA or ReferenciaStatusEnum.EN_REVISION));

    private async Task<string> HtmlAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(_ct));

    // ------------------------------------------------------------------ Acceso

    [Fact]
    public async Task Index_Cliente_RedirigeAAccesoDenegado()
    {
        var response = await ClientAs(RolesEnum.CLIENTE).GetAsync("/Expedientes", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe("/Auth/Denied");
        _service.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(RolesEnum.CAPTURADOR)]
    [InlineData(RolesEnum.GLOSADOR)]
    [InlineData(RolesEnum.ADMINISTRADOR)]
    public async Task Index_PersonalInterno_VeLaBandeja(RolesEnum role)
    {
        _service.SearchAsync(Arg.Any<ExpedienteFilterDto>(), Arg.Any<CancellationToken>()).Returns(new ExpedientePageDto(
            [new ExpedienteSummaryDto(ReferenciaId, "R-000007", "Importadora Norte", "AICM", ReferenciaStatusEnum.RECIBIDA,
                "Recibida", new DateTime(2026, 11, 23, 16, 0, 0), 3, 2, null)], TotalCount: 1, Page: 1, PageSize: 20));

        var response = await ClientAs(role).GetAsync("/Expedientes?q=norte&porRevisar=true", _ct);
        var html = await HtmlAsync(response);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("href=\"/Expedientes/Detalle/7\"");
        html.ShouldContain("2 por revisar");
        await _service.Received(1).SearchAsync(new ExpedienteFilterDto("norte", true, 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Index_VariasPaginas_PintaLaPaginacionConservandoElFiltro()
    {
        _service.SearchAsync(Arg.Any<ExpedienteFilterDto>(), Arg.Any<CancellationToken>()).Returns(new ExpedientePageDto(
            [new ExpedienteSummaryDto(ReferenciaId, "R-000007", "Importadora Norte", "AICM", ReferenciaStatusEnum.RECIBIDA,
                "Recibida", new DateTime(2026, 11, 23, 16, 0, 0), 1, 0, null)], TotalCount: 45, Page: 2, PageSize: 20));

        var html = await HtmlAsync(await ClientAs(RolesEnum.CAPTURADOR).GetAsync("/Expedientes?q=norte&porRevisar=true&pagina=2", _ct));

        html.ShouldContain("Página 2 de 3 · 45 referencias");
        html.ShouldContain("href=\"/Expedientes?pagina=3&q=norte&porRevisar=true\"");
        Regex.IsMatch(html, "aria-current=\"page\"[^>]*>2</a>").ShouldBeTrue();
        html.ShouldContain("Sin asignar");
        await _service.Received(1).SearchAsync(new ExpedienteFilterDto("norte", true, 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Index_UnaSolaPagina_NoPintaPaginacion()
    {
        _service.SearchAsync(Arg.Any<ExpedienteFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new ExpedientePageDto([], TotalCount: 0, Page: 1, PageSize: 20));

        var html = await HtmlAsync(await ClientAs(RolesEnum.GLOSADOR).GetAsync("/Expedientes", _ct));

        html.ShouldContain("No se encontraron referencias.");
        html.ShouldNotContain("pagination");
    }

    [Fact]
    public async Task Detalle_MuestraCapturadorYObservacion()
    {
        GivenReferencia();

        var html = await HtmlAsync(await ClientAs(RolesEnum.CAPTURADOR).GetAsync(DetalleUrl, _ct));

        html.ShouldContain("Capturador: Capturador Uno");
        html.ShouldContain("Observación:</span> Sin firma del proveedor");
        html.ShouldContain("name=\"Nota\"");
        html.ShouldContain("maxlength=\"300\"");
    }

    [Fact]
    public async Task Detalle_Glosador_ConsultaSinBotonesParaMarcar()
    {
        GivenReferencia();

        var html = await HtmlAsync(await ClientAs(RolesEnum.GLOSADOR).GetAsync(DetalleUrl, _ct));

        html.ShouldContain("Factura comercial");
        html.ShouldContain("Lista de verificación");
        html.ShouldNotContain("name=\"Estado\"");
    }

    [Theory]
    [InlineData(RolesEnum.CAPTURADOR)]
    [InlineData(RolesEnum.ADMINISTRADOR)]
    public async Task Detalle_QuienPuedeMarcar_VeLasTresMarcas(RolesEnum role)
    {
        GivenReferencia();

        var html = await HtmlAsync(await ClientAs(role).GetAsync(DetalleUrl, _ct));

        html.ShouldContain("value=\"CORRECTO\"");
        html.ShouldContain("value=\"FALTANTE\"");
        html.ShouldContain("value=\"ERROR\"");
        html.ShouldContain("__RequestVerificationToken");
        html.ShouldContain("0 de 1 obligatorios correctos");
        html.ShouldContain("No recibido");   // la póliza es opcional: no se pinta como Faltante
    }

    [Fact]
    public async Task Detalle_ExpedienteCompleto_NadieMarca()
    {
        GivenReferencia(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO);

        var html = await HtmlAsync(await ClientAs(RolesEnum.CAPTURADOR).GetAsync(DetalleUrl, _ct));

        html.ShouldNotContain("name=\"Estado\"");
        html.ShouldContain("ya solo se consultan");
    }

    [Fact]
    public async Task Detalle_ReferenciaQueNoExiste_Regresa404()
    {
        var response = await ClientAs(RolesEnum.CAPTURADOR).GetAsync("/Expedientes/Detalle/999", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ------------------------------------------------------------------ Vista previa

    [Fact]
    public async Task Detalle_ConPdf_AbreElDialogoConElIframe()
    {
        GivenReferencia();
        _service.GetPreviewAsync(FacturaId, Arg.Any<CancellationToken>())
            .Returns(new DocumentPreviewDto(FacturaId, ReferenciaId, "factura.pdf", DocumentPreviewKindEnum.Pdf, null));

        var html = await HtmlAsync(await ClientAs(RolesEnum.GLOSADOR).GetAsync($"{DetalleUrl}?documento={FacturaId}", _ct));

        html.ShouldContain("modal show d-block");
        html.ShouldContain($"src=\"/Expedientes/Archivo/{FacturaId}\"");
        html.ShouldContain("loading=\"lazy\"");
    }

    [Fact]
    public async Task Detalle_ConExcel_LoPintaComoTabla()
    {
        GivenReferencia();
        _service.GetPreviewAsync(FacturaId, Arg.Any<CancellationToken>()).Returns(new DocumentPreviewDto(
            FacturaId, ReferenciaId, "lista.xlsx", DocumentPreviewKindEnum.Spreadsheet,
            new SpreadsheetPreviewDto([new SheetPreviewDto("Empaque", [["Caja", "<b>10</b>"]])], Truncated: true)));

        var raw = await (await ClientAs(RolesEnum.CAPTURADOR).GetAsync($"{DetalleUrl}?documento={FacturaId}", _ct))
            .Content.ReadAsStringAsync(_ct);

        raw.ShouldContain("<td>Caja</td>");
        raw.ShouldContain("&lt;b&gt;10&lt;/b&gt;");   // el contenido de la celda nunca se pinta como HTML
        raw.ShouldNotContain("<iframe");
        WebUtility.HtmlDecode(raw).ShouldContain("solo se muestran las primeras");
    }

    [Fact]
    public async Task Detalle_DocumentoDeOtraReferencia_NoAbreElDialogo()
    {
        GivenReferencia();
        _service.GetPreviewAsync(99, Arg.Any<CancellationToken>())
            .Returns(new DocumentPreviewDto(99, ReferenciaId + 1, "ajeno.pdf", DocumentPreviewKindEnum.Pdf, null));

        var html = await HtmlAsync(await ClientAs(RolesEnum.CAPTURADOR).GetAsync($"{DetalleUrl}?documento=99", _ct));

        html.ShouldNotContain("modal show");
    }

    [Fact]
    public async Task Archivo_Pdf_SeSirveInlineSinCache()
    {
        _service.OpenFileAsync(FacturaId, Arg.Any<CancellationToken>()).Returns(
            new DocumentFileDto(new MemoryStream("%PDF-1.7"u8.ToArray()), DocumentPreviewKindEnum.Pdf, "factura.pdf"));

        var response = await ClientAs(RolesEnum.GLOSADOR).GetAsync($"/Expedientes/Archivo/{FacturaId}", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("inline");
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Archivo_Xml_SeSirveComoTextoEnSandbox()
    {
        _service.OpenFileAsync(FacturaId, Arg.Any<CancellationToken>()).Returns(new DocumentFileDto(
            new MemoryStream("<html xmlns=\"http://www.w3.org/1999/xhtml\"><script>alert(1)</script></html>"u8.ToArray()),
            DocumentPreviewKindEnum.Text, "cfdi.xml"));

        var response = await ClientAs(RolesEnum.CAPTURADOR).GetAsync($"/Expedientes/Archivo/{FacturaId}", _ct);

        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
        response.Headers.GetValues("Content-Security-Policy").ShouldBe(["sandbox"]);
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("inline");
    }

    [Fact]
    public async Task Archivo_SinVistaPreviaInline_Regresa404()
    {
        var response = await ClientAs(RolesEnum.CAPTURADOR).GetAsync("/Expedientes/Archivo/5", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ------------------------------------------------------------------ Marcar (POST)

    // GET + POST con la misma sesión: el POST lleva el token del formulario y la cookie de antiforgery de ese GET.
    private async Task<HttpResponseMessage> PostMarcaAsync(string estado, string nota = "")
    {
        var client = ClientAs(role: null);
        var session = $"{AuthCookie.Name}={factory.CreateToken(RolesEnum.CAPTURADOR, UserId).AccessToken}";

        var get = new HttpRequestMessage(HttpMethod.Get, DetalleUrl);
        get.Headers.Add("Cookie", session);
        var page = await client.SendAsync(get, _ct);
        var html = await page.Content.ReadAsStringAsync(_ct);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var antiforgery = page.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]).Single(c => c.StartsWith(".AspNetCore.Antiforgery"));

        var post = new HttpRequestMessage(HttpMethod.Post, DetalleUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["DocumentoMarcado"] = FacturaId.ToString(),
                ["Estado"] = estado,
                ["Nota"] = nota
            })
        };
        post.Headers.Add("Cookie", $"{session}; {antiforgery}");
        return await client.SendAsync(post, _ct);
    }

    [Fact]
    public async Task Marcar_SinAntiforgery_SeRechazaSinMarcar()
    {
        GivenReferencia();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["DocumentoMarcado"] = "11", ["Estado"] = "CORRECTO" });

        var response = await ClientAs(RolesEnum.CAPTURADOR).PostAsync(DetalleUrl, content, _ct);

        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        await _service.DidNotReceiveWithAnyArgs().MarkAsync(default!, default);
    }

    [Fact]
    public async Task Marcar_Glosador_RedirigeAAccesoDenegado()
    {
        GivenReferencia();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["DocumentoMarcado"] = "11", ["Estado"] = "CORRECTO" });

        var response = await ClientAs(RolesEnum.GLOSADOR).PostAsync(DetalleUrl, content, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe("/Auth/Denied");
        await _service.DidNotReceiveWithAnyArgs().MarkAsync(default!, default);
    }

    [Fact]
    public async Task Marcar_Capturador_UsaElUsuarioDelTokenYRegresaAlDetalle()
    {
        GivenReferencia();
        _service.MarkAsync(Arg.Any<ReviewDocumentDto>(), Arg.Any<CancellationToken>())
            .Returns(new ExpedienteStatusDto(ReferenciaId, ReferenciaStatusEnum.EXPEDIENTE_COMPLETO, [], [], true, StatusChanged: true));

        var response = await PostMarcaAsync("CORRECTO");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldStartWith(DetalleUrl);
        await _service.Received(1).MarkAsync(
            new ReviewDocumentDto(ReferenciaId, FacturaId, DocumentStatusEnum.CORRECTO, UserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Marcar_ConObservacion_LaMandaAlServicio()
    {
        GivenReferencia();
        _service.MarkAsync(Arg.Any<ReviewDocumentDto>(), Arg.Any<CancellationToken>())
            .Returns(new ExpedienteStatusDto(ReferenciaId, ReferenciaStatusEnum.EN_REVISION, [], [], false, StatusChanged: false));

        await PostMarcaAsync("ERROR", "Ilegible");

        await _service.Received(1).MarkAsync(
            new ReviewDocumentDto(ReferenciaId, FacturaId, DocumentStatusEnum.ERROR, UserId, "Ilegible"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Marcar_ErrorDeNegocio_VuelveAPintarElDetalleConElMensaje()
    {
        GivenReferencia();
        _service.MarkAsync(Arg.Any<ReviewDocumentDto>(), Arg.Any<CancellationToken>())
            .Returns<ExpedienteStatusDto>(_ => throw new ExpedienteNotInReviewException());

        var response = await PostMarcaAsync("ERROR");
        var html = await HtmlAsync(response);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("ya no está en revisión");
        html.ShouldContain("R-000007");
    }
}
