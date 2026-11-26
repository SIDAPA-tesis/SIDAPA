using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Models.Core.Permissions;

namespace SIDAPA.presentation.ViewComponets;

/// <summary>
/// Tarjetas de "Accesos rápidos": un acceso por cada módulo del rol (los mismos del menú lateral, sin Inicio).
/// Vista: Views/Shared/Components/AccesosRapidos/Default.cshtml.
/// </summary>
public class AccesosRapidosViewComponent(IUserActual usuario, IPermisosService permisos) : ViewComponent
{
    public IViewComponentResult Invoke() =>
        View(AccesosRapidosViewModel.Desde(permisos.ModulosPara(usuario.Roles)));
}
