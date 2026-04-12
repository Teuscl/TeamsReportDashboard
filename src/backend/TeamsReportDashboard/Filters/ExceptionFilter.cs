using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamsReportDashboard.Exceptions;

namespace TeamsReportDashboard.Filters;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionFilter(ILogger<ExceptionFilter> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ExceptionBase exception)
            HandleProjectException(exception, context);
        else
            HandleUnknownException(context);
    }

    private static void HandleProjectException(ExceptionBase exception, ExceptionContext context)
    {
        context.HttpContext.Response.StatusCode = (int)exception.GetStatusCode();
        context.Result = new ObjectResult(new
        {
            errors = exception.GetErrorMessages()
        });
    }

    private void HandleUnknownException(ExceptionContext context)
    {
        _logger.LogError(context.Exception,
            "Unhandled exception on {Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);

        context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        object body = _env.IsDevelopment()
            ? new
            {
                errors = new[] { "An unexpected error has occurred." },
                detail = context.Exception.Message,
                exceptionType = context.Exception.GetType().Name,
                stackTrace = context.Exception.StackTrace
            }
            : new { errors = new[] { "An unexpected error has occurred." } };

        context.Result = new ObjectResult(body);
    }
}