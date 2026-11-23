using Microsoft.Extensions.Diagnostics.HealthChecks;
using November.Bff.Api.Downstream;

namespace November.Bff.Api.Infrastructure.Health;

public sealed class DownstreamReadinessCheck(IAccountsService accounts, IReferenceDataService reference) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await accounts.ListAsync(cancellationToken);
            await reference.GetCurrenciesAsync(cancellationToken);
            return HealthCheckResult.Healthy("Downstream services answered.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("A downstream service did not answer.", exception);
        }
    }
}
