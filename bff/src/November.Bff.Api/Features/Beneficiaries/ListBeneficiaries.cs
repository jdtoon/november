using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Beneficiaries;

public sealed record ListBeneficiaries : IQuery<IReadOnlyList<Beneficiary>>;

public sealed class ListBeneficiariesHandler(IBeneficiariesService beneficiaries)
    : IQueryHandler<ListBeneficiaries, IReadOnlyList<Beneficiary>>
{
    public ValueTask<IReadOnlyList<Beneficiary>> Handle(ListBeneficiaries query, CancellationToken cancellationToken) =>
        beneficiaries.ListAsync(cancellationToken);
}
