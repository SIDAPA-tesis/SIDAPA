using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.domain.Rules.Core;
using SIDAPA.presentation.Controllers.Web.CaseFile.ArrivalNotice;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.CaseFile.Documents;
using SIDAPA.presentation.Security;
using SIDAPA.presentation.ViewComponets;

namespace SIDAPA.presentation.Controllers.Web.CaseFile.Documents;

/// <summary>
/// Portal del Cliente: subir el documento que le pidió el Capturador (HU-05 / SCRUM-64).
/// Se entra desde la tarjeta "Documentos que te pedimos" de Mis avisos de arribo.
/// </summary>
[Authorize(Policy = Policies.ClientPortal)]
public class SolicitudesController(IDocumentRequestService solicitudes, TimeProvider clock) : Controller
{
    // Un archivo del tamaño máximo más 1 MB para el resto del formulario. El archivo se valida aparte en application.
    private const long RequestLimit = DocumentFileRule.maxSizeInBytes + 1L * 1024 * 1024;

    // El cliente y el usuario salen siempre del token, nunca del formulario.
    private int? ClientId => int.TryParse(User.FindFirst("client_id")?.Value, out var id) ? id : null;
    private int? UserId => int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;

    /// <summary>GET /Solicitudes/Subir?requestId=5. Si la solicitud ya no está disponible, la vista dice por qué.</summary>
    [HttpGet]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> Subir(SubirSolicitudViewModel model, CancellationToken ct)
    {
        if (ClientId is not { } clientId)
            return Forbid();

        ModelState.Clear();   // en el GET todavía no se eligió archivo: no es un error
        model.Completar(await solicitudes.GetForUploadAsync(model.RequestId, clientId, ct), clock.LocalTimeZone);
        return View(model);
    }

    [HttpPost]
    [ActionName(nameof(Subir))]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> SubirPost(SubirSolicitudViewModel model, CancellationToken ct)
    {
        if (ClientId is not { } clientId || UserId is not { } userId)
            return Forbid();

        // Antes de cualquier error: si el filtro vuelve a pintar la vista, ya trae los datos de la solicitud.
        model.Completar(await solicitudes.GetForUploadAsync(model.RequestId, clientId, ct), clock.LocalTimeZone);

        if (!ModelState.IsValid)
            return View(nameof(Subir), model);

        // El stream de IFormFile lo cierra ASP.NET Core al terminar la petición.
        var atendida = await solicitudes.AttendAsync(new AttendDocumentRequestDto(
            model.RequestId, clientId, userId, model.Archivo!.FileName, model.Archivo.Length, model.Archivo.OpenReadStream()), ct);

        TempData[SolicitudesPendientesViewComponent.RecibidoKey] = SubirSolicitudViewModel.Confirmacion(atendida);
        return RedirectToAction(nameof(AvisosController.Index), "Avisos");
    }
}
