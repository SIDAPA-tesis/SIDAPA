using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.DTOs.Core.Documents;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.Dev;

namespace SIDAPA.presentation.Controllers.Web.Dev;

/// <summary>Página para probar el guardado de documentos en la nube (SCRUM-48). Solo responde en Development.</summary>
[Route("dev/documentos")]
public class DevDocumentsController(IWebHostEnvironment env, IDocumentService documents) : Controller
{
    private const long RequestLimit = 100L * 1024 * 1024; // deja llegar archivos grandes para ver el mensaje de SCRUM-50

    // El id del usuario que inició sesión viene en el claim "sub" del token.
    private int CurrentUserId => int.Parse(User.FindFirst("sub")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index(int referenciaId = 1, CancellationToken ct = default)
    {
        if (!env.IsDevelopment())
            return NotFound();

        return View(await LoadAsync(new DevDocumentViewModel { ReferenciaId = referenciaId }, ct));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> Index(DevDocumentViewModel model, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        await LoadAsync(model, ct);   // si la subida truena, la vista ya trae la lista

        if (model.File is null)
        {
            ModelState.AddModelError(nameof(model.File), "Selecciona un archivo.");
            return View(model);
        }

        await using var stream = model.File.OpenReadStream();
        await documents.UploadAsync(new UploadDocumentDto(
            model.ReferenciaId, model.DocumentType, model.File.FileName, model.File.Length, stream, CurrentUserId), ct);

        return RedirectToAction(nameof(Index), new { referenciaId = model.ReferenciaId });
    }

    /// <summary>Abre el archivo con un link firmado temporal (el bucket no es público).</summary>
    [HttpGet("{id:int}/ver")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        return Redirect(await documents.GetDownloadUrlAsync(id, ct));
    }

    private async Task<DevDocumentViewModel> LoadAsync(DevDocumentViewModel model, CancellationToken ct)
    {
        model.Documents = await documents.GetByReferenciaAsync(model.ReferenciaId, ct);
        return model;
    }
}