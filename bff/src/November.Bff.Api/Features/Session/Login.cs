using System.Security.Claims;
using FluentValidation;
using Mediator;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.ErrorHandling;

namespace November.Bff.Api.Features.Session;

public sealed record Login(string PersonaId) : ICommand<LoginResult>;

public sealed record LoginResult(ClaimsPrincipal Principal, SessionResponse Session);

public sealed class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
    }
}

public sealed class LoginHandler(IPersonaDirectory personas) : ICommandHandler<Login, LoginResult>
{
    public ValueTask<LoginResult> Handle(Login command, CancellationToken cancellationToken)
    {
        var persona = personas.Find(command.PersonaId) ?? throw Errors.NotFound("Persona", command.PersonaId);
        var userId = $"usr_{persona.PersonaId}";
        var principal = SessionClaims.CreatePrincipal(userId, persona.DisplayName, persona.Roles, personas.Company);
        var session = new SessionResponse(
            userId,
            persona.DisplayName,
            persona.Roles,
            new CompanyResponse(personas.Company.Id, personas.Company.Name));

        return ValueTask.FromResult(new LoginResult(principal, session));
    }
}
