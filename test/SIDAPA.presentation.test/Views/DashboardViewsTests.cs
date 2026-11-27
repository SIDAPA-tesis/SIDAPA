using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.application.DTOs.Core.Pedimento;
using SIDAPA.application.Interface.CaseFile.Expediente;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.test.Views;

/// <summary>
/// Pantallas del dashboard que pasaron a Bootstrap + una hoja propia (skill sidapa-frontend): se renderizan
/// completas y cada hoja de estilos que piden existe. Antes Pedimentos y Claves cargaban hojas inexistentes
/// (css/componentes/…) y salían sin estilos sin que nada fallara.
/// </summary>
public class DashboardViewsTests(SidapaWebFactory factory) : IClassFixture<SidapaWebFactory>, IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 11, 25);

    private readonly IPedimentoService _pedimentos = Substitute.For<IPedimentoService>();
    private readonly IClaveService _claves = Substitute.For<IClaveService>();
    private readonly INotificationService _notificaciones = Substitute.For<INotificationService>();
    private readonly IExpedienteReviewService _expedientes = Substitute.For<IExpedienteReviewService>();
    private readonly List<WebApplicationFactory<Program>> _apps = [];
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private HttpClient ClientAs(RolesEnum role)
    {
        _pedimentos.ObtenerPanelAsync(Arg.Any<string?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new PanelPedimentosDto(
                [
                    new PedimentoDto(1, "26 47 3456 6000123", "Importadora Norte", null, 40, Hoy.AddDays(-1), UrgenciaEnum.Vencido),
                    new PedimentoDto(2, "26 47 3456 6000124", "Comercial Sur", null, 80, Hoy.AddDays(3), UrgenciaEnum.Proximo),
                ],
                new DateOnly(2026, 11, 1), Hoy));
        _claves.ObtenerTableroAsync(Arg.Any<FiltroClaves>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var clave = new ClaveDto("A1", "Importación definitiva", "Claves de pedimento", null, null, true, Hoy);
                return new TableroClavesDto(new MetricasClavesDto(1, 0), [clave], call.ArgAt<string?>(1) == "A1" ? clave : null);
            });
        _notificaciones.GetRecentAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _expedientes.SearchAsync(Arg.Any<ExpedienteFilterDto>(), Arg.Any<CancellationToken>()).Returns([]);
        _expedientes.GetAsync(1, Arg.Any<CancellationToken>()).Returns(new ExpedienteReviewDto(
            new ExpedienteDetailDto(1, "R-000001", "Importadora Norte", "AICM", "Importación", ReferenciaStatusEnum.RECIBIDA, "Recibida",
                null, null, new DateTime(2026, 11, 23, 16, 0, 0), [], []),
            [], CanBeReviewed: true));

        var app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IPedimentoService>();
            s.AddSingleton(_pedimentos);
            s.RemoveAll<IClaveService>();
            s.AddSingleton(_claves);
            s.RemoveAll<INotificationService>();
            s.AddSingleton(_notificaciones);
            s.RemoveAll<IExpedienteReviewService>();
            s.AddSingleton(_expedientes);
        }));
        _apps.Add(app);

        var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={factory.CreateToken(role).AccessToken}");
        return client;
    }

    [Theory]
    [InlineData("/Home", RolesEnum.ADMINISTRADOR)]
    [InlineData("/Home", RolesEnum.CAPTURADOR)]
    [InlineData("/Pedimentos", RolesEnum.CAPTURADOR)]
    [InlineData("/Pedimentos/Detalle/1", RolesEnum.CAPTURADOR)]
    [InlineData("/Claves?clave=A1", RolesEnum.GLOSADOR)]
    [InlineData("/Usuarios", RolesEnum.ADMINISTRADOR)]
    [InlineData("/Reportes", RolesEnum.ADMINISTRADOR)]
    [InlineData("/Expedientes", RolesEnum.CAPTURADOR)]
    [InlineData("/Expedientes/Detalle/1", RolesEnum.GLOSADOR)]
    public async Task Pantalla_TodasSusHojasDeEstiloExisten(string ruta, RolesEnum rol)
    {
        _pedimentos.ObtenerAsync(1, Arg.Any<CancellationToken>())
            .Returns(new PedimentoDto(1, "26 47 3456 6000123", "Importadora Norte", null, 40, Hoy, UrgenciaEnum.EnTiempo));
        var client = ClientAs(rol);

        var html = await client.GetStringAsync(ruta, _ct);

        var hojas = Regex.Matches(html, "<link rel=\"stylesheet\" href=\"([^\"]+)\"").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
        hojas.ShouldNotBeEmpty();
        foreach (var hoja in hojas)
        {
            var response = await client.GetAsync(hoja, _ct);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{ruta} pide {hoja}");
        }
    }

    [Fact]
    public async Task Pedimentos_Capturador_CargaUnaSolaHojaPropiaYPintaLaUrgencia()
    {
        var html = await GetHtmlAsync(RolesEnum.CAPTURADOR, "/Pedimentos");

        Regex.Matches(html, "href=\"/css/vistas/").Count.ShouldBe(1);
        html.ShouldContain("/css/vistas/pedimentos.css");
        html.ShouldContain("page-content page-content--ancha");
        html.ShouldContain("text-bg-danger\">Vencido");
        html.ShouldContain("text-bg-warning\">Próximo");
    }

    [Fact]
    public async Task Claves_ConClaveSeleccionada_AbreElPanelDeDetalleSinJs()
    {
        var html = await GetHtmlAsync(RolesEnum.GLOSADOR, "/Claves?clave=A1");

        html.ShouldContain("offcanvas offcanvas-end show");
        html.ShouldContain("Importación definitiva");
    }

    [Theory]
    [InlineData(RolesEnum.ADMINISTRADOR, true)]
    [InlineData(RolesEnum.CAPTURADOR, false)]
    public async Task Inicio_SoloElAdministradorVeElPanelDeIndicadores(RolesEnum rol, bool loVe)
    {
        var html = await GetHtmlAsync(rol, "/Home");

        html.ShouldContain($"Bienvenido, Usuario {rol}");
        html.Contains("Indicadores principales").ShouldBe(loVe);
    }

    [Fact]
    public async Task Inicio_Capturador_MuestraElResumenDeSusPedimentos()
    {
        var html = await GetHtmlAsync(RolesEnum.CAPTURADOR, "/Home");

        html.ShouldContain("Pedimentos por urgencia");
        html.ShouldContain("Próximos a vencer");
        html.ShouldContain("href=\"/Pedimentos/Detalle/1\"");   // el vencido sale en la lista
        html.ShouldContain("Ver todos (2)");
    }

    [Fact]
    public async Task Inicio_RolSinPedimentos_NoConsultaNiMuestraElResumen()
    {
        var html = await GetHtmlAsync(RolesEnum.VALIDADOR, "/Home");

        html.ShouldNotContain("Pedimentos por urgencia");
        await _pedimentos.DidNotReceiveWithAnyArgs().ObtenerPanelAsync(default, default, default);
    }

    /// <summary>HTML ya decodificado: Razor escribe los acentos como entidades (Próximo → Pr&amp;#xF3;ximo).</summary>
    private async Task<string> GetHtmlAsync(RolesEnum rol, string ruta) =>
        WebUtility.HtmlDecode(await ClientAs(rol).GetStringAsync(ruta, _ct));

    public void Dispose()
    {
        foreach (var app in _apps)
            app.Dispose();
    }
}
