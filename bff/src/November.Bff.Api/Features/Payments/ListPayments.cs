using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Payments;

public sealed record ListPayments(PaymentStatus? Status, PaymentType? Type) : IQuery<IReadOnlyList<PaymentSummary>>;

public sealed class ListPaymentsHandler(IPaymentsService payments, PaymentViewBuilder views)
    : IQueryHandler<ListPayments, IReadOnlyList<PaymentSummary>>
{
    public async ValueTask<IReadOnlyList<PaymentSummary>> Handle(ListPayments query, CancellationToken cancellationToken)
    {
        var records = await payments.ListAsync(new PaymentFilter(query.Status, query.Type), cancellationToken);
        return await views.SummariesAsync(records, cancellationToken);
    }
}
