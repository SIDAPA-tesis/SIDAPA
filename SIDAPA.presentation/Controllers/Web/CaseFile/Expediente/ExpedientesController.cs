using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.Interface.CaseFile.Expediente;
using SIDAPA.domain.Enums.CaseFile;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.CaseFile.Expediente;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.Controllers.Web.CaseFile.Expediente;

/// <summary>
/// HU-03 · revisar si el expediente está completo (SCRUM-55). Capturador, Glosador y Administrador consultan;
/// solo Capturador y Administrador (política Capture) marcan los documentos.
/// </summary>
[Authorize(Policy = Policies.Staff)]
public class ExpedientesController(
    IExpedienteReviewService expedientes, IAuthorizationService authorization, TimeProvider clock) : Controller
{
    [TempData] public string? Mensaje { get; set; }

    private int? UserId => int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, bool porRevisar, int pagina = 1, CancellationToken ct = default)
    {
        var referencias = await expedientes.SearchAsync(new ExpedienteFilterDto(q, porRevisar, pagina), ct);
        return View(BandejaExpedientesViewModel.Desde(referencias, q, porRevisar, clock.LocalTimeZone));
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(ExpedienteDetalleViewModel model, CancellationToken ct)
    {
        if (!await CompletarAsync(model, ct))
            return NotFound();

        model.Mensaje = Mensaje;
        return View(model);
    }

    [HttpPost]
    [ActionName(nameof(Detalle))]
    [Authorize(Policy = Policies.Capture)]
    [ValidateAntiForgeryToken]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> DetallePost(ExpedienteDetalleViewModel model, CancellationToken ct)
    {
        if (UserId is not { } userId)
            return Forbid();

        // Antes de marcar: si el filtro vuelve a pintar la vista, ya trae el expediente.
        if (!await CompletarAsync(model, ct))
            return NotFound();

        var resultado = await expedientes.MarkAsync(
            new ReviewDocumentDto(model.Id, model.DocumentoMarcado, model.Estado, userId, model.Nota), ct);

        Mensaje = ExpedienteDetalleViewModel.Confirmacion(resultado, model.Estado);
        return RedirectToAction(nameof(Detalle), new { id = model.Id, documento = model.Documento });
    }

    /// <summary>
    /// PDF o XML para el iframe de la vista previa: <c>inline</c>, nunca como descarga. El XML se manda como texto
    /// plano y con <c>CSP: sandbox</c>: como application/xml, un XML con XHTML y &lt;script&gt; correría en el origen
    /// de SIDAPA (XSS con la sesión del Capturador).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Archivo(int id, CancellationToken ct)
    {
        var file = await expedientes.OpenFileAsync(id, ct);
        if (file is null)
            return NotFound();

        Response.Headers.ContentDisposition =
            new ContentDispositionHeaderValue("inline") { FileNameStar = file.FileName }.ToString();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, no-store";

        if (file.Kind == DocumentPreviewKindEnum.Text)
        {
            Response.Headers.ContentSecurityPolicy = "sandbox";
            return File(file.Content, "text/plain; charset=utf-8");
        }

        return File(file.Content, "application/pdf");
    }

    private async Task<bool> CompletarAsync(ExpedienteDetalleViewModel model, CancellationToken ct)
    {
        var review = await expedientes.GetAsync(model.Id, ct);
        if (review is null)
            return false;

        var vistaPrevia = model.Documento is { } documento ? await expedientes.GetPreviewAsync(documento, ct) : null;
        var puedeMarcar = (await authorization.AuthorizeAsync(User, Policies.Capture)).Succeeded;

        model.Completar(review, vistaPrevia, puedeMarcar, clock.LocalTimeZone);
        return true;
    }
}
