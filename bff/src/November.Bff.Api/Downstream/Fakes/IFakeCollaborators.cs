using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Downstream.Fakes;

/// <summary>
/// Platform-to-platform calls that a real bank would make behind the scenes. The BFF never uses these,
/// which is why they are not part of the public downstream interfaces.
/// </summary>
internal interface IAccountLedger
{
    Account Get(string accountId);

    void Reserve(string accountId, decimal amount);

    void AdjustReservation(string accountId, decimal delta);

    void Release(string accountId, decimal amount);

    void Settle(string accountId, decimal amount);

    void Credit(string accountId, decimal amount);
}

internal interface IFxQuoteRedeemer
{
    FxQuote Redeem(string quoteId, FxQuoteMatch expected);
}

internal interface IProcessingSimulation
{
    int Advance(DateTimeOffset startedOnOrBefore);
}
