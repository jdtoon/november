using Mediator;
using November.Bff.Api.Infrastructure.Auth;

namespace November.Bff.Api.Features.Session;

public sealed record ListPersonas : IQuery<IReadOnlyList<Persona>>;

public sealed class ListPersonasHandler(IPersonaDirectory personas) : IQueryHandler<ListPersonas, IReadOnlyList<Persona>>
{
    public ValueTask<IReadOnlyList<Persona>> Handle(ListPersonas query, CancellationToken cancellationToken) =>
        ValueTask.FromResult(personas.All);
}
