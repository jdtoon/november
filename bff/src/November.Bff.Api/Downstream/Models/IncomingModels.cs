namespace November.Bff.Api.Downstream.Models;

public sealed record IncomingPaymentRecord(
    string Id,
    string SenderName,
    string SenderCountry,
    string SenderBankName,
    string SenderBankSwiftBic,
    string SenderReference,
    Money Amount,
    string CreditAccountId,
    IncomingStatus Status,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<IncomingAction> ActionsRequired,
    string? PurposeCode,
    Money? CreditedAmount,
    FxAcceptance? Fx,
    DateTimeOffset? ProcessingStartedAt,
    IReadOnlyList<TimelineEntry> Timeline);

public sealed record IncomingResponse(string? PurposeCode, string? FxQuoteId, string RespondedByName);
