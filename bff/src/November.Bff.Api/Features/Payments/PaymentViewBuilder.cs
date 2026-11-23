using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.Auth;

namespace November.Bff.Api.Features.Payments;

/// <summary>
/// Turns platform payment records into UI-shaped responses. The platform stores ids, so names, beneficiaries and
/// purpose codes are looked up from other platforms and memoised for the lifetime of the request.
/// </summary>
public sealed class PaymentViewBuilder(
    IAccountsService accounts,
    IBeneficiariesService beneficiaries,
    IReferenceDataService reference,
    ICurrentUser user)
{
    private readonly Dictionary<string, Account> accountCache = [];
    private readonly Dictionary<string, Beneficiary> beneficiaryCache = [];

    public async ValueTask<IReadOnlyList<PaymentSummary>> SummariesAsync(
        IEnumerable<PaymentRecord> records,
        CancellationToken cancellationToken)
    {
        var summaries = new List<PaymentSummary>();
        foreach (var record in records)
        {
            summaries.Add(await SummaryAsync(record, cancellationToken));
        }

        return summaries;
    }

    public async ValueTask<PaymentSummary> SummaryAsync(PaymentRecord record, CancellationToken cancellationToken)
    {
        var from = await AccountAsync(record.FromAccountId, cancellationToken);
        var payeeName = await PayeeNameAsync(record, cancellationToken);

        return new PaymentSummary(
            record.Id,
            record.Type,
            record.Status,
            from.Name,
            payeeName,
            record.SendAmount,
            record.ReceiveAmount,
            record.Reference,
            record.CapturedByName,
            record.CapturedAt,
            PaymentPolicy.CanApprove(user, record),
            PaymentPolicy.CanCancel(user, record));
    }

    public async ValueTask<PaymentDetail> DetailAsync(PaymentRecord record, CancellationToken cancellationToken)
    {
        var from = await AccountAsync(record.FromAccountId, cancellationToken);
        var beneficiary = record.BeneficiaryId is null ? null : await BeneficiaryAsync(record.BeneficiaryId, cancellationToken);
        var to = record.ToAccountId is null ? null : await AccountAsync(record.ToAccountId, cancellationToken);
        var purpose = record.PurposeCode is null ? null : await reference.FindPurposeCodeAsync(record.PurposeCode, cancellationToken);

        return new PaymentDetail(
            record.Id,
            record.Type,
            record.Status,
            from.Name,
            beneficiary?.Name ?? to?.Name ?? string.Empty,
            record.SendAmount,
            record.ReceiveAmount,
            record.Reference,
            record.CapturedByName,
            record.CapturedAt,
            PaymentPolicy.CanApprove(user, record),
            PaymentPolicy.CanCancel(user, record),
            new AccountRef(from.Id, from.Name, from.Currency),
            beneficiary,
            to is null ? null : new AccountRef(to.Id, to.Name, to.Currency),
            purpose,
            record.RequiresFx,
            record.FixedSide,
            record.Fx,
            record.RejectionReason,
            record.Timeline);
    }

    private async ValueTask<string> PayeeNameAsync(PaymentRecord record, CancellationToken cancellationToken)
    {
        if (record.BeneficiaryId is not null)
        {
            return (await BeneficiaryAsync(record.BeneficiaryId, cancellationToken)).Name;
        }

        return record.ToAccountId is null ? string.Empty : (await AccountAsync(record.ToAccountId, cancellationToken)).Name;
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

    private async ValueTask<Beneficiary> BeneficiaryAsync(string id, CancellationToken cancellationToken)
    {
        if (!beneficiaryCache.TryGetValue(id, out var beneficiary))
        {
            beneficiary = await beneficiaries.GetAsync(id, cancellationToken);
            beneficiaryCache[id] = beneficiary;
        }

        return beneficiary;
    }
}
