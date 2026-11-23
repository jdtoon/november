using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Reference;

public sealed record ListPurposeCodes(PurposeDirection? Direction) : IQuery<IReadOnlyList<PurposeCode>>;

public sealed class ListPurposeCodesHandler(IReferenceDataService reference)
    : IQueryHandler<ListPurposeCodes, IReadOnlyList<PurposeCode>>
{
    public ValueTask<IReadOnlyList<PurposeCode>> Handle(ListPurposeCodes query, CancellationToken cancellationToken) =>
        reference.GetPurposeCodesAsync(query.Direction, cancellationToken);
}
