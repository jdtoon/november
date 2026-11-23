using FluentValidation;
using Mediator;
using November.Bff.Api.Downstream;

namespace November.Bff.Api.Features.Incoming;

public sealed record GetIncomingPayment(string Id) : IQuery<IncomingPaymentDetail>;

public sealed class GetIncomingPaymentValidator : AbstractValidator<GetIncomingPayment>
{
    public GetIncomingPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetIncomingPaymentHandler(IIncomingPaymentsService incoming, IncomingViewBuilder views)
    : IQueryHandler<GetIncomingPayment, IncomingPaymentDetail>
{
    public async ValueTask<IncomingPaymentDetail> Handle(GetIncomingPayment query, CancellationToken cancellationToken)
    {
        var record = await incoming.GetAsync(query.Id, cancellationToken);
        return await views.DetailAsync(record, cancellationToken);
    }
}
