using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CrossBusExplorer.Host.Diagnostics;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    DiagnosticsLog diagnostics) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (status, title, code) = exception switch
        {
            ValidationException validation => (StatusCodes.Status400BadRequest, "Invalid request", validation.Code),
            MessageTooBigException => (StatusCodes.Status400BadRequest, "Message too big", null),
            ServiceBusConnectionDoesntExist => (StatusCodes.Status404NotFound, "Connection not found", null),
            ServiceBusOperationException operation => (StatusCodes.Status502BadGateway, "Service Bus operation failed", operation.Code),
            _ => (StatusCodes.Status500InternalServerError, exception.GetType().Name, (string?)null)
        };

        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? title
            : exception.Message;

        diagnostics.Add(
            "error",
            $"{httpContext.Request.Method} {httpContext.Request.Path}",
            code is null ? message : $"[{code}] {message}",
            exception.ToString());

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = code is null ? message : $"{message} ({code})"
        };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
