using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.CaseFile.ArrivalNotice;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.Controllers.Web.CaseFile.ArrivalNotice;

/// <summary>Portal del Cliente: nuevo aviso de arribo y sus referencias (HU-02 / SCRUM-46).</summary>
[Authorize(Policy = Policies.ClientPortal)]
public class AvisosController(IArrivalNoticeService avisos) : Controller
{
    // 5 archivos de 20 MB más los campos del formulario. Cada archivo se valida aparte (20 MB) en application.
    private const long RequestLimit = 110L * 1024 * 1024;

    [TempData] public string? ReferenciaCreada { get; set; }

    // El cliente y el usuario salen siempre del token, nunca del formulario.
    private int? ClientId => int.TryParse(User.FindFirst("client_id")?.Value, out var id) ? id : null;
    private int? UserId => int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (ClientId is not { } clientId)
            return Forbid();

        var referencias = await avisos.GetClientPanelAsync(clientId, ct);
        return View(new PanelClienteViewModel(referencias, ReferenciaCreada));
    }

    [HttpGet]
    public async Task<IActionResult> Nuevo(CancellationToken ct) =>
        View(NuevoAvisoViewModel.Vacio().Completar(await avisos.GetCatalogsAsync(ct)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> Nuevo(NuevoAvisoViewModel model, CancellationToken ct)
    {
        if (ClientId is not { } clientId || UserId is not { } userId)
            return Forbid();

        // Antes de cualquier error: si el filtro vuelve a pintar la vista, ya trae los catálogos.
        model.Completar(await avisos.GetCatalogsAsync(ct));

        if (!ModelState.IsValid)
            return View(model);

        var streams = new List<Stream>();
        try
        {
            var files = model.ConArchivo()
                .Select(d =>
                {
                    var stream = d.Archivo!.OpenReadStream();
                    streams.Add(stream);
                    return new ArrivalNoticeFileDto(d.Tipo ?? default(DocumentTypeEnum), d.Archivo.FileName, d.Archivo.Length, stream);
                })
                .ToList();

            var created = await avisos.CreateAsync(new CreateArrivalNoticeDto(
                clientId, userId, model.TipoOperacion, model.FechaArribo, model.Aduana, model.Observaciones, files), ct);

            ReferenciaCreada = created.ReferenciaNumber;
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
