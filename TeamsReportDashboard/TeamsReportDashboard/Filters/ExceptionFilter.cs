using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamsReportDashboard.Exceptions;

namespace TeamsReportDashboard.Filters;

public class ExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if(context.Exception is ExceptionBase exception)
            HandleProjectException(exception, context);
        else
            ThrowUnknownException(context);
    }
    
    private static void HandleProjectException(ExceptionBase exception, ExceptionContext context)
    {
        context.HttpContext.Response.StatusCode = (int)exception.GetStatusCode();
        context.Result = new ObjectResult(new
        {
            errors = exception.GetErrorMessages()
        });
    }

    private static void ThrowUnknownException(ExceptionContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Result = new ObjectResult(new
        {
            errors = new List<string> { "An unknown error has occurred." }
        });
    }
}