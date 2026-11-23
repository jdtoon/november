using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Features.Incoming;

public sealed record RespondToIncomingPaymentRequest(string? PurposeCode, string? FxQuoteId);

public sealed record RespondToIncomingPayment(string Id, string? PurposeCode, string? FxQuoteId) : ICommand<IncomingPaymentDetail>;

public sealed class RespondToIncomingPaymentValidator : AbstractValidator<RespondToIncomingPayment>
{
    public RespondToIncomingPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class RespondToIncomingPaymentHandler(
    IIncomingPaymentsService incoming,
    IReferenceDataService reference,
    IncomingViewBuilder views,
    ICurrentUser user) : ICommandHandler<RespondToIncomingPayment, IncomingPaymentDetail>
{
    public async ValueTask<IncomingPaymentDetail> Handle(RespondToIncomingPayment command, CancellationToken cancellationToken)
    {
        string? purposeCode = null;
        if (!string.IsNullOrWhiteSpace(command.PurposeCode))
        {
            var purpose = await reference.FindPurposeCodeAsync(command.PurposeCode, cancellationToken);
            if (purpose is null || purpose.Direction == PurposeDirection.Outgoing)
            {
                throw Errors.Validation("purposeCode", "Choose an incoming purpose code.");
            }

            purposeCode = purpose.Code;
        }

        var updated = await incoming.RespondAsync(
            command.Id,
            new IncomingResponse(purposeCode, command.FxQuoteId, user.DisplayName),
            cancellationToken);

        return await views.DetailAsync(updated, cancellationToken);
    }
}
