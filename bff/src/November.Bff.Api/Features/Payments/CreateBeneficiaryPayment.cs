using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.ErrorHandling;
using November.Bff.Api.Infrastructure.Validation;

namespace November.Bff.Api.Features.Payments;

public sealed record NewBeneficiaryInput(
    string Name,
    string Country,
    string Currency,
    string BankName,
    string? Iban,
    string? AccountNumber,
    string? RoutingNumber,
    string SwiftBic,
    bool SaveForFuture);

public sealed record CreateBeneficiaryPayment(
    string FromAccountId,
    string? BeneficiaryId,
    NewBeneficiaryInput? NewBeneficiary,
    decimal Amount,
    string Currency,
    string PurposeCode,
    string Reference,
    string? FxQuoteId,
    bool DeclarationsAccepted) : ICommand<PaymentDetail>;

public sealed class NewBeneficiaryInputValidator : AbstractValidator<NewBeneficiaryInput>
{
    public NewBeneficiaryInputValidator(IReferenceDataService reference)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(140);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(140);
        RuleFor(x => x.Currency).SupportedCurrency();
        RuleFor(x => x.SwiftBic)
            .Must(BankFormats.IsValidSwiftBic)
            .WithMessage("SWIFT/BIC must be 8 or 11 letters and digits, for example RHHBDEFF.");

        RuleFor(x => x).CustomAsync(async (input, context, cancellationToken) =>
        {
            var country = await reference.FindCountryAsync(input.Country ?? string.Empty, cancellationToken);
            if (country is null)
            {
                context.AddFailure(nameof(input.Country), $"Country '{input.Country}' is not supported.");
                return;
            }

            if (country.BankDetailsFormat == BankDetailsFormat.Iban)
            {
                if (!BankFormats.IsValidIban(input.Iban))
                {
                    context.AddFailure(nameof(input.Iban), "IBAN is not valid.");
                }
                else if (!BankFormats.NormaliseIban(input.Iban!).StartsWith(country.Code, StringComparison.Ordinal))
                {
                    context.AddFailure(nameof(input.Iban), $"IBAN must start with {country.Code} for {country.Name}.");
                }

                return;
            }

            if (!BankFormats.IsValidAccountNumber(input.AccountNumber))
            {
                context.AddFailure(nameof(input.AccountNumber), "Account number must be 4 to 34 letters or digits.");
            }

            if (country.BankDetailsFormat == BankDetailsFormat.AccountNumberRoutingSwift
                && !BankFormats.IsValidRoutingNumber(input.RoutingNumber))
            {
                context.AddFailure(nameof(input.RoutingNumber), "Routing number must be exactly 9 digits.");
            }
        });
    }
}

public sealed class CreateBeneficiaryPaymentValidator : AbstractValidator<CreateBeneficiaryPayment>
{
    public CreateBeneficiaryPaymentValidator(IValidator<NewBeneficiaryInput> newBeneficiaryValidator)
    {
        RuleFor(x => x.FromAccountId).NotEmpty();

        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.BeneficiaryId) != (x.NewBeneficiary is null))
            .OverridePropertyName("beneficiaryId")
            .WithMessage("Provide exactly one of beneficiaryId and newBeneficiary.");

        When(x => x.NewBeneficiary is not null, () =>
            RuleFor(x => x.NewBeneficiary!).SetValidator(newBeneficiaryValidator));

        RuleFor(x => x.Currency).SupportedCurrency();
        RuleFor(x => x.Amount).ValidAmount(x => x.Currency);
        RuleFor(x => x.PurposeCode).NotEmpty();
        RuleFor(x => x.Reference).PaymentReference();
        RuleFor(x => x.DeclarationsAccepted).Equal(true).WithMessage("The declarations must be accepted.");
    }
}

public sealed class CreateBeneficiaryPaymentHandler(
    IAccountsService accounts,
    IBeneficiariesService beneficiaries,
    IPaymentsService payments,
    IReferenceDataService reference,
    PaymentViewBuilder views,
    ICurrentUser user) : ICommandHandler<CreateBeneficiaryPayment, PaymentDetail>
{
    public async ValueTask<PaymentDetail> Handle(CreateBeneficiaryPayment command, CancellationToken cancellationToken)
    {
        var from = await accounts.GetAsync(command.FromAccountId, cancellationToken);

        var purpose = await reference.FindPurposeCodeAsync(command.PurposeCode, cancellationToken);
        if (purpose is null || purpose.Direction == PurposeDirection.Incoming)
        {
            throw Errors.Validation("purposeCode", "Choose an outgoing purpose code.");
        }

        var beneficiary = string.IsNullOrWhiteSpace(command.BeneficiaryId)
            ? await beneficiaries.AddAsync(ToDetails(command.NewBeneficiary!), cancellationToken)
            : await beneficiaries.GetAsync(command.BeneficiaryId, cancellationToken);

        var side = PaymentPolicy.ResolveFixedSide(from.Currency, beneficiary.Currency, command.Currency);

        var record = await payments.CreateAsync(
            new NewPayment(
                PaymentType.BeneficiaryPayment,
                from.Id,
                beneficiary.Id,
                null,
                beneficiary.Currency,
                side,
                command.Amount,
                purpose.Code,
                command.Reference,
                command.FxQuoteId,
                user.UserId,
                user.DisplayName),
            cancellationToken);

        if (command.NewBeneficiary is { SaveForFuture: true })
        {
            await beneficiaries.SaveForFutureAsync(beneficiary.Id, cancellationToken);
        }

        return await views.DetailAsync(record, cancellationToken);
    }

    private static NewBeneficiaryDetails ToDetails(NewBeneficiaryInput input) =>
        new(input.Name, input.Country.ToUpperInvariant(), input.Currency, input.BankName, input.Iban, input.AccountNumber,
            input.RoutingNumber, input.SwiftBic);
}
