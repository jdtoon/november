using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;

namespace November.Bff.Api.Features.Payments;

public sealed record GetPayment(string Id) : IQuery<PaymentDetail>;

public sealed class GetPaymentValidator : AbstractValidator<GetPayment>
{
    public GetPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetPaymentHandler(IPaymentsService payments, PaymentViewBuilder views)
    : IQueryHandler<GetPayment, PaymentDetail>
{
    public async ValueTask<PaymentDetail> Handle(GetPayment query, CancellationToken cancellationToken)
    {
        var record = await payments.GetAsync(query.Id, cancellationToken);
        return await views.DetailAsync(record, cancellationToken);
    }
}
