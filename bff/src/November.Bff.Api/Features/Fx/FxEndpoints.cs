using Mediator;

namespace November.Bff.Api.Features.Fx;

public static class FxEndpoints
{
    public static IEndpointRouteBuilder MapFxEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/fx").WithTags("FX").RequireAuthorization();

        group.MapPost("/quotes", async (CreateFxQuote command, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Created((string?)null, await sender.Send(command, cancellationToken)));

        return api;
    }
}
