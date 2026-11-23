using System.Text.Json.Serialization;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using November.Bff.Api.Downstream;
using November.Bff.Api.Features.Accounts;
using November.Bff.Api.Features.Beneficiaries;
using November.Bff.Api.Features.Dashboard;
using November.Bff.Api.Features.Fx;
using November.Bff.Api.Features.Incoming;
using November.Bff.Api.Features.Payments;
using November.Bff.Api.Features.Reference;
using November.Bff.Api.Features.Session;
using November.Bff.Api.Infrastructure.Auth;
using November.Bff.Api.Infrastructure.Behaviors;
using November.Bff.Api.Infrastructure.ErrorHandling;
using November.Bff.Api.Infrastructure.Health;
using November.Bff.Api.Infrastructure.Http;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddBffAuthentication(authOptions);
builder.Services.AddInMemoryDownstream(builder.Configuration);

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<PaymentViewBuilder>();
builder.Services.AddScoped<IncomingViewBuilder>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var problem = context.ProblemDetails;
        if (!problem.Extensions.ContainsKey("code"))
        {
            var code = Problems.DefaultCodeFor(problem.Status ?? StatusCodes.Status500InternalServerError);
            problem.Extensions["code"] = code;
            problem.Type = Problems.TypeFor(code);
        }
    };
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<DownstreamReadinessCheck>("downstream", tags: ["ready"]);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api").AddEndpointFilter<CsrfHeaderFilter>();
api.MapSessionEndpoints(authOptions.Mode);
api.MapDashboardEndpoints();
api.MapReferenceEndpoints();
api.MapAccountsEndpoints();
api.MapBeneficiariesEndpoints();
api.MapFxEndpoints();
api.MapPaymentsEndpoints();
api.MapIncomingEndpoints();

app.Run();

public partial class Program;
