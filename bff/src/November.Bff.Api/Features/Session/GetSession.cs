using Mediator;
using November.Bff.Api.Infrastructure.Auth;

namespace November.Bff.Api.Features.Session;

public sealed record GetSession : IQuery<SessionResponse>;

public sealed class GetSessionHandler(ICurrentUser user) : IQueryHandler<GetSession, SessionResponse>
{
    public ValueTask<SessionResponse> Handle(GetSession query, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new SessionResponse(
            user.UserId,
            user.DisplayName,
            user.Roles,
            new CompanyResponse(user.Company.Id, user.Company.Name)));
}
