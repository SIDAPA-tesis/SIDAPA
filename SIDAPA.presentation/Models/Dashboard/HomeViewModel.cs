using System.Security.Claims;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Dashboard;

/// <summary>
/// Inicio del dashboard: saludo, resumen de pedimentos (solo personal; <c>null</c> para el resto) y si se muestra el
/// panel de ejemplo del administrador.
/// </summary>
public record HomeViewModel(string Nombre, bool EsAdministrador, ResumenPedimentosViewModel? Pedimentos = null)
{
    public static HomeViewModel Desde(ClaimsPrincipal usuario, ResumenPedimentosViewModel? pedimentos = null) => new(
        string.IsNullOrWhiteSpace(usuario.Identity?.Name) ? "Usuario" : usuario.Identity.Name,
        usuario.IsInRole(nameof(RolesEnum.ADMINISTRADOR)),
        pedimentos);
}
