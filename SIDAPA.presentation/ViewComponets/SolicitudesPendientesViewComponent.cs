using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.presentation.Models.CaseFile.Documents;

namespace SIDAPA.presentation.ViewComponets;

/// <summary>
/// Tarjeta "Documentos que te pedimos" (HU-05 / SCRUM-64): el aviso de documento faltante con su botón Subir.
/// Obtiene sus propios datos, por eso es ViewComponent y no partial.
/// Vista: Views/Shared/Components/SolicitudesPendientes/Default.cshtml.
/// </summary>
public class SolicitudesPendientesViewComponent(IDocumentRequestService solicitudes, TimeProvider clock) : ViewComponent
{
    /// <summary>TempData con la confirmación que deja <c>SolicitudesController</c> después de subir.</summary>
    public const string RecibidoKey = "DocumentoRecibido";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!int.TryParse(UserClaimsPrincipal.FindFirst("client_id")?.Value, out var clientId))
            return Content(string.Empty);

        var pendientes = await solicitudes.GetPendingAsync(clientId, HttpContext.RequestAborted);
        var recibido = TempData[RecibidoKey] as string;

        return View(SolicitudesPendientesViewModel.Desde(pendientes, recibido, clock.LocalTimeZone));
    }
}
