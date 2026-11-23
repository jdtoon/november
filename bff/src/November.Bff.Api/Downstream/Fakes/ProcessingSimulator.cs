using Microsoft.Extensions.Options;

namespace November.Bff.Api.Downstream.Fakes;

public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    public int ProcessingDelaySeconds { get; set; } = 8;
}

internal sealed class ProcessingSimulator(
    IEnumerable<IProcessingSimulation> simulations,
    IOptions<SimulationOptions> options,
    TimeProvider timeProvider,
    ILogger<ProcessingSimulator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(options.Value.ProcessingDelaySeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var cutoff = timeProvider.GetUtcNow() - delay;
                foreach (var simulation in simulations)
                {
                    var advanced = simulation.Advance(cutoff);
                    if (advanced > 0)
                    {
                        logger.LogInformation("{Simulation} advanced {Count} item(s) out of Processing",
                            simulation.GetType().Name, advanced);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
