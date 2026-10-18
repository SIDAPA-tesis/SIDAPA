using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Filters;
using SIDAPA.presentation.Models.Auth;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.Controllers.Web.Auth;

[AllowAnonymous]
[TypeFilter(typeof(AntiforgeryRedirectFilter))]
public class AuthController(IUserService userService, ITokenService tokenService) : Controller
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHome();

        return View(new LoginViewModel());
    }

    /// <summary>
    /// Post normal del formulario. Si las credenciales no son correctas, UserService lanza
    /// InvalidCredentialsException y DomainExceptionFilter vuelve a pintar la vista con el mensaje.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [TypeFilter(typeof(DomainExceptionFilter))]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var session = await userService.LoginAsync(model.Email, model.Password, ct);
        AuthCookie.Write(Response, tokenService.CreateToken(session));

        return RedirectToHome();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        AuthCookie.Delete(Response);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Denied() => View();

    private IActionResult RedirectToHome() => RedirectToAction("Index", "Index");
}
