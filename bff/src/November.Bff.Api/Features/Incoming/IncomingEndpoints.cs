using Mediator;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Incoming;

public static class IncomingEndpoints
{
    public static IEndpointRouteBuilder MapIncomingEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/incoming-payments").WithTags("Incoming payments").RequireAuthorization();

        group.MapGet("", async (IncomingStatus? status, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListIncomingPayments(status), cancellationToken)));

        group.MapGet("/{id}", async (string id, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new GetIncomingPayment(id), cancellationToken)));

        group.MapPost("/{id}/respond", async (string id, RespondToIncomingPaymentRequest body, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new RespondToIncomingPayment(id, body.PurposeCode, body.FxQuoteId), cancellationToken)));

        return api;
    }
}
