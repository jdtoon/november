using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Features.Payments;

public sealed record CancelPayment(string Id) : ICommand<PaymentDetail>;

public sealed class CancelPaymentValidator : AbstractValidator<CancelPayment>
{
    public CancelPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class CancelPaymentHandler(IPaymentsService payments, PaymentViewBuilder views, ICurrentUser user)
    : ICommandHandler<CancelPayment, PaymentDetail>
{
    public async ValueTask<PaymentDetail> Handle(CancelPayment command, CancellationToken cancellationToken)
    {
        var existing = await payments.GetAsync(command.Id, cancellationToken);
        if (existing.CapturedByUserId != user.UserId)
        {
            throw Errors.Forbidden("Only the person who captured a payment can cancel it.");
        }

        var cancelled = await payments.CancelAsync(command.Id, user.UserId, user.DisplayName, cancellationToken);
        return await views.DetailAsync(cancelled, cancellationToken);
    }
}
