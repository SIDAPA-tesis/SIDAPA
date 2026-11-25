using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Security;
using static SIDAPA.domain.Enums.Core.RolesEnum;

namespace SIDAPA.presentation.test.Security;

/// <summary>
/// Matriz completa política × rol de docs/permisos-por-rol.md, evaluada con las políticas reales registradas
/// en Program.cs (AddSidapaAuthorization). Reemplaza las rutas manuales /dev/permisos/*.
/// </summary>
public class AuthorizationPolicyTests(SidapaWebFactory factory) : IClassFixture<SidapaWebFactory>
{
    public static TheoryData<string, RolesEnum, bool> Matrix()
    {
        var allowed = new Dictionary<string, RolesEnum[]>
        {
            [Policies.ClientPortal] = [CLIENTE],
            [Policies.Staff] = [CAPTURADOR, GLOSADOR, ADMINISTRADOR],
            [Policies.Capture] = [CAPTURADOR, ADMINISTRADOR],
            [Policies.Glosa] = [GLOSADOR, ADMINISTRADOR],
            [Policies.Administration] = [ADMINISTRADOR]
        };

        // Todas las combinaciones, incluido VALIDADOR (todavía sin política: no debe entrar a nada).
        var data = new TheoryData<string, RolesEnum, bool>();
        foreach (var (policy, roles) in allowed)
            foreach (var role in Enum.GetValues<RolesEnum>())
                data.Add(policy, role, roles.Contains(role));
        return data;
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Policy_PermiteSoloALosRolesDeLaMatriz(string policy, RolesEnum role, bool expected)
    {
        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(UserWithRole(role.ToString()), policy);

        result.Succeeded.ShouldBe(expected, $"{role} en {policy}");
    }

    [Theory]
    [InlineData("glosador")]      // la comparación del rol distingue mayúsculas
    [InlineData("Glosador")]
    public async Task Policy_RolConOtrasMayusculas_NoEntra(string role)
    {
        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(UserWithRole(role), Policies.Glosa);

        result.Succeeded.ShouldBeFalse();
    }

    // Misma forma que el principal que arma JwtBearer: claims cortos ("role", "name") por MapInboundClaims = false.
    private static ClaimsPrincipal UserWithRole(string role) =>
        new(new ClaimsIdentity([new Claim("sub", "1"), new Claim("role", role)], "Test", "name", "role"));
}
