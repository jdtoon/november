using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Downstream.Fakes;

internal sealed class InMemoryFxService(TimeProvider timeProvider) : IFxService, IFxQuoteRedeemer
{
    public const int ValidForSeconds = 60;

    private static readonly Dictionary<string, decimal> UnitsPerGbp = new()
    {
        ["GBP"] = 1.00m,
        ["EUR"] = 1.16m,
        ["USD"] = 1.27m,
        ["ZAR"] = 22.85m,
        ["CHF"] = 1.12m,
        ["JPY"] = 192.40m
    };

    private readonly Lock gate = new();
    private readonly Dictionary<string, Entry> quotes = [];

    public ValueTask<FxQuote> CreateQuoteAsync(
        string sellCurrency,
        string buyCurrency,
        decimal amount,
        FixedSide fixedSide,
        CancellationToken cancellationToken)
    {
        if (!UnitsPerGbp.TryGetValue(sellCurrency, out var sellUnits) || !UnitsPerGbp.TryGetValue(buyCurrency, out var buyUnits))
        {
            throw Errors.Validation("sellCurrency", "Both currencies must be supported.");
        }

        var jitter = 1m + ((decimal)Random.Shared.NextDouble() - 0.5m) * 0.006m;
        var rate = Math.Round(buyUnits / sellUnits * jitter, 6);

        decimal sellAmount;
        decimal buyAmount;
        if (fixedSide == FixedSide.Sell)
        {
            sellAmount = Currencies.Round(amount, sellCurrency);
            buyAmount = Currencies.Round(sellAmount * rate, buyCurrency);
        }
        else
        {
            buyAmount = Currencies.Round(amount, buyCurrency);
            sellAmount = Currencies.Round(buyAmount / rate, sellCurrency, MidpointRounding.AwayFromZero);
        }

        var now = timeProvider.GetUtcNow();
        var quote = new FxQuote(
            "fxq_" + Guid.NewGuid().ToString("N")[..12],
            sellCurrency,
            buyCurrency,
            rate,
            sellAmount,
            buyAmount,
            now,
            now.AddSeconds(ValidForSeconds),
            ValidForSeconds);

        lock (gate)
        {
            var stale = quotes.Where(q => q.Value.Quote.ExpiresAt < now.AddHours(-1)).Select(q => q.Key).ToList();
            stale.ForEach(key => quotes.Remove(key));
            quotes[quote.QuoteId] = new Entry(quote, fixedSide, fixedSide == FixedSide.Sell ? sellAmount : buyAmount);
        }

        return ValueTask.FromResult(quote);
    }

    FxQuote IFxQuoteRedeemer.Redeem(string quoteId, FxQuoteMatch expected)
    {
        lock (gate)
        {
            if (!quotes.TryGetValue(quoteId, out var entry))
            {
                throw Errors.NotFound("FX quote", quoteId);
            }

            if (entry.Used)
            {
                throw Errors.FxQuoteUsed(quoteId);
            }

            if (timeProvider.GetUtcNow() > entry.Quote.ExpiresAt)
            {
                throw Errors.FxQuoteExpired(quoteId, entry.Quote.ExpiresAt);
            }

            if (entry.Quote.SellCurrency != expected.SellCurrency || entry.Quote.BuyCurrency != expected.BuyCurrency)
            {
                throw Errors.FxQuoteMismatch(
                    $"Quote {quoteId} converts {entry.Quote.SellCurrency} to {entry.Quote.BuyCurrency}, but {expected.SellCurrency} to {expected.BuyCurrency} is needed.");
            }

            if (entry.Side != expected.Side || entry.FixedAmount != expected.Amount)
            {
                throw Errors.FxQuoteMismatch(
                    $"Quote {quoteId} fixes the {entry.Side.ToString().ToLowerInvariant()} side at {entry.FixedAmount}, but the {expected.Side.ToString().ToLowerInvariant()} side at {expected.Amount} is needed.");
            }

            entry.Used = true;
            return entry.Quote;
        }
    }

    private sealed class Entry(FxQuote quote, FixedSide side, decimal fixedAmount)
    {
        public FxQuote Quote { get; } = quote;

        public FixedSide Side { get; } = side;

        public decimal FixedAmount { get; } = fixedAmount;

        public bool Used { get; set; }
    }
}
