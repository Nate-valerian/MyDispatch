using Logistics.Application.Abstractions.LoadBoard;
using Logistics.Domain.Entities;
using Logistics.Domain.Primitives.Enums;
using Microsoft.Extensions.Logging;

namespace Logistics.Application.Modules.Integrations.LoadBoard.Commands;

internal static class LoadBoardProviderCredentials
{
    public static async Task<string?> RefreshIfNeededAsync(
        LoadBoardConfiguration config,
        ILoadBoardProviderFactory providerFactory,
        ILoadBoardCredentialProtector credentialProtector,
        ILogger logger)
    {
        if (config.ProviderType is not (LoadBoardProviderType.Dat or LoadBoardProviderType.Truckstop))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(config.AccessToken)
            && config.TokenExpiresAt > DateTime.UtcNow.AddMinutes(2))
        {
            return null;
        }

        var provider = providerFactory.GetProvider(config.ProviderType);
        var validation = await provider.ValidateCredentialsAsync(
            credentialProtector.Unprotect(config.ApiKey) ?? string.Empty,
            credentialProtector.Unprotect(config.ApiSecret));
        if (!validation.IsValid)
        {
            logger.LogWarning(
                "Unable to refresh load board credentials for provider {ProviderType}",
                config.ProviderType);

            config.LastConnectionTestedAt = DateTime.UtcNow;
            config.LastConnectionError = $"Unable to refresh credentials for {config.ProviderType}. Please reconnect the provider.";

            return $"Unable to refresh credentials for {config.ProviderType}. Please reconnect the provider.";
        }

        config.AccessToken = credentialProtector.Protect(validation.AccessToken);
        config.RefreshToken = credentialProtector.Protect(validation.RefreshToken);
        config.TokenExpiresAt = validation.ExpiresAt;
        config.ExternalAccountId = validation.ExternalAccountId;
        config.LastConnectionTestedAt = DateTime.UtcNow;
        config.LastConnectionError = null;

        return null;
    }
}
