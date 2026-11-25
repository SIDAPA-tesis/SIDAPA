using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.test.Security;

/// <summary>
/// Pipeline real de autenticación: el token viaja en la cookie <c>sidapa_token</c>, se valida (firma, emisor,
/// vigencia) y las pantallas responden con redirección a login o a acceso denegado.
/// Reemplaza a DevAuthController (/api/dev/auth/*) y a las rutas /dev/permisos/*.
/// Se usa /Claves (política Glosa) porque rechaza antes de llegar a la BD.
/// </summary>
public class AuthCookieTests(SidapaWebFactory factory) : IClassFixture<SidapaWebFactory>
{
    private const string ProtectedPage = "/Claves";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SinSesion_RedirigeAlLoginConReturnUrl()
    {
        var response = await factory.CreateAnonymousClient().GetAsync(ProtectedPage, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe("/Auth/Login?returnUrl=%2FClaves");
    }

    [Fact]
    public async Task ConSesionSinPermiso_RedirigeAAccesoDenegado()
    {
        var response = await factory.CreateClientAs(RolesEnum.CAPTURADOR).GetAsync(ProtectedPage, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe("/Auth/Denied");
    }

    [Fact]
    public async Task TokenFirmadoConOtraLlave_SeTrataComoSinSesion()
    {
        var forged = Token(key: "otra-llave-que-no-es-la-de-sidapa-0123456789", expires: DateTime.UtcNow.AddMinutes(30));

        var response = await WithCookie(forged).GetAsync(ProtectedPage, _ct);

        response.Headers.Location!.OriginalString.ShouldStartWith("/Auth/Login");
    }

    [Fact]
    public async Task TokenVencido_SeTrataComoSinSesion()
    {
        // Vencido hace 5 minutos: más que el ClockSkew de 1 minuto de Program.cs.
        var expired = Token(key: SidapaWebFactory.JwtKey, expires: DateTime.UtcNow.AddMinutes(-5));

        var response = await WithCookie(expired).GetAsync(ProtectedPage, _ct);

        response.Headers.Location!.OriginalString.ShouldStartWith("/Auth/Login");
    }

    [Fact]
    public async Task TokenAPuntoDeVencer_EntregaUnaCookieNueva()
    {
        // Le quedan 5 de 30 minutos: menos de la mitad de su vida → TokenRenewalMiddleware lo renueva.
        var aging = Token(key: SidapaWebFactory.JwtKey, expires: DateTime.UtcNow.AddMinutes(5), issuedAt: DateTime.UtcNow.AddMinutes(-25));

        var response = await WithCookie(aging).GetAsync(ProtectedPage, _ct);

        var setCookie = response.Headers.GetValues("Set-Cookie").ShouldHaveSingleItem();
        setCookie.ShouldStartWith($"{AuthCookie.Name}=");
        setCookie.ShouldContain("httponly", Case.Insensitive);
        setCookie.ShouldContain("secure", Case.Insensitive);
        setCookie.ShouldContain("samesite=strict", Case.Insensitive);
    }

    [Fact]
    public async Task TokenRecien_NoSeRenueva()
    {
        var response = await factory.CreateClientAs(RolesEnum.CAPTURADOR).GetAsync(ProtectedPage, _ct);

        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }

    private HttpClient WithCookie(string token)
    {
        var client = factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={token}");
        return client;
    }

    // Mismos claims y emisor que JwtTokenService, pero con llave y fechas controladas por la prueba.
    private static string Token(string key, DateTime expires, DateTime? issuedAt = null)
    {
        var iat = issuedAt ?? expires.AddMinutes(-30);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", "1"), new Claim("name", "Prueba"), new Claim("email", "prueba@sidapa.test"),
                new Claim("role", nameof(RolesEnum.CAPTURADOR))
            ]),
            Issuer = "SIDAPA",
            Audience = "SIDAPA",
            IssuedAt = iat,
            NotBefore = iat,
            Expires = expires,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        });
    }
}
