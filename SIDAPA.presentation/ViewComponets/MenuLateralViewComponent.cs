using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Models.Core.Permissions;

namespace SIDAPA.presentation.ViewComponets;

/// <summary>
/// Opciones del menú lateral según el rol de quien está en sesión. Qué ve cada rol lo decide
/// <see cref="IPermisosService"/>; cómo se pinta cada módulo, <see cref="ModuloCatalogo"/>.
/// Vista: Views/Shared/Components/MenuLateral/Default.cshtml.
/// </summary>
public class MenuLateralViewComponent(IUserActual usuario, IPermisosService permisos) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var actual = ViewContext.RouteData.Values["controller"]?.ToString();
        var modulos = permisos.ModulosPara(usuario.Roles);

        return View(MenuLateralViewModel.Desde(usuario.Nombre, usuario.Roles, modulos, actual));
    }
}
