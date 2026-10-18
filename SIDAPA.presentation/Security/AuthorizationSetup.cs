using Microsoft.AspNetCore.Authorization;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Security;

public static class AuthorizationSetup
{
    private const string Administrador = nameof(RolesEnum.ADMINISTRADOR);
    private const string Capturador = nameof(RolesEnum.CAPTURADOR);
    private const string Glosador = nameof(RolesEnum.GLOSADOR);
    private const string Cliente = nameof(RolesEnum.CLIENTE);

    public static IServiceCollection AddSidapaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.ClientPortal, p => p.RequireRole(Cliente))
            .AddPolicy(Policies.Staff, p => p.RequireRole(Capturador, Glosador, Administrador))
            .AddPolicy(Policies.Capture, p => p.RequireRole(Capturador, Administrador))
            .AddPolicy(Policies.Glosa, p => p.RequireRole(Glosador, Administrador))
            .AddPolicy(Policies.Administration, p => p.RequireRole(Administrador));

        return services;
    }
}