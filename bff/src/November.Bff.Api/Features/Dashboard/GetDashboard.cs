using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Features.Payments;
using November.Bff.Api.Infrastructure.Auth;

namespace November.Bff.Api.Features.Dashboard;

public sealed record GetDashboard : IQuery<DashboardResponse>;

public sealed record DashboardResponse(
    IReadOnlyList<Account> Accounts,
    int PendingApprovalCount,
    int AwaitingMyApprovalCount,
    int IncomingActionRequiredCount,
    IReadOnlyList<PaymentSummary> RecentPayments);

public sealed class GetDashboardHandler(
    IAccountsService accounts,
    IPaymentsService payments,
    IIncomingPaymentsService incomingPayments,
    PaymentViewBuilder views,
    ICurrentUser user) : IQueryHandler<GetDashboard, DashboardResponse>
{
    private const int RecentPaymentCount = 5;

    public async ValueTask<DashboardResponse> Handle(GetDashboard query, CancellationToken cancellationToken)
    {
        var accountsTask = accounts.ListAsync(cancellationToken).AsTask();
        var paymentsTask = payments.ListAsync(new PaymentFilter(null, null), cancellationToken).AsTask();
        var incomingTask = incomingPayments.ListAsync(IncomingStatus.ActionRequired, cancellationToken).AsTask();
        await Task.WhenAll(accountsTask, paymentsTask, incomingTask);

        var allPayments = await paymentsTask;
        var recent = await views.SummariesAsync(allPayments.Take(RecentPaymentCount), cancellationToken);

        return new DashboardResponse(
            await accountsTask,
            allPayments.Count(p => p.Status == PaymentStatus.PendingApproval),
            allPayments.Count(p => PaymentPolicy.CanApprove(user, p)),
            (await incomingTask).Count,
            recent);
    }
}
