using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.Dev;

namespace SIDAPA.presentation.Controllers.Web.Dev;

/// <summary>Página para probar el servicio de avisos (SCRUM-59). Solo responde en Development.</summary>
[Route("dev/avisos")]
public class DevNotificationsController(IWebHostEnvironment env, INotificationService notifications) : Controller
{
    // El id del usuario que inició sesión viene en el claim "sub" del token.
    private int CurrentUserId => int.Parse(User.FindFirst("sub")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        return View(await LoadAsync(new DevNotificationViewModel(), ct));
    }

    /// <summary>Se manda un aviso a sí mismo.</summary>
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> Index(DevNotificationViewModel model, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        await notifications.NotifyUserAsync(CurrentUserId, model.Message ?? "", sendEmail: model.SendEmail, ct: ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/leida")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        await notifications.MarkAsReadAsync(CurrentUserId, id, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<DevNotificationViewModel> LoadAsync(DevNotificationViewModel model, CancellationToken ct)
    {
        model.Notifications = await notifications.GetRecentAsync(CurrentUserId, ct: ct);
        model.UnreadCount = await notifications.CountUnreadAsync(CurrentUserId, ct);
        return model;
    }
}