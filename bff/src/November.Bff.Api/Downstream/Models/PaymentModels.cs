namespace November.Bff.Api.Downstream.Models;

public sealed record TimelineEntry(DateTimeOffset At, string Event, string? By = null, string? Note = null);

public sealed record PaymentRecord(
    string Id,
    PaymentType Type,
    PaymentStatus Status,
    string FromAccountId,
    string? BeneficiaryId,
    string? ToAccountId,
    string DestinationCurrency,
    Money SendAmount,
    Money ReceiveAmount,
    FixedSide FixedSide,
    bool RequiresFx,
    string? PurposeCode,
    string Reference,
    string CapturedByUserId,
    string CapturedByName,
    DateTimeOffset CapturedAt,
    FxAcceptance? Fx,
    string? RejectionReason,
    DateTimeOffset? ProcessingStartedAt,
    IReadOnlyList<TimelineEntry> Timeline);

public sealed record NewPayment(
    PaymentType Type,
    string FromAccountId,
    string? BeneficiaryId,
    string? ToAccountId,
    string DestinationCurrency,
    FixedSide FixedSide,
    decimal Amount,
    string? PurposeCode,
    string Reference,
    string? FxQuoteId,
    string CapturedByUserId,
    string CapturedByName);

public sealed record PaymentFilter(PaymentStatus? Status, PaymentType? Type);
