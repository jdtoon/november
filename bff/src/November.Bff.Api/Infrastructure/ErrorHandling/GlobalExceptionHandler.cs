using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace November.Bff.Api.Infrastructure.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainException domain => Problems.From(domain),
            ValidationException validation => Problems.From(validation),
            BadHttpRequestException => Problems.Create(
                StatusCodes.Status400BadRequest,
                "bad_request",
                "The request could not be read",
                "A route, query or body value is missing or has the wrong format."),
            _ => null
        };

        if (problem is null)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            problem = Problems.Create(
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "An unexpected error occurred",
                "The request could not be completed. Quote the trace id when reporting this.");
        }
        else
        {
            logger.LogInformation("Request failed with {Status} {Code} for {Method} {Path}",
                problem.Status, problem.Extensions["code"], httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
