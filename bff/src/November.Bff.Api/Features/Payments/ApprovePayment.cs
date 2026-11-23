using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Features.Payments;

public sealed record ApprovePaymentRequest(string? FxQuoteId);

public sealed record ApprovePayment(string Id, string? FxQuoteId) : ICommand<PaymentDetail>;

public sealed class ApprovePaymentValidator : AbstractValidator<ApprovePayment>
{
    public ApprovePaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class ApprovePaymentHandler(
    IPaymentsService payments,
    PaymentViewBuilder views,
    IPublisher publisher,
    ICurrentUser user,
    TimeProvider timeProvider) : ICommandHandler<ApprovePayment, PaymentDetail>
{
    public async ValueTask<PaymentDetail> Handle(ApprovePayment command, CancellationToken cancellationToken)
    {
        var existing = await payments.GetAsync(command.Id, cancellationToken);
        if (existing.CapturedByUserId == user.UserId)
        {
            throw Errors.SelfApprovalForbidden();
        }

        var approved = await payments.ApproveAsync(command.Id, user.UserId, user.DisplayName, command.FxQuoteId, cancellationToken);

        await publisher.Publish(new PaymentApproved(approved.Id, user.DisplayName, timeProvider.GetUtcNow()), cancellationToken);

        return await views.DetailAsync(approved, cancellationToken);
    }
}
