using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Downstream.Fakes;

internal sealed class InMemoryReferenceDataService : IReferenceDataService
{
    private static readonly IReadOnlyList<Country> Countries =
    [
        new("DE", "Germany", "EUR", BankDetailsFormat.Iban),
        new("FR", "France", "EUR", BankDetailsFormat.Iban),
        new("NL", "Netherlands", "EUR", BankDetailsFormat.Iban),
        new("IE", "Ireland", "EUR", BankDetailsFormat.Iban),
        new("ES", "Spain", "EUR", BankDetailsFormat.Iban),
        new("CH", "Switzerland", "CHF", BankDetailsFormat.Iban),
        new("US", "United States", "USD", BankDetailsFormat.AccountNumberRoutingSwift),
        new("ZA", "South Africa", "ZAR", BankDetailsFormat.AccountNumberSwift),
        new("JP", "Japan", "JPY", BankDetailsFormat.AccountNumberSwift)
    ];

    private static readonly IReadOnlyList<PurposeCode> PurposeCodes =
    [
        new("GDSP", "Goods purchase", PurposeDirection.Outgoing),
        new("PSVC", "Professional services", PurposeDirection.Outgoing),
        new("RENT", "Rent and property costs", PurposeDirection.Outgoing),
        new("TRVL", "Travel and expenses", PurposeDirection.Outgoing),
        new("GDSS", "Sale of goods", PurposeDirection.Incoming),
        new("SVCR", "Services rendered", PurposeDirection.Incoming),
        new("LOAN", "Loan drawdown or repayment", PurposeDirection.Both),
        new("INVT", "Investment", PurposeDirection.Both)
    ];

    public ValueTask<IReadOnlyList<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(Currencies.All);

    public ValueTask<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(Countries);

    public ValueTask<Country?> FindCountryAsync(string code, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Countries.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));

    public ValueTask<IReadOnlyList<PurposeCode>> GetPurposeCodesAsync(PurposeDirection? direction, CancellationToken cancellationToken)
    {
        IReadOnlyList<PurposeCode> result = direction is null
            ? PurposeCodes
            : PurposeCodes.Where(p => p.Direction == direction || p.Direction == PurposeDirection.Both).ToList();
        return ValueTask.FromResult(result);
    }

    public ValueTask<PurposeCode?> FindPurposeCodeAsync(string code, CancellationToken cancellationToken) =>
        ValueTask.FromResult(PurposeCodes.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)));
}
