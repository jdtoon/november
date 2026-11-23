using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Payments;

public sealed record PaymentSummary(
    string Id,
    PaymentType Type,
    PaymentStatus Status,
    string FromAccountName,
    string PayeeName,
    Money SendAmount,
    Money ReceiveAmount,
    string Reference,
    string CapturedBy,
    DateTimeOffset CapturedAt,
    bool CanApprove,
    bool CanCancel);

public sealed record PaymentDetail(
    string Id,
    PaymentType Type,
    PaymentStatus Status,
    string FromAccountName,
    string PayeeName,
    Money SendAmount,
    Money ReceiveAmount,
    string Reference,
    string CapturedBy,
    DateTimeOffset CapturedAt,
    bool CanApprove,
    bool CanCancel,
    AccountRef FromAccount,
    Beneficiary? Beneficiary,
    AccountRef? ToAccount,
    PurposeCode? Purpose,
    bool RequiresFx,
    FixedSide FixedSide,
    FxAcceptance? Fx,
    string? RejectionReason,
    IReadOnlyList<TimelineEntry> Timeline);
