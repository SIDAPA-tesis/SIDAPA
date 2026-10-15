using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.Controllers.Web.Dev;

/// <summary>Rutas de prueba para verificar los permisos por rol. Solo responden en Development.</summary>
[Route("dev/permisos")]
public class DevPermissionsController(IWebHostEnvironment env) : Controller
{
    [HttpGet("cliente")]
    [Authorize(Policy = Policies.ClientPortal)]
    public IActionResult Cliente() => Allowed("ClientPortal");

    [HttpGet("captura")]
    [Authorize(Policy = Policies.Capture)]
    public IActionResult Captura() => Allowed("Capture");

    [HttpGet("glosa")]
    [Authorize(Policy = Policies.Glosa)]
    public IActionResult Glosa() => Allowed("Glosa");

    [HttpGet("admin")]
    [Authorize(Policy = Policies.Administration)]
    public IActionResult Admin() => Allowed("Administration");

    private IActionResult Allowed(string area) =>
        env.IsDevelopment()
            ? Content($"OK: {User.FindFirst("name")?.Value} ({User.FindFirst("role")?.Value}) puede entrar a {area}.")
            : NotFound();
}