using Mediator;
using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.Auth;

namespace November.Bff.Api.Features.Payments;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/payments").WithTags("Payments").RequireAuthorization();

        group.MapPost("/beneficiary-payments", async (CreateBeneficiaryPayment command, ISender sender, CancellationToken cancellationToken) =>
            {
                var payment = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/payments/{payment.Id}", payment);
            })
            .RequireAuthorization(Policies.Capturer);

        group.MapPost("/international-transfers", async (CreateInternationalTransfer command, ISender sender, CancellationToken cancellationToken) =>
            {
                var payment = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/payments/{payment.Id}", payment);
            })
            .RequireAuthorization(Policies.Capturer);

        group.MapGet("", async (PaymentStatus? status, PaymentType? type, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListPayments(status, type), cancellationToken)));

        group.MapGet("/{id}", async (string id, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new GetPayment(id), cancellationToken)));

        group.MapPost("/{id}/approve", async (string id, ApprovePaymentRequest? body, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new ApprovePayment(id, body?.FxQuoteId), cancellationToken)))
            .RequireAuthorization(Policies.Approver);

        group.MapPost("/{id}/reject", async (string id, RejectPaymentRequest body, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new RejectPayment(id, body.Reason), cancellationToken)))
            .RequireAuthorization(Policies.Approver);

        group.MapPost("/{id}/cancel", async (string id, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new CancelPayment(id), cancellationToken)));

        return api;
    }
}
