using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SIDAPA.application.DTOs.Core.Auth;
using SIDAPA.domain.Enums.Core;
using SIDAPA.infraestructure.Security;

namespace SIDAPA.infraestructure.test.Security;

/// <summary>
/// Token que emite <see cref="JwtTokenService"/>: claims cortos que espera Program.cs (MapInboundClaims = false,
/// RoleClaimType = "role"), firma HS256 y vigencia. Reemplaza a DevAuthController (/api/dev/auth/token).
/// </summary>
public class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "SIDAPA",
        Audience = "SIDAPA",
        Key = "clave-solo-para-pruebas-de-sidapa-0123456789",
        ExpiresMinutes = 30
    };

    private readonly JwtTokenService _service = new(Options);

    private static JsonWebToken Read(AuthToken token) => new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);

    [Fact]
    public void CreateToken_IncluyeLosClaimsQueUsaLaApp()
    {
        var token = Read(_service.CreateToken(new UserSessionDto(42, "Ana Pérez", "ana@sidapa.mx", RolesEnum.GLOSADOR, null)));

        token.GetClaim("sub").Value.ShouldBe("42");
        token.GetClaim("name").Value.ShouldBe("Ana Pérez");
        token.GetClaim("email").Value.ShouldBe("ana@sidapa.mx");
        token.GetClaim("role").Value.ShouldBe(nameof(RolesEnum.GLOSADOR));   // nombre exacto del enum: las políticas distinguen mayúsculas
        token.GetClaim("jti").Value.ShouldNotBeNullOrWhiteSpace();
        token.Issuer.ShouldBe("SIDAPA");
        token.Audiences.ShouldBe(["SIDAPA"]);
    }

    [Fact]
    public void CreateToken_UsuarioCliente_IncluyeClientId()
    {
        var token = Read(_service.CreateToken(new UserSessionDto(7, "Cliente", "c@empresa.mx", RolesEnum.CLIENTE, ClientId: 15)));

        token.GetClaim("client_id").Value.ShouldBe("15");
    }

    [Fact]
    public void CreateToken_PersonalInterno_NoIncluyeClientId()
    {
        var token = Read(_service.CreateToken(new UserSessionDto(7, "Capturador", "c@sidapa.mx", RolesEnum.CAPTURADOR, null)));

        token.TryGetClaim("client_id", out _).ShouldBeFalse();
    }

    [Fact]
    public void CreateToken_VenceSegunLaConfiguracion()
    {
        var before = DateTime.UtcNow;

        var result = _service.CreateToken(new UserSessionDto(1, "U", "u@sidapa.mx", RolesEnum.CAPTURADOR, null));

        result.ExpiresAtUtc.ShouldBeInRange(before.AddMinutes(30).AddSeconds(-1), DateTime.UtcNow.AddMinutes(30).AddSeconds(1));
        Read(result).ValidTo.ShouldBe(result.ExpiresAtUtc, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateToken_DosTokensDelMismoUsuario_TienenJtiDistinto()
    {
        var user = new UserSessionDto(1, "U", "u@sidapa.mx", RolesEnum.CAPTURADOR, null);

        Read(_service.CreateToken(user)).Id.ShouldNotBe(Read(_service.CreateToken(user)).Id);
    }

    [Fact]
    public async Task CreateToken_EsValidoConLaMismaLlaveYNoConOtra()
    {
        var token = _service.CreateToken(new UserSessionDto(1, "U", "u@sidapa.mx", RolesEnum.CAPTURADOR, null)).AccessToken;
        var handler = new JsonWebTokenHandler();

        var ok = await handler.ValidateTokenAsync(token, Parameters(Options.Key));
        var forged = await handler.ValidateTokenAsync(token, Parameters("otra-llave-que-no-es-la-de-sidapa-0123456789"));

        ok.IsValid.ShouldBeTrue();
        forged.IsValid.ShouldBeFalse();
    }

    // Igual que TokenValidationParameters en Program.cs.
    private static TokenValidationParameters Parameters(string key) => new()
    {
        ValidIssuer = "SIDAPA",
        ValidAudience = "SIDAPA",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
}
