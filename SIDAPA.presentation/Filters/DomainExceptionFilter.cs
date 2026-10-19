using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.presentation.Filters;

/// <summary>
/// Si la acción lanza una <see cref="DomainException"/> (o una que herede de ella), vuelve a pintar
/// la misma vista con lo que el usuario envió y el mensaje del error. Así el controlador no necesita
/// try/catch ni JavaScript para mostrar errores de negocio.
/// Uso: <c>[TypeFilter(typeof(DomainExceptionFilter))]</c> en la acción POST.
/// La vista debe llamarse igual que la acción y mostrar los errores con <c>asp-validation-summary</c>.
/// </summary>
public class DomainExceptionFilter(IModelMetadataProvider metadataProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.ExceptionHandled || executed.Exception is not DomainException exception)
            return;

        context.ModelState.AddModelError(string.Empty, exception.Message);

        // El modelo que recibió la acción (p. ej. LoginViewModel), para que el formulario conserve lo escrito.
        var model = context.ActionArguments.Values.FirstOrDefault(arg => arg is not null and not CancellationToken);

        executed.Result = new ViewResult
        {
            ViewName = context.ActionDescriptor.RouteValues["action"],
            ViewData = new ViewDataDictionary(metadataProvider, context.ModelState) { Model = model }
        };
        executed.ExceptionHandled = true;
    }
}
