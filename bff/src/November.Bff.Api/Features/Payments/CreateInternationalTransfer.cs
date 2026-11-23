using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.Validation;

namespace November.Bff.Api.Features.Payments;

public sealed record CreateInternationalTransfer(
    string FromAccountId,
    string ToAccountId,
    decimal Amount,
    string Currency,
    string Reference,
    string? FxQuoteId,
    bool DeclarationsAccepted) : ICommand<PaymentDetail>;

public sealed class CreateInternationalTransferValidator : AbstractValidator<CreateInternationalTransfer>
{
    public CreateInternationalTransferValidator()
    {
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountId).NotEmpty().NotEqual(x => x.FromAccountId)
            .WithMessage("The destination account must differ from the source account.");
        RuleFor(x => x.Currency).SupportedCurrency();
        RuleFor(x => x.Amount).ValidAmount(x => x.Currency);
        RuleFor(x => x.Reference).PaymentReference();
        RuleFor(x => x.DeclarationsAccepted).Equal(true).WithMessage("The declarations must be accepted.");
    }
}

public sealed class CreateInternationalTransferHandler(
    IAccountsService accounts,
    IPaymentsService payments,
    PaymentViewBuilder views,
    ICurrentUser user) : ICommandHandler<CreateInternationalTransfer, PaymentDetail>
{
    public async ValueTask<PaymentDetail> Handle(CreateInternationalTransfer command, CancellationToken cancellationToken)
    {
        var from = await accounts.GetAsync(command.FromAccountId, cancellationToken);
        var to = await accounts.GetAsync(command.ToAccountId, cancellationToken);
        var side = PaymentPolicy.ResolveFixedSide(from.Currency, to.Currency, command.Currency);

        var record = await payments.CreateAsync(
            new NewPayment(
                PaymentType.InternationalTransfer,
                from.Id,
                null,
                to.Id,
                to.Currency,
                side,
                command.Amount,
                null,
                command.Reference,
                command.FxQuoteId,
                user.UserId,
                user.DisplayName),
            cancellationToken);

        return await views.DetailAsync(record, cancellationToken);
    }
}
