using InvoiceReminders.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminders.Api.Errors;

/// <summary>Traduce excepciones a ProblemDetails (RFC 9457). Los errores inesperados no exponen detalles internos.</summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // El cliente cerró la conexión: no es un error del servidor y no hay a quién responder.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var problem = exception switch
        {
            CustomerNotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Cliente no encontrado",
                Detail = notFound.Message,
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error inesperado",
                Detail = "Consulte el log de la aplicación con el traceId de esta respuesta.",
            },
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado en {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}
