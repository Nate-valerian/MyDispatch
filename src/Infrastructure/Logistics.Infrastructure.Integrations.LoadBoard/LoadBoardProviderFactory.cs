using Logistics.Domain.Entities;
using Logistics.Domain.Primitives.Enums;
using Logistics.Infrastructure.Integrations.LoadBoard.Providers;
using Logistics.Infrastructure.Integrations.LoadBoard.Providers.Dat;
using Logistics.Infrastructure.Integrations.LoadBoard.Providers.OneTwo3;
using Logistics.Infrastructure.Integrations.LoadBoard.Providers.Truckstop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Logistics.Application.Abstractions.LoadBoard;

namespace Logistics.Infrastructure.Integrations.LoadBoard;

internal class LoadBoardProviderFactory(
    IServiceProvider serviceProvider,
    ILoadBoardCredentialProtector credentialProtector,
    ILogger<LoadBoardProviderFactory> logger)
    : ILoadBoardProviderFactory
{
    public ILoadBoardProviderService GetProvider(LoadBoardProviderType providerType)
    {
        ILoadBoardProviderService service = providerType switch
        {
            LoadBoardProviderType.Dat => serviceProvider.GetRequiredService<DatLoadBoardService>(),
            LoadBoardProviderType.Truckstop => serviceProvider.GetRequiredService<TruckstopLoadBoardService>(),
            LoadBoardProviderType.OneTwo3Loadboard => serviceProvider.GetRequiredService<OneTwo3LoadBoardService>(),
            LoadBoardProviderType.Demo => serviceProvider.GetRequiredService<DemoLoadBoardService>(),
            _ => throw new NotSupportedException($"Load board provider '{providerType}' is not supported.")
        };

        logger.LogDebug("Created load board provider service for {ProviderType}", providerType);
        return service;
    }

    public ILoadBoardProviderService GetProvider(LoadBoardConfiguration configuration)
    {
        var service = GetProvider(configuration.ProviderType);
        service.Initialize(Unprotect(configuration));
        return service;
    }

    public bool IsProviderSupported(LoadBoardProviderType providerType)
    {
        return providerType is LoadBoardProviderType.Dat
            or LoadBoardProviderType.Truckstop
            or LoadBoardProviderType.OneTwo3Loadboard
            or LoadBoardProviderType.Demo;
    }

    private LoadBoardConfiguration Unprotect(LoadBoardConfiguration configuration) => new()
    {
        Id = configuration.Id,
        ProviderType = configuration.ProviderType,
        ApiKey = credentialProtector.Unprotect(configuration.ApiKey) ?? string.Empty,
        ApiSecret = credentialProtector.Unprotect(configuration.ApiSecret),
        AccessToken = credentialProtector.Unprotect(configuration.AccessToken),
        RefreshToken = credentialProtector.Unprotect(configuration.RefreshToken),
        TokenExpiresAt = configuration.TokenExpiresAt,
        WebhookSecret = credentialProtector.Unprotect(configuration.WebhookSecret),
        IsActive = configuration.IsActive,
        LastSyncedAt = configuration.LastSyncedAt,
        ExternalAccountId = configuration.ExternalAccountId,
        CompanyDotNumber = configuration.CompanyDotNumber,
        CompanyMcNumber = configuration.CompanyMcNumber
    };
}
