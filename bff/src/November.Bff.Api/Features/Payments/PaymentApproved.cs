using Mediator;

namespace November.Bff.Api.Features.Payments;

public sealed record PaymentApproved(string PaymentId, string ApprovedBy, DateTimeOffset ApprovedAt) : INotification;

public sealed class PaymentApprovedAuditLogHandler(ILogger<PaymentApprovedAuditLogHandler> logger)
    : INotificationHandler<PaymentApproved>
{
    public ValueTask Handle(PaymentApproved notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "AUDIT payment {PaymentId} approved by {ApprovedBy} at {ApprovedAt:o}",
            notification.PaymentId,
            notification.ApprovedBy,
            notification.ApprovedAt);
        return ValueTask.CompletedTask;
    }
}
