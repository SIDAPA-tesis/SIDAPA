using System.Security.Claims;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Dashboard;

/// <summary>Inicio del dashboard: saludo y si se muestra el panel (de ejemplo) del administrador.</summary>
public record HomeViewModel(string Nombre, bool EsAdministrador)
{
    public static HomeViewModel Desde(ClaimsPrincipal usuario) => new(
        string.IsNullOrWhiteSpace(usuario.Identity?.Name) ? "Usuario" : usuario.Identity.Name,
        usuario.IsInRole(nameof(RolesEnum.ADMINISTRADOR)));
}
