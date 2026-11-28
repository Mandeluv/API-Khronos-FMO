using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
namespace API_Khronos_FMO.Services;
public class IdentidadFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            var prop = arg.GetType().GetProperty("CarnetUsuario");
            if (prop?.PropertyType != typeof(string)) continue;
            if (prop.GetValue(arg) as string != context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier))
            { context.Result = new ForbidResult(); return; }
        }
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}
