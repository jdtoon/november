namespace November.Bff.Api.Downstream.Models;

public sealed record FxQuote(
    string QuoteId,
    string SellCurrency,
    string BuyCurrency,
    decimal Rate,
    decimal SellAmount,
    decimal BuyAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int ValidForSeconds);

public sealed record FxQuoteMatch(string SellCurrency, string BuyCurrency, FixedSide Side, decimal Amount);

public sealed record FxAcceptance(decimal Rate, string QuoteId, string AcceptedBy, DateTimeOffset AcceptedAt);
