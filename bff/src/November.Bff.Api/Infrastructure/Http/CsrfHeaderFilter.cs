using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Infrastructure.Http;

public sealed class CsrfHeaderFilter : IEndpointFilter
{
    public const string HeaderName = "X-CSRF";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var isSafe = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method);

        if (!isSafe && request.Headers[HeaderName] != "1")
        {
            var problem = Problems.Create(
                StatusCodes.Status400BadRequest,
                "csrf_header_missing",
                "The X-CSRF header is missing",
                $"Non-GET requests must send the header '{HeaderName}: 1'.");
            return ValueTask.FromResult<object?>(TypedResults.Problem(problem));
        }

        return next(context);
    }
}
