using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Downstream.Fakes;

internal sealed class InMemoryIncomingPaymentsService : IIncomingPaymentsService, IProcessingSimulation
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, IncomingPaymentRecord> incoming = [];
    private readonly IAccountLedger ledger;
    private readonly IFxQuoteRedeemer fx;
    private readonly TimeProvider timeProvider;

    public InMemoryIncomingPaymentsService(IAccountLedger ledger, IFxQuoteRedeemer fx, TimeProvider timeProvider)
    {
        this.ledger = ledger;
        this.fx = fx;
        this.timeProvider = timeProvider;
        SeedIncoming();
    }

    public ValueTask<IReadOnlyList<IncomingPaymentRecord>> ListAsync(IncomingStatus? status, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            IReadOnlyList<IncomingPaymentRecord> result = incoming.Values
                .Where(p => status is null || p.Status == status)
                .OrderByDescending(p => p.ReceivedAt)
                .ToList();
            return ValueTask.FromResult(result);
        }
    }

    public ValueTask<IncomingPaymentRecord> GetAsync(string incomingPaymentId, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return ValueTask.FromResult(Find(incomingPaymentId));
        }
    }

    public ValueTask<IncomingPaymentRecord> RespondAsync(
        string incomingPaymentId,
        IncomingResponse response,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var payment = Find(incomingPaymentId);
            if (payment.Status != IncomingStatus.ActionRequired)
            {
                throw Errors.InvalidStatus(
                    $"Incoming payment {payment.Id} is {payment.Status} and no longer needs a response.");
            }

            var needsPurpose = payment.ActionsRequired.Contains(IncomingAction.PurposeCode);
            var needsFx = payment.ActionsRequired.Contains(IncomingAction.FxAcceptance);
            var now = timeProvider.GetUtcNow();

            if (needsPurpose && string.IsNullOrWhiteSpace(response.PurposeCode))
            {
                throw Errors.Validation("purposeCode", "A purpose code is required for this payment.");
            }

            var creditAccount = ledger.Get(payment.CreditAccountId);
            Money credited = payment.Amount;
            FxAcceptance? acceptance = null;

            if (needsFx)
            {
                if (response.FxQuoteId is null)
                {
                    throw Errors.FxQuoteRequired(
                        $"Accepting an FX quote from {payment.Amount.Currency} to {creditAccount.Currency} is required.");
                }

                var quote = fx.Redeem(
                    response.FxQuoteId,
                    new FxQuoteMatch(payment.Amount.Currency, creditAccount.Currency, FixedSide.Sell, payment.Amount.Amount));
                credited = new Money(quote.BuyAmount, creditAccount.Currency);
                acceptance = new FxAcceptance(quote.Rate, quote.QuoteId, response.RespondedByName, now);
            }
            else if (response.FxQuoteId is not null)
            {
                throw Errors.Validation("fxQuoteId", "No FX quote is needed for this payment.");
            }

            var updated = payment with
            {
                Status = IncomingStatus.Processing,
                ActionsRequired = [],
                PurposeCode = response.PurposeCode ?? payment.PurposeCode,
                CreditedAmount = credited,
                Fx = acceptance,
                ProcessingStartedAt = now,
                Timeline = [.. payment.Timeline, new TimelineEntry(now, "Details provided", response.RespondedByName)]
            };
            incoming[updated.Id] = updated;
            return ValueTask.FromResult(updated);
        }
    }

    int IProcessingSimulation.Advance(DateTimeOffset startedOnOrBefore)
    {
        lock (gate)
        {
            var due = incoming.Values
                .Where(p => p.Status == IncomingStatus.Processing && p.ProcessingStartedAt <= startedOnOrBefore)
                .ToList();

            foreach (var payment in due)
            {
                var credited = payment.CreditedAmount ?? payment.Amount;
                ledger.Credit(payment.CreditAccountId, credited.Amount);
                incoming[payment.Id] = payment with
                {
                    Status = IncomingStatus.Credited,
                    Timeline = [.. payment.Timeline, new TimelineEntry(timeProvider.GetUtcNow(), "Credited")]
                };
            }

            return due.Count;
        }
    }

    private IncomingPaymentRecord Find(string incomingPaymentId) =>
        incoming.TryGetValue(incomingPaymentId, out var payment)
            ? payment
            : throw Errors.NotFound("Incoming payment", incomingPaymentId);

    private void SeedIncoming()
    {
        var now = timeProvider.GetUtcNow();

        IncomingPaymentRecord Make(
            string id, string sender, string country, string bank, string swift, string reference, Money amount, string creditAccountId,
            IncomingStatus status, TimeSpan receivedAgo, IncomingAction[] actions, string? purpose, Money? credited, FxAcceptance? acceptance,
            DateTimeOffset? processingStartedAt, params TimelineEntry[] timeline)
        {
            var receivedAt = now - receivedAgo;
            return new IncomingPaymentRecord(
                id, sender, country, bank, swift, reference, amount, creditAccountId, status, receivedAt, actions, purpose, credited,
                acceptance, processingStartedAt, [new TimelineEntry(receivedAt, "Received from sender's bank"), .. timeline]);
        }

        IncomingPaymentRecord[] seeded =
        [
            Make("inc_5Ra2kVx9Dm1q", "Redwood Outfitters LLC", "US", "Cascade Mutual Bank", "CSMBUS44", "INV 88213 SPRING ORDER",
                new Money(18_400.00m, "USD"), "acc_gbp_operating", IncomingStatus.ActionRequired, TimeSpan.FromHours(5),
                [IncomingAction.PurposeCode, IncomingAction.FxAcceptance], null, null, null, null,
                new TimelineEntry(now - TimeSpan.FromHours(5) + TimeSpan.FromMinutes(2), "Bank needs a purpose code and FX acceptance")),
            Make("inc_7Tg4nBz3Lc8w", "Cape Meridian Exports (Pty) Ltd", "ZA", "Table Bay Commercial Bank", "TBCBZAJJ", "SHIPMENT 4471",
                new Money(245_000.00m, "ZAR"), "acc_gbp_operating", IncomingStatus.ActionRequired, TimeSpan.FromHours(26),
                [IncomingAction.FxAcceptance], null, null, null, null,
                new TimelineEntry(now - TimeSpan.FromHours(26) + TimeSpan.FromMinutes(2), "Bank needs FX acceptance")),
            Make("inc_2Vh6pCy1Qe5d", "Elbe Fahrzeugteile GmbH", "DE", "Hansestadt Bank AG", "HSBKDEHH", "RECHNUNG 2026-1190",
                new Money(9_600.00m, "EUR"), "acc_eur_trading", IncomingStatus.ActionRequired, TimeSpan.FromHours(8),
                [IncomingAction.PurposeCode], null, null, null, null,
                new TimelineEntry(now - TimeSpan.FromHours(8) + TimeSpan.FromMinutes(2), "Bank needs a purpose code")),
            Make("inc_9Jd3wMr6Bf4s", "Prairie Lane Farms LLC", "US", "Cascade Mutual Bank", "CSMBUS44", "HARVEST CONTRACT 12",
                new Money(7_250.00m, "USD"), "acc_usd_trading", IncomingStatus.Processing, TimeSpan.FromHours(1),
                [], "GDSS", new Money(7_250.00m, "USD"), null, now,
                new TimelineEntry(now, "Details provided", "Sam Okafor")),
            Make("inc_4Lk8tZq2Nx7a", "Atelier Verdane SARL", "FR", "Banque Lumiere Atlantique", "BLATFRPP", "FACTURE 20260317",
                new Money(3_150.00m, "EUR"), "acc_eur_trading", IncomingStatus.Credited, TimeSpan.FromDays(2),
                [], "SVCR", new Money(3_150.00m, "EUR"), null, null,
                new TimelineEntry(now - TimeSpan.FromDays(2) + TimeSpan.FromMinutes(5), "Credited")),
            Make("inc_6Pb1yHs5Wc9m", "Hoshizora Trading KK", "JP", "Sakurazaka Bank", "SKZKJPJT", "ORDER 55-0198",
                new Money(1_200_000m, "JPY"), "acc_gbp_operating", IncomingStatus.Cancelled, TimeSpan.FromDays(4),
                [], null, null, null, null,
                new TimelineEntry(now - TimeSpan.FromDays(4) + TimeSpan.FromHours(3), "Cancelled", null, "Sender recalled the payment."))
        ];

        foreach (var payment in seeded)
        {
            incoming[payment.Id] = payment;
        }
    }
}
