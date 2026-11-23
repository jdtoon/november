using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Incoming;

public sealed record ListIncomingPayments(IncomingStatus? Status) : IQuery<IReadOnlyList<IncomingPaymentSummary>>;

public sealed class ListIncomingPaymentsHandler(IIncomingPaymentsService incoming, IncomingViewBuilder views)
    : IQueryHandler<ListIncomingPayments, IReadOnlyList<IncomingPaymentSummary>>
{
    public async ValueTask<IReadOnlyList<IncomingPaymentSummary>> Handle(
        ListIncomingPayments query,
        CancellationToken cancellationToken)
    {
        var records = await incoming.ListAsync(query.Status, cancellationToken);
        return await views.SummariesAsync(records, cancellationToken);
    }
}
