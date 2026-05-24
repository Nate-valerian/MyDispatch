using Logistics.Infrastructure.Integrations.LoadBoard.Providers;
using Logistics.Infrastructure.Integrations.LoadBoard.Providers.Dat;
using Logistics.Infrastructure.Integrations.LoadBoard.Providers.OneTwo3;
using Logistics.Infrastructure.Integrations.LoadBoard.Providers.Truckstop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Logistics.Application.Abstractions.LoadBoard;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Integrations.LoadBoard;

public static class Registrar
{
    /// <summary>
    ///     Add LoadBoard provider integrations (DAT, Truckstop, 123Loadboard, Demo).
    /// </summary>
    public static IServiceCollection AddLoadBoardIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<LoadBoardOptions>(
            configuration.GetSection(LoadBoardOptions.SectionName));
        services.AddDataProtection();

        // LoadBoard providers (with HttpClient for external APIs)
        services.AddHttpClient<DatLoadBoardService>(ConfigureProviderClient);
        services.AddHttpClient<TruckstopLoadBoardService>(ConfigureProviderClient);
        services.AddHttpClient<OneTwo3LoadBoardService>(ConfigureProviderClient);
        services.AddScoped<DemoLoadBoardService>();

        // Factory pattern for provider selection
        services.AddScoped<ILoadBoardCredentialProtector, LoadBoardCredentialProtector>();
        services.AddScoped<ILoadBoardProviderFactory, LoadBoardProviderFactory>();

        return services;
    }

    private static void ConfigureProviderClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<LoadBoardOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds));
    }
}
