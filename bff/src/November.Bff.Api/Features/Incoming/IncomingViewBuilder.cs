using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Incoming;

public sealed class IncomingViewBuilder(IAccountsService accounts, IReferenceDataService reference)
{
    private readonly Dictionary<string, Account> accountCache = [];

    public async ValueTask<IReadOnlyList<IncomingPaymentSummary>> SummariesAsync(
        IEnumerable<IncomingPaymentRecord> records,
        CancellationToken cancellationToken)
    {
        var summaries = new List<IncomingPaymentSummary>();
        foreach (var record in records)
        {
            var account = await AccountAsync(record.CreditAccountId, cancellationToken);
            summaries.Add(new IncomingPaymentSummary(
                record.Id,
                record.SenderName,
                record.SenderCountry,
                record.Amount,
                account.Name,
                record.Status,
                record.ReceivedAt,
                record.ActionsRequired));
        }

        return summaries;
    }

    public async ValueTask<IncomingPaymentDetail> DetailAsync(IncomingPaymentRecord record, CancellationToken cancellationToken)
    {
        var account = await AccountAsync(record.CreditAccountId, cancellationToken);
        var purpose = record.PurposeCode is null ? null : await reference.FindPurposeCodeAsync(record.PurposeCode, cancellationToken);

        return new IncomingPaymentDetail(
            record.Id,
            record.SenderName,
            record.SenderCountry,
            record.Amount,
            account.Name,
            record.Status,
            record.ReceivedAt,
            record.ActionsRequired,
            new BankRef(record.SenderBankName, record.SenderBankSwiftBic),
            record.SenderReference,
            new AccountRef(account.Id, account.Name, account.Currency),
            purpose,
            record.CreditedAmount,
            record.Fx,
            record.Timeline);
    }

    private async ValueTask<Account> AccountAsync(string id, CancellationToken cancellationToken)
    {
        if (!accountCache.TryGetValue(id, out var account))
        {
            account = await accounts.GetAsync(id, cancellationToken);
            accountCache[id] = account;
        }

        return account;
    }
}
