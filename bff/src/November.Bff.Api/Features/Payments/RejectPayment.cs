using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Features.Payments;

public sealed record RejectPaymentRequest(string Reason);

public sealed record RejectPayment(string Id, string Reason) : ICommand<PaymentDetail>;

public sealed class RejectPaymentValidator : AbstractValidator<RejectPayment>
{
    public RejectPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RejectPaymentHandler(IPaymentsService payments, PaymentViewBuilder views, ICurrentUser user)
    : ICommandHandler<RejectPayment, PaymentDetail>
{
    public async ValueTask<PaymentDetail> Handle(RejectPayment command, CancellationToken cancellationToken)
    {
        var existing = await payments.GetAsync(command.Id, cancellationToken);
        if (existing.CapturedByUserId == user.UserId)
        {
            throw Errors.SelfApprovalForbidden();
        }

        var rejected = await payments.RejectAsync(command.Id, user.UserId, user.DisplayName, command.Reason.Trim(), cancellationToken);
        return await views.DetailAsync(rejected, cancellationToken);
    }
}
