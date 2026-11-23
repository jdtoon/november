using Mediator;
using Microsoft.AspNetCore.Authentication;
using November.Bff.Api.Infrastructure.Auth;

namespace November.Bff.Api.Features.Session;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder api, AuthMode mode)
    {
        var group = api.MapGroup("/session").WithTags("Session");

        group.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetSession(), cancellationToken)))
            .RequireAuthorization();

        group.MapPost("/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync();
                return TypedResults.NoContent();
            })
            .AllowAnonymous();

        if (mode == AuthMode.Mock)
        {
            group.MapGet("/personas", async (ISender sender, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await sender.Send(new ListPersonas(), cancellationToken)))
                .AllowAnonymous();

            group.MapPost("/login", async (Login command, HttpContext httpContext, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(command, cancellationToken);
                    await httpContext.SignInAsync(result.Principal);
                    return TypedResults.Ok(result.Session);
                })
                .AllowAnonymous();
        }

        return api;
    }
}
