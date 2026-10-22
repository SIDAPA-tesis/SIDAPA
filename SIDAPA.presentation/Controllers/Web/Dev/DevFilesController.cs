using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.Dev;

namespace SIDAPA.presentation.Controllers.Web.Dev;

/// <summary>Página para probar la validación de archivos (SCRUM-50). Solo responde en Development.</summary>
[Route("dev/archivos")]
public class DevFilesController(IWebHostEnvironment env, IDocumentFileValidator validator) : Controller
{
    private const long RequestLimit = 100L * 1024 * 1024; // deja llegar archivos grandes para mostrar el mensaje

    [HttpGet("")]
    public IActionResult Validate() => env.IsDevelopment() ? View(new DevFileViewModel()) : NotFound();

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> Validate(DevFileViewModel model, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (model.File is null)
        {
            ModelState.AddModelError(nameof(model.File), "Selecciona un archivo.");
            return View(model);
        }

        await using var stream = model.File.OpenReadStream();
        await validator.ValidateAsync(model.File.FileName, model.File.Length, stream, ct);

        model.Result = $"\"{model.File.FileName}\" es válido ({model.File.Length / 1048576d:0.##} MB).";
        return View(model);
    }
}