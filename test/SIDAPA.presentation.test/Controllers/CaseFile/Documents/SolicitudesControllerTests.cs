using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;
using SIDAPA.presentation.Controllers.Web.CaseFile.Documents;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.test.Controllers.CaseFile.Documents;

/// <summary>
/// <see cref="SolicitudesController"/> (SCRUM-64 · HU-05): solo el Cliente entra, el cliente sale del token (no de la URL),
/// una solicitud no disponible se explica en la misma página y el POST exige antiforgery. Las reglas de negocio
/// (tipo de la solicitud, cierre, estatus, aviso) se prueban en application e infraestructura.
/// </summary>
public class SolicitudesControllerTests(SidapaWebFactory factory) : IClassFixture<SidapaWebFactory>, IDisposable
{
    private const int RequestId = 5;
    private const int ClientId = 10;
    private const int UserId = 21;
    private const string Url = "/Solicitudes/Subir?requestId=5";

    private readonly IDocumentRequestService _service = Substitute.For<IDocumentRequestService>();
    private readonly List<WebApplicationFactory<Program>> _apps = [];
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static DocumentRequestDetailDto Detalle() => new(
        RequestId, ReferenciaId: 7, "R-000007", ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS,
        DocumentTypeEnum.LISTA_EMPAQUE, "Lista de empaque", "Falta la lista de empaque", RequestedByUserId: 3,
        new DateTime(2026, 11, 24, 18, 0, 0), IsAttended: false);

    /// <summary>La app con el servicio sustituido (la BD de pruebas no existe) y la sesión del rol indicado.</summary>
    private HttpClient ClientAs(RolesEnum? role)
    {
        var app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IDocumentRequestService>();
            s.AddSingleton(_service);
        }));
        _apps.Add(app);

        var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        if (role is { } r)
            client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={factory.CreateToken(r, UserId, ClientId).AccessToken}");
        return client;
    }

    public void Dispose()
    {
        foreach (var app in _apps)
            app.Dispose();
    }

    [Fact]
    public async Task Subir_SinSesion_RedirigeAlLogin()
    {
        var response = await ClientAs(role: null).GetAsync(Url, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldStartWith("/Auth/Login");
    }

    [Theory]
    [InlineData(RolesEnum.CAPTURADOR)]
    [InlineData(RolesEnum.ADMINISTRADOR)]
    public async Task Subir_PersonalInterno_RedirigeAAccesoDenegado(RolesEnum role)
    {
        var response = await ClientAs(role).GetAsync(Url, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe("/Auth/Denied");
        _service.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Subir_ClienteConSolicitudAbierta_MuestraQueDocumentoFaltaYElFormulario()
    {
        _service.GetForUploadAsync(RequestId, ClientId, Arg.Any<CancellationToken>()).Returns(Detalle());

        var response = await ClientAs(RolesEnum.CLIENTE).GetAsync(Url, _ct);
        var html = await response.Content.ReadAsStringAsync(_ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Lista de empaque");
        html.ShouldContain("R-000007");
        html.ShouldContain("type=\"file\"");
        html.ShouldContain("__RequestVerificationToken");
        // el cliente sale del token: la URL solo trae el id de la solicitud
        await _service.Received(1).GetForUploadAsync(RequestId, ClientId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Subir_SolicitudYaAtendida_LoExplicaSinFormulario()
    {
        _service.GetForUploadAsync(RequestId, ClientId, Arg.Any<CancellationToken>())
            .Returns<DocumentRequestDetailDto>(_ => throw new DocumentRequestAlreadyAttendedException());

        var response = await ClientAs(RolesEnum.CLIENTE).GetAsync(Url, _ct);
        var html = await response.Content.ReadAsStringAsync(_ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Ya recibimos este documento");
        html.ShouldNotContain("type=\"file\"");
    }

    [Fact]
    public async Task SubirPost_SinAntiforgery_SeRechazaSinAtender()
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(RequestId.ToString()), "RequestId" },
            { new ByteArrayContent("%PDF-1.4"u8.ToArray()), "Archivo", "lista.pdf" }
        };

        var response = await ClientAs(RolesEnum.CLIENTE).PostAsync("/Solicitudes/Subir", form, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await _service.DidNotReceive().AttendAsync(Arg.Any<AttendDocumentRequestDto>(), Arg.Any<CancellationToken>());
    }
}
