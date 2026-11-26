using System.Net;
using System.Text.RegularExpressions;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.test.Security;

/// <summary>
/// El servidor protege las pantallas que el menú lateral esconde (docs/permisos-por-rol.md): Usuarios y Reportes
/// (Administration) y Productos (Glosa). Antes no tenían política y cualquier usuario con sesión entraba por URL.
/// Las pantallas no tocan la BD, así que el "sí entra" se puede afirmar con 200.
/// </summary>
public class DashboardRoutesTests(SidapaWebFactory factory) : IClassFixture<SidapaWebFactory>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/Usuarios", RolesEnum.CAPTURADOR)]
    [InlineData("/Usuarios", RolesEnum.GLOSADOR)]
    [InlineData("/Usuarios", RolesEnum.CLIENTE)]
    [InlineData("/Reportes", RolesEnum.CAPTURADOR)]
    [InlineData("/Reportes", RolesEnum.VALIDADOR)]
    [InlineData("/Productos", RolesEnum.CAPTURADOR)]
    [InlineData("/Productos", RolesEnum.CLIENTE)]
    public async Task Pantalla_RolSinPermiso_RedirigeAAccesoDenegado(string ruta, RolesEnum rol)
    {
        var response = await factory.CreateClientAs(rol).GetAsync(ruta, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe("/Auth/Denied");
    }

    [Theory]
    [InlineData("/Usuarios", RolesEnum.ADMINISTRADOR)]
    [InlineData("/Reportes", RolesEnum.ADMINISTRADOR)]
    [InlineData("/Productos", RolesEnum.GLOSADOR)]
    public async Task Pantalla_RolConPermiso_Entra(string ruta, RolesEnum rol)
    {
        var response = await factory.CreateClientAs(rol).GetAsync(ruta, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MenuLateral_Glosador_MuestraSoloSusModulosYMarcaLaPantallaActual()
    {
        var html = await factory.CreateClientAs(RolesEnum.GLOSADOR).GetStringAsync("/Productos", _ct);

        html.ShouldContain("href=\"/Pedimentos\"");
        html.ShouldContain("href=\"/Claves\"");
        Regex.Matches(html, "<a[^>]*aria-current=\"page\"[^>]*>").ShouldHaveSingleItem().Value.ShouldContain("href=\"/Productos\"");
        html.ShouldNotContain("href=\"/Usuarios\"");
        html.ShouldNotContain("href=\"/Reportes\"");
        html.ShouldNotContain("href=\"/Avisos\"");
    }
}
