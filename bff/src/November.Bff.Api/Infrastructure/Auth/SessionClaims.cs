using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace November.Bff.Api.Infrastructure.Auth;

public static class SessionClaims
{
    public const string CompanyId = "company_id";
    public const string CompanyName = "company_name";

    public static ClaimsPrincipal CreatePrincipal(string userId, string displayName, IEnumerable<string> roles, CompanyRef company)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName),
            new(CompanyId, company.Id),
            new(CompanyName, company.Name)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
