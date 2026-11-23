namespace November.Bff.Api.Infrastructure.Auth;

public enum AuthMode
{
    Mock,
    Oidc
}

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public AuthMode Mode { get; set; } = AuthMode.Mock;
}
