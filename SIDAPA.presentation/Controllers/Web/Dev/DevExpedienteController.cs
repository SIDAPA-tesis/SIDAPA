using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.Dev;

namespace SIDAPA.presentation.Controllers.Web.Dev;

/// <summary>Página para probar el cambio automático a "Expediente completo" (SCRUM-56). Solo responde en Development.</summary>
[Route("dev/expediente")]
public class DevExpedienteController(IWebHostEnvironment env, IExpedienteService expediente) : Controller
{
    // El id del usuario que inició sesión viene en el claim "sub" del token.
    private int CurrentUserId => int.Parse(User.FindFirst("sub")!.Value);

    [HttpGet("")]
    public IActionResult Index() => env.IsDevelopment() ? View(new DevExpedienteViewModel()) : NotFound();

    /// <summary>"Solo revisar" llama a CheckAsync; "Evaluar" llama a EvaluateAsync (lo mismo que hará SCRUM-55).</summary>
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> Index(DevExpedienteViewModel model, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        model.Result = model.Apply
            ? await expediente.EvaluateAsync(model.ReferenciaId, CurrentUserId, ct)
            : await expediente.CheckAsync(model.ReferenciaId, ct);

        return View(model);
    }
}