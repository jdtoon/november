using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Downstream.Fakes;

internal sealed class InMemoryPaymentsService : IPaymentsService, IProcessingSimulation
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, PaymentRecord> payments = [];
    private readonly IAccountLedger ledger;
    private readonly IFxQuoteRedeemer fx;
    private readonly TimeProvider timeProvider;

    public InMemoryPaymentsService(IAccountLedger ledger, IFxQuoteRedeemer fx, TimeProvider timeProvider)
    {
        this.ledger = ledger;
        this.fx = fx;
        this.timeProvider = timeProvider;
        SeedPayments();
    }

    public ValueTask<PaymentRecord> CreateAsync(NewPayment request, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var from = ledger.Get(request.FromAccountId);
            var requiresFx = from.Currency != request.DestinationCurrency;
            var now = timeProvider.GetUtcNow();

            Money send;
            Money receive;
            FxAcceptance? acceptance = null;

            if (requiresFx)
            {
                if (request.FxQuoteId is null)
                {
                    throw Errors.FxQuoteRequired(
                        $"Converting {from.Currency} to {request.DestinationCurrency} needs an FX quote.");
                }

                var quote = fx.Redeem(
                    request.FxQuoteId,
                    new FxQuoteMatch(from.Currency, request.DestinationCurrency, request.FixedSide, request.Amount));
                send = new Money(quote.SellAmount, from.Currency);
                receive = new Money(quote.BuyAmount, request.DestinationCurrency);
                acceptance = new FxAcceptance(quote.Rate, quote.QuoteId, request.CapturedByName, now);
            }
            else
            {
                if (request.FxQuoteId is not null)
                {
                    throw Errors.Validation("fxQuoteId", "No FX quote is needed when both currencies are the same.");
                }

                send = new Money(request.Amount, from.Currency);
                receive = send;
            }

            ledger.Reserve(from.Id, send.Amount);

            var payment = new PaymentRecord(
                "pay_" + Guid.NewGuid().ToString("N")[..12],
                request.Type,
                PaymentStatus.PendingApproval,
                from.Id,
                request.BeneficiaryId,
                request.ToAccountId,
                request.DestinationCurrency,
                send,
                receive,
                request.FixedSide,
                requiresFx,
                request.PurposeCode,
                request.Reference,
                request.CapturedByUserId,
                request.CapturedByName,
                now,
                acceptance,
                null,
                null,
                [new TimelineEntry(now, "Captured", request.CapturedByName, acceptance is null ? null : $"Indicative FX rate {acceptance.Rate}")]);

            payments[payment.Id] = payment;
            return ValueTask.FromResult(payment);
        }
    }

    public ValueTask<IReadOnlyList<PaymentRecord>> ListAsync(PaymentFilter filter, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            IReadOnlyList<PaymentRecord> result = payments.Values
                .Where(p => filter.Status is null || p.Status == filter.Status)
                .Where(p => filter.Type is null || p.Type == filter.Type)
                .OrderByDescending(p => p.CapturedAt)
                .ToList();
            return ValueTask.FromResult(result);
        }
    }

    public ValueTask<PaymentRecord> GetAsync(string paymentId, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return ValueTask.FromResult(Find(paymentId));
        }
    }

    public ValueTask<PaymentRecord> ApproveAsync(
        string paymentId,
        string approverUserId,
        string approverName,
        string? fxQuoteId,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var payment = Find(paymentId);
            RequirePendingApproval(payment, "approved");
            RequireFourEyes(payment, approverUserId);

            var now = timeProvider.GetUtcNow();
            var send = payment.SendAmount;
            var receive = payment.ReceiveAmount;
            var acceptance = payment.Fx;
            string? note = null;

            if (payment.RequiresFx)
            {
                if (fxQuoteId is null)
                {
                    throw Errors.FxQuoteRequired("Approving a payment that converts currency needs a fresh FX quote.");
                }

                var fixedAmount = payment.FixedSide == FixedSide.Sell ? send.Amount : receive.Amount;
                var quote = fx.Redeem(
                    fxQuoteId,
                    new FxQuoteMatch(send.Currency, receive.Currency, payment.FixedSide, fixedAmount));

                ledger.AdjustReservation(payment.FromAccountId, quote.SellAmount - send.Amount);
                send = new Money(quote.SellAmount, send.Currency);
                receive = new Money(quote.BuyAmount, receive.Currency);
                acceptance = new FxAcceptance(quote.Rate, quote.QuoteId, approverName, now);
                note = $"FX rate {quote.Rate}";
            }
            else if (fxQuoteId is not null)
            {
                throw Errors.Validation("fxQuoteId", "No FX quote is needed for this payment.");
            }

            var updated = payment with
            {
                Status = PaymentStatus.Processing,
                SendAmount = send,
                ReceiveAmount = receive,
                Fx = acceptance,
                ProcessingStartedAt = now,
                Timeline = [.. payment.Timeline, new TimelineEntry(now, "Approved", approverName, note)]
            };
            payments[updated.Id] = updated;
            return ValueTask.FromResult(updated);
        }
    }

    public ValueTask<PaymentRecord> RejectAsync(
        string paymentId,
        string approverUserId,
        string approverName,
        string reason,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var payment = Find(paymentId);
            RequirePendingApproval(payment, "rejected");
            RequireFourEyes(payment, approverUserId);

            ledger.Release(payment.FromAccountId, payment.SendAmount.Amount);
            var updated = payment with
            {
                Status = PaymentStatus.Rejected,
                RejectionReason = reason,
                Timeline = [.. payment.Timeline, new TimelineEntry(timeProvider.GetUtcNow(), "Rejected", approverName, reason)]
            };
            payments[updated.Id] = updated;
            return ValueTask.FromResult(updated);
        }
    }

    public ValueTask<PaymentRecord> CancelAsync(
        string paymentId,
        string userId,
        string userName,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var payment = Find(paymentId);
            RequirePendingApproval(payment, "cancelled");
            if (payment.CapturedByUserId != userId)
            {
                throw Errors.Forbidden("Only the person who captured a payment can cancel it.");
            }

            ledger.Release(payment.FromAccountId, payment.SendAmount.Amount);
            var updated = payment with
            {
                Status = PaymentStatus.Cancelled,
                Timeline = [.. payment.Timeline, new TimelineEntry(timeProvider.GetUtcNow(), "Cancelled", userName)]
            };
            payments[updated.Id] = updated;
            return ValueTask.FromResult(updated);
        }
    }

    int IProcessingSimulation.Advance(DateTimeOffset startedOnOrBefore)
    {
        lock (gate)
        {
            var due = payments.Values
                .Where(p => p.Status == PaymentStatus.Processing && p.ProcessingStartedAt <= startedOnOrBefore)
                .ToList();

            foreach (var payment in due)
            {
                ledger.Settle(payment.FromAccountId, payment.SendAmount.Amount);
                if (payment.Type == PaymentType.InternationalTransfer && payment.ToAccountId is not null)
                {
                    ledger.Credit(payment.ToAccountId, payment.ReceiveAmount.Amount);
                }

                payments[payment.Id] = payment with
                {
                    Status = PaymentStatus.Completed,
                    Timeline = [.. payment.Timeline, new TimelineEntry(timeProvider.GetUtcNow(), "Completed")]
                };
            }

            return due.Count;
        }
    }

    private PaymentRecord Find(string paymentId) =>
        payments.TryGetValue(paymentId, out var payment) ? payment : throw Errors.NotFound("Payment", paymentId);

    private static void RequirePendingApproval(PaymentRecord payment, string action)
    {
        if (payment.Status != PaymentStatus.PendingApproval)
        {
            throw Errors.InvalidStatus($"Payment {payment.Id} is {payment.Status} and cannot be {action}.");
        }
    }

    private static void RequireFourEyes(PaymentRecord payment, string approverUserId)
    {
        if (payment.CapturedByUserId == approverUserId)
        {
            throw Errors.SelfApprovalForbidden();
        }
    }

    private void SeedPayments()
    {
        var now = timeProvider.GetUtcNow();
        const string alex = "usr_alex";
        const string jordan = "usr_jordan";
        const string alexName = "Alex Morgan";
        const string samName = "Sam Okafor";
        const string jordanName = "Jordan Reeves";

        PaymentRecord Make(
            string id, PaymentType type, PaymentStatus status, string fromAccountId, string? beneficiaryId, string? toAccountId,
            Money send, Money receive, bool requiresFx, string? purpose, string reference, string capturerId, string capturerName,
            TimeSpan capturedAgo, decimal? rate, string? rejectionReason, params TimelineEntry[] timeline)
        {
            var capturedAt = now - capturedAgo;
            var fxAcceptance = rate is null ? null : new FxAcceptance(rate.Value, "fxq_seed_" + id[4..], capturerName, capturedAt);
            return new PaymentRecord(
                id, type, status, fromAccountId, beneficiaryId, toAccountId, receive.Currency, send, receive, FixedSide.Sell,
                requiresFx, purpose, reference, capturerId, capturerName, capturedAt, fxAcceptance, rejectionReason, null,
                [new TimelineEntry(capturedAt, "Captured", capturerName), .. timeline]);
        }

        PaymentRecord[] seeded =
        [
            Make("pay_4Kd8rWq2Lm0a", PaymentType.BeneficiaryPayment, PaymentStatus.PendingApproval, "acc_gbp_operating", "ben_de_brandt", null,
                new Money(4_200.00m, "GBP"), new Money(4_872.00m, "EUR"), true, "GDSP", "INV-2026-0412", alex, alexName,
                TimeSpan.FromHours(3), 1.16m, null),
            Make("pay_9Tz1xNc5Vb7e", PaymentType.BeneficiaryPayment, PaymentStatus.PendingApproval, "acc_eur_trading", "ben_de_brandt", null,
                new Money(8_750.00m, "EUR"), new Money(8_750.00m, "EUR"), false, "PSVC", "CONSULTING-OCT", jordan, jordanName,
                TimeSpan.FromMinutes(90), null, null),
            Make("pay_2Qe6mHs8Yd3f", PaymentType.InternationalTransfer, PaymentStatus.PendingApproval, "acc_gbp_operating", null, "acc_usd_trading",
                new Money(5_000.00m, "GBP"), new Money(6_350.00m, "USD"), true, null, "Fund US trading float", alex, alexName,
                TimeSpan.FromMinutes(45), 1.27m, null),
            Make("pay_6Ws3cJk0Pt5g", PaymentType.BeneficiaryPayment, PaymentStatus.Completed, "acc_gbp_operating", "ben_za_karoo", null,
                new Money(1_800.00m, "GBP"), new Money(41_130.00m, "ZAR"), true, "GDSP", "PO-77120", alex, alexName,
                TimeSpan.FromDays(3), 22.85m, null,
                new TimelineEntry(now - TimeSpan.FromDays(3) + TimeSpan.FromHours(1), "Approved", samName, "FX rate 22.85"),
                new TimelineEntry(now - TimeSpan.FromDays(3) + TimeSpan.FromHours(1) + TimeSpan.FromSeconds(8), "Completed")),
            Make("pay_1Ym7vBn4Rc9h", PaymentType.BeneficiaryPayment, PaymentStatus.Rejected, "acc_gbp_operating", "ben_jp_tsukimi", null,
                new Money(2_300.00m, "GBP"), new Money(442_520m, "JPY"), true, "GDSP", "INV-JP-0099", jordan, jordanName,
                TimeSpan.FromDays(2), 192.40m, "Invoice number does not match the purchase order.",
                new TimelineEntry(now - TimeSpan.FromDays(2) + TimeSpan.FromHours(2), "Rejected", samName, "Invoice number does not match the purchase order.")),
            Make("pay_8Hc4sTd1Zk6j", PaymentType.BeneficiaryPayment, PaymentStatus.Cancelled, "acc_usd_trading", "ben_us_lakeshore", null,
                new Money(3_200.00m, "USD"), new Money(3_200.00m, "USD"), false, "RENT", "WAREHOUSE-Q4", alex, alexName,
                TimeSpan.FromDays(1), null, null,
                new TimelineEntry(now - TimeSpan.FromDays(1) + TimeSpan.FromMinutes(20), "Cancelled", alexName)),
            Make("pay_3Nf5gLp7Wa2k", PaymentType.InternationalTransfer, PaymentStatus.Completed, "acc_gbp_operating", null, "acc_eur_trading",
                new Money(10_000.00m, "GBP"), new Money(11_600.00m, "EUR"), true, null, "Top up EUR trading", jordan, jordanName,
                TimeSpan.FromDays(5), 1.16m, null,
                new TimelineEntry(now - TimeSpan.FromDays(5) + TimeSpan.FromHours(1), "Approved", samName, "FX rate 1.16"),
                new TimelineEntry(now - TimeSpan.FromDays(5) + TimeSpan.FromHours(1) + TimeSpan.FromSeconds(8), "Completed"))
        ];

        foreach (var payment in seeded)
        {
            payments[payment.Id] = payment;
        }
    }
}
