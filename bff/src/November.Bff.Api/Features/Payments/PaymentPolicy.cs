using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Features.Payments;

public static class PaymentPolicy
{
    public static bool CanApprove(ICurrentUser user, PaymentRecord payment) =>
        user.IsInRole(Roles.Approver)
        && payment.CapturedByUserId != user.UserId
        && payment.Status == PaymentStatus.PendingApproval;

    public static bool CanCancel(ICurrentUser user, PaymentRecord payment) =>
        payment.CapturedByUserId == user.UserId
        && payment.Status == PaymentStatus.PendingApproval;

    public static FixedSide ResolveFixedSide(string sourceCurrency, string destinationCurrency, string requestCurrency)
    {
        if (requestCurrency == sourceCurrency)
        {
            return FixedSide.Sell;
        }

        if (requestCurrency == destinationCurrency)
        {
            return FixedSide.Buy;
        }

        throw Errors.Validation(
            "currency",
            $"Currency must be {sourceCurrency} (the amount sent) or {destinationCurrency} (the amount received).");
    }
}
