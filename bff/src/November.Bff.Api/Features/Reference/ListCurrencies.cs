using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Reference;

public sealed record ListCurrencies : IQuery<IReadOnlyList<Currency>>;

public sealed class ListCurrenciesHandler(IReferenceDataService reference) : IQueryHandler<ListCurrencies, IReadOnlyList<Currency>>
{
    public ValueTask<IReadOnlyList<Currency>> Handle(ListCurrencies query, CancellationToken cancellationToken) =>
        reference.GetCurrenciesAsync(cancellationToken);
}
