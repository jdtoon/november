using Mediator;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Reference;

public static class ReferenceEndpoints
{
    public static IEndpointRouteBuilder MapReferenceEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/reference").WithTags("Reference data").RequireAuthorization();

        group.MapGet("/currencies", async (ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListCurrencies(), cancellationToken)));

        group.MapGet("/countries", async (ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListCountries(), cancellationToken)));

        group.MapGet("/purpose-codes", async (PurposeDirection? direction, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListPurposeCodes(direction), cancellationToken)));

        return api;
    }
}
