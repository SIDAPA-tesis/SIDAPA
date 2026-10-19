using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SIDAPA.presentation.Filters;

/// <summary>
/// Si el token antiforgery ya no sirve (la página quedó abierta mucho tiempo o cambió la sesión),
/// en lugar de un 400 en blanco vuelve a abrir la misma pantalla con GET y un formulario nuevo.
/// Uso: <c>[TypeFilter(typeof(AntiforgeryRedirectFilter))]</c> en el controlador.
/// </summary>
public class AntiforgeryRedirectFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not IAntiforgeryValidationFailedResult)
            return;

        var request = context.HttpContext.Request;
        context.Result = new RedirectResult((request.PathBase + request.Path).Value ?? "/");
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
