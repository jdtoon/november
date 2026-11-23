using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.Validation;

namespace November.Bff.Api.Features.Fx;

public sealed record CreateFxQuote(string SellCurrency, string BuyCurrency, decimal Amount, FixedSide FixedSide) : ICommand<FxQuote>;

public sealed class CreateFxQuoteValidator : AbstractValidator<CreateFxQuote>
{
    public CreateFxQuoteValidator()
    {
        RuleFor(x => x.SellCurrency).SupportedCurrency();
        RuleFor(x => x.BuyCurrency).SupportedCurrency();
        RuleFor(x => x.BuyCurrency)
            .NotEqual(x => x.SellCurrency).WithMessage("Sell and buy currencies must be different.");
        RuleFor(x => x.Amount).ValidAmount(x => x.FixedSide == FixedSide.Sell ? x.SellCurrency : x.BuyCurrency);
        RuleFor(x => x.FixedSide).IsInEnum();
    }
}

public sealed class CreateFxQuoteHandler(IFxService fx) : ICommandHandler<CreateFxQuote, FxQuote>
{
    public ValueTask<FxQuote> Handle(CreateFxQuote command, CancellationToken cancellationToken) =>
        fx.CreateQuoteAsync(command.SellCurrency, command.BuyCurrency, command.Amount, command.FixedSide, cancellationToken);
}
