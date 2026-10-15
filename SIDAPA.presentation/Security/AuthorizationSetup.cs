using Microsoft.AspNetCore.Authorization;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.presentation.Security;

public static class AuthorizationSetup
{
    public static IServiceCollection AddSidapaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Todo lo que no diga otra cosa requiere sesión iniciada.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.ClientPortal, p => p.RequireRole(RoleNames.Cliente))
            .AddPolicy(Policies.Staff, p => p.RequireRole(RoleNames.Capturador, RoleNames.Glosador, RoleNames.Administrador))
            .AddPolicy(Policies.Capture, p => p.RequireRole(RoleNames.Capturador, RoleNames.Administrador))
            .AddPolicy(Policies.Glosa, p => p.RequireRole(RoleNames.Glosador, RoleNames.Administrador))
            .AddPolicy(Policies.Administration, p => p.RequireRole(RoleNames.Administrador));

        return services;
    }
}