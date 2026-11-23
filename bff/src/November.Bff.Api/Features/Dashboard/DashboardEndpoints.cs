using Mediator;

namespace November.Bff.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new GetDashboard(), cancellationToken)));

        return api;
    }
}
