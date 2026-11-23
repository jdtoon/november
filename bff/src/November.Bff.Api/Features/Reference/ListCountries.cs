using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Reference;

public sealed record ListCountries : IQuery<IReadOnlyList<Country>>;

public sealed class ListCountriesHandler(IReferenceDataService reference) : IQueryHandler<ListCountries, IReadOnlyList<Country>>
{
    public ValueTask<IReadOnlyList<Country>> Handle(ListCountries query, CancellationToken cancellationToken) =>
        reference.GetCountriesAsync(cancellationToken);
}
