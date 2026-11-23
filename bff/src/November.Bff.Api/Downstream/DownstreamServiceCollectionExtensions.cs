using November.Bff.Api.Downstream.Fakes;

namespace November.Bff.Api.Downstream;

public static class DownstreamServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryDownstream(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SimulationOptions>()
            .Bind(configuration.GetSection(SimulationOptions.SectionName))
            .Validate(o => o.ProcessingDelaySeconds >= 0, "Simulation:ProcessingDelaySeconds must not be negative.")
            .ValidateOnStart();

        services.AddSingleton<InMemoryAccountsService>();
        services.AddSingleton<IAccountsService>(sp => sp.GetRequiredService<InMemoryAccountsService>());
        services.AddSingleton<IAccountLedger>(sp => sp.GetRequiredService<InMemoryAccountsService>());

        services.AddSingleton<InMemoryFxService>();
        services.AddSingleton<IFxService>(sp => sp.GetRequiredService<InMemoryFxService>());
        services.AddSingleton<IFxQuoteRedeemer>(sp => sp.GetRequiredService<InMemoryFxService>());

        services.AddSingleton<InMemoryPaymentsService>();
        services.AddSingleton<IPaymentsService>(sp => sp.GetRequiredService<InMemoryPaymentsService>());
        services.AddSingleton<IProcessingSimulation>(sp => sp.GetRequiredService<InMemoryPaymentsService>());

        services.AddSingleton<InMemoryIncomingPaymentsService>();
        services.AddSingleton<IIncomingPaymentsService>(sp => sp.GetRequiredService<InMemoryIncomingPaymentsService>());
        services.AddSingleton<IProcessingSimulation>(sp => sp.GetRequiredService<InMemoryIncomingPaymentsService>());

        services.AddSingleton<IBeneficiariesService, InMemoryBeneficiariesService>();
        services.AddSingleton<IReferenceDataService, InMemoryReferenceDataService>();

        services.AddHostedService<ProcessingSimulator>();
        return services;
    }
}
