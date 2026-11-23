using Mediator;

namespace November.Bff.Api.Features.Beneficiaries;

public static class BeneficiariesEndpoints
{
    public static IEndpointRouteBuilder MapBeneficiariesEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/beneficiaries").WithTags("Beneficiaries").RequireAuthorization();

        group.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListBeneficiaries(), cancellationToken)));

        group.MapGet("/{id}", async (string id, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new GetBeneficiary(id), cancellationToken)));

        return api;
    }
}
