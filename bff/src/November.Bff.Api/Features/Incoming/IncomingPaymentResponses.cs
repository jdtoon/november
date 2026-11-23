using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Incoming;

public sealed record BankRef(string Name, string SwiftBic);

public sealed record IncomingPaymentSummary(
    string Id,
    string SenderName,
    string SenderCountry,
    Money Amount,
    string CreditAccountName,
    IncomingStatus Status,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<IncomingAction> ActionsRequired);

public sealed record IncomingPaymentDetail(
    string Id,
    string SenderName,
    string SenderCountry,
    Money Amount,
    string CreditAccountName,
    IncomingStatus Status,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<IncomingAction> ActionsRequired,
    BankRef SenderBank,
    string SenderReference,
    AccountRef CreditAccount,
    PurposeCode? Purpose,
    Money? CreditedAmount,
    FxAcceptance? Fx,
    IReadOnlyList<TimelineEntry> Timeline);
