using FluentValidation;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Infrastructure.Validation;

public static class MoneyRules
{
    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule, Func<T, string> currency) =>
        rule
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .Must((instance, amount) => !Currencies.IsSupported(currency(instance)) || Currencies.HasValidPrecision(amount, currency(instance)))
            .WithMessage(instance => $"{currency(instance)} amounts allow at most {Currencies.DecimalsFor(currency(instance))} decimal places.");

    public static IRuleBuilderOptions<T, string> SupportedCurrency<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(Currencies.IsSupported).WithMessage("Currency is not supported.");

    public static IRuleBuilderOptions<T, string> PaymentReference<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Reference is required.")
            .MaximumLength(BankFormats.ReferenceMaxLength)
            .WithMessage($"Reference must be at most {BankFormats.ReferenceMaxLength} characters.");
}
