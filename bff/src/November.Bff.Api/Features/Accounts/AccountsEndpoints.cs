using Mediator;

namespace November.Bff.Api.Features.Accounts;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/accounts").WithTags("Accounts").RequireAuthorization();

        group.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListAccounts(), cancellationToken)));

        return api;
    }
}
