using Microsoft.AspNetCore.Authentication.Cookies;

namespace November.Bff.Api.Infrastructure.Auth;

public static class AuthExtensions
{
    public const string CookieName = "november.session";

    public static IServiceCollection AddBffAuthentication(this IServiceCollection services, AuthOptions auth)
    {
        if (auth.Mode != AuthMode.Mock)
        {
            throw new NotSupportedException(
                $"Auth:Mode '{auth.Mode}' is not implemented in this reference. See docs/bff/README.md, 'Swapping mock auth for Entra ID'.");
        }

        services.AddSingleton<IPersonaDirectory, MockPersonaDirectory>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<TimeProvider>((options, timeProvider) => options.TimeProvider = timeProvider);

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Capturer, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Capturer))
            .AddPolicy(Policies.Approver, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Approver));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
