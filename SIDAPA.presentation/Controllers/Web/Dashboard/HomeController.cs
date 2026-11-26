using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Models.Dashboard;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    public class HomeController(IPedimentoService pedimentos, IAuthorizationService autorizacion) : Controller
    {
        // Inicio de todos los roles. El resumen de pedimentos solo se arma para quien puede entrar a Pedimentos
        // (la misma política Staff que protege PedimentosController).
        public async Task<IActionResult> index(CancellationToken ct)
        {
            ResumenPedimentosViewModel? resumen = null;

            if ((await autorizacion.AuthorizeAsync(User, Policies.Staff)).Succeeded)
            {
                var panel = await pedimentos.ObtenerPanelAsync(null, null, ct);
                resumen = ResumenPedimentosViewModel.Desde(panel.Pedimentos);
            }

            return View(HomeViewModel.Desde(User, resumen));
        }
    }
}
