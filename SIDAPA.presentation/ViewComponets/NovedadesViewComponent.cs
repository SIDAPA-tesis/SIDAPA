using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Models.Core.Notifications;

namespace SIDAPA.presentation.ViewComponets;

/// <summary>
/// Tarjetas de "Novedades": los avisos más recientes del usuario en sesión (los que guarda
/// <see cref="INotificationService"/>). Vista: Views/Shared/Components/Novedades/Default.cshtml.
/// </summary>
public class NovedadesViewComponent(INotificationService notificaciones, TimeProvider clock) : ViewComponent
{
    public const int Maximo = 6;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!int.TryParse(UserClaimsPrincipal.FindFirst("sub")?.Value, out var userId))
            return View(NovedadesViewModel.Vacio);

        var recientes = await notificaciones.GetRecentAsync(userId, Maximo, HttpContext.RequestAborted);

        return View(NovedadesViewModel.Desde(recientes, clock.LocalTimeZone));
    }
}
