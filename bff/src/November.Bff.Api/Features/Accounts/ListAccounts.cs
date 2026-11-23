using Mediator;
using November.Bff.Api.Downstream;
using November.Bff.Api.Downstream.Models;

namespace November.Bff.Api.Features.Accounts;

public sealed record ListAccounts : IQuery<IReadOnlyList<Account>>;

public sealed class ListAccountsHandler(IAccountsService accounts) : IQueryHandler<ListAccounts, IReadOnlyList<Account>>
{
    public ValueTask<IReadOnlyList<Account>> Handle(ListAccounts query, CancellationToken cancellationToken) =>
        accounts.ListAsync(cancellationToken);
}
