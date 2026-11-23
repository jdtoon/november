using System.Security.Claims;

namespace November.Bff.Api.Infrastructure.Auth;

public sealed record CompanyRef(string Id, string Name);

public interface ICurrentUser
{
    string UserId { get; }

    string DisplayName { get; }

    IReadOnlyList<string> Roles { get; }

    CompanyRef Company { get; }

    bool IsInRole(string role);
}

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
            ? user
            : throw new InvalidOperationException("There is no authenticated user on the current request.");

    public string UserId => Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public string DisplayName => Principal.FindFirstValue(ClaimTypes.Name)!;

    public IReadOnlyList<string> Roles => Principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

    public CompanyRef Company => new(
        Principal.FindFirstValue(SessionClaims.CompanyId)!,
        Principal.FindFirstValue(SessionClaims.CompanyName)!);

    public bool IsInRole(string role) => Principal.IsInRole(role);
}
