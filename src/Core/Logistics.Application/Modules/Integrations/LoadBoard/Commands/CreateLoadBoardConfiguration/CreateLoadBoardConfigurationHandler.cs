using Logistics.Application.Abstractions;
using Logistics.Domain.Entities;
using Logistics.Domain.Persistence;
using Logistics.Shared.Models;
using Microsoft.Extensions.Logging;
using Logistics.Application.Abstractions.LoadBoard;

namespace Logistics.Application.Modules.Integrations.LoadBoard.Commands;

internal sealed class CreateLoadBoardConfigurationHandler(
    ITenantUnitOfWork tenantUow,
    ILoadBoardProviderFactory providerFactory,
    ILoadBoardCredentialProtector credentialProtector,
    ILogger<CreateLoadBoardConfigurationHandler> logger)
    : IAppRequestHandler<CreateLoadBoardConfigurationCommand, Result>
{
    public async Task<Result> Handle(CreateLoadBoardConfigurationCommand req, CancellationToken ct)
    {
        // Check if provider is supported
        if (!providerFactory.IsProviderSupported(req.ProviderType))
        {
            return Result.Fail($"Load board provider '{req.ProviderType}' is not supported");
        }

        // Check if configuration already exists for this provider
        var existingConfig = await tenantUow.Repository<LoadBoardConfiguration>()
            .GetAsync(c => c.ProviderType == req.ProviderType, ct);

        if (existingConfig is not null)
        {
            return Result.Fail(
                $"Configuration for {req.ProviderType} already exists. Please update the existing configuration.");
        }

        // Validate credentials with the provider
        var providerService = providerFactory.GetProvider(req.ProviderType);
        var validation = await providerService.ValidateCredentialsAsync(req.ApiKey, req.ApiSecret);

        if (!validation.IsValid)
        {
            return Result.Fail("Invalid API credentials. Please verify your API key and try again.");
        }

        // Create the configuration
        var config = new LoadBoardConfiguration
        {
            ProviderType = req.ProviderType,
            ApiKey = credentialProtector.Protect(req.ApiKey) ?? string.Empty,
            ApiSecret = credentialProtector.Protect(req.ApiSecret),
            AccessToken = credentialProtector.Protect(validation.AccessToken),
            RefreshToken = credentialProtector.Protect(validation.RefreshToken),
            TokenExpiresAt = validation.ExpiresAt,
            WebhookSecret = credentialProtector.Protect(req.WebhookSecret),
            LastConnectionTestedAt = DateTime.UtcNow,
            LastConnectionError = null,
            ExternalAccountId = validation.ExternalAccountId,
            CompanyDotNumber = req.CompanyDotNumber,
            CompanyMcNumber = req.CompanyMcNumber,
            IsActive = true
        };

        await tenantUow.Repository<LoadBoardConfiguration>().AddAsync(config, ct);
        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation("Created load board provider configuration for {ProviderType}", req.ProviderType);
        return Result.Ok();
    }
}
