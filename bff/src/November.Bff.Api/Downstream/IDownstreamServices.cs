using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Downstream;

/// <summary>Read access to the company's accounts on the core banking platform.</summary>
public interface IAccountsService
{
    ValueTask<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);

    ValueTask<Account> GetAsync(string accountId, CancellationToken cancellationToken);
}

/// <summary>Saved and one-off payees held by the beneficiary platform.</summary>
public interface IBeneficiariesService
{
    ValueTask<IReadOnlyList<Beneficiary>> ListAsync(CancellationToken cancellationToken);

    ValueTask<Beneficiary> GetAsync(string beneficiaryId, CancellationToken cancellationToken);

    ValueTask<Beneficiary> AddAsync(NewBeneficiaryDetails details, CancellationToken cancellationToken);

    ValueTask SaveForFutureAsync(string beneficiaryId, CancellationToken cancellationToken);
}

/// <summary>The dealing platform that issues time-limited FX quotes.</summary>
public interface IFxService
{
    ValueTask<FxQuote> CreateQuoteAsync(
        string sellCurrency,
        string buyCurrency,
        decimal amount,
        FixedSide fixedSide,
        CancellationToken cancellationToken);
}

/// <summary>The payments platform. It owns status transitions, four-eyes and FX quote redemption.</summary>
public interface IPaymentsService
{
    ValueTask<PaymentRecord> CreateAsync(NewPayment payment, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<PaymentRecord>> ListAsync(PaymentFilter filter, CancellationToken cancellationToken);

    ValueTask<PaymentRecord> GetAsync(string paymentId, CancellationToken cancellationToken);

    ValueTask<PaymentRecord> ApproveAsync(
        string paymentId,
        string approverUserId,
        string approverName,
        string? fxQuoteId,
        CancellationToken cancellationToken);

    ValueTask<PaymentRecord> RejectAsync(
        string paymentId,
        string approverUserId,
        string approverName,
        string reason,
        CancellationToken cancellationToken);

    ValueTask<PaymentRecord> CancelAsync(
        string paymentId,
        string userId,
        string userName,
        CancellationToken cancellationToken);
}

/// <summary>The platform that tracks money sent to the company from abroad.</summary>
public interface IIncomingPaymentsService
{
    ValueTask<IReadOnlyList<IncomingPaymentRecord>> ListAsync(IncomingStatus? status, CancellationToken cancellationToken);

    ValueTask<IncomingPaymentRecord> GetAsync(string incomingPaymentId, CancellationToken cancellationToken);

    ValueTask<IncomingPaymentRecord> RespondAsync(
        string incomingPaymentId,
        IncomingResponse response,
        CancellationToken cancellationToken);
}

/// <summary>Static reference data: currencies, destination countries and purpose codes.</summary>
public interface IReferenceDataService
{
    ValueTask<IReadOnlyList<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken);

    ValueTask<Country?> FindCountryAsync(string code, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<PurposeCode>> GetPurposeCodesAsync(PurposeDirection? direction, CancellationToken cancellationToken);

    ValueTask<PurposeCode?> FindPurposeCodeAsync(string code, CancellationToken cancellationToken);
}
