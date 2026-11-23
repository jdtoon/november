using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Beneficiaries;

public sealed record GetBeneficiary(string Id) : IQuery<Beneficiary>;

public sealed class GetBeneficiaryValidator : AbstractValidator<GetBeneficiary>
{
    public GetBeneficiaryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetBeneficiaryHandler(IBeneficiariesService beneficiaries) : IQueryHandler<GetBeneficiary, Beneficiary>
{
    public ValueTask<Beneficiary> Handle(GetBeneficiary query, CancellationToken cancellationToken) =>
        beneficiaries.GetAsync(query.Id, cancellationToken);
}
