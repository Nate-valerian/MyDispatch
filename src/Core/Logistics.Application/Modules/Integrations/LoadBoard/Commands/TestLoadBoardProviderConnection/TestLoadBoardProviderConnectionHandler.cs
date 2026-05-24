using Logistics.Application.Abstractions;
using Logistics.Application.Abstractions.LoadBoard;
using Logistics.Domain.Entities;
using Logistics.Domain.Persistence;
using Logistics.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Logistics.Application.Modules.Integrations.LoadBoard.Commands;

internal sealed class TestLoadBoardProviderConnectionHandler(
    ITenantUnitOfWork tenantUow,
    ILoadBoardProviderFactory providerFactory,
    ILoadBoardCredentialProtector credentialProtector,
    ILogger<TestLoadBoardProviderConnectionHandler> logger)
    : IAppRequestHandler<TestLoadBoardProviderConnectionCommand, Result<LoadBoardProviderConnectionTestResultDto>>
{
    public async Task<Result<LoadBoardProviderConnectionTestResultDto>> Handle(
        TestLoadBoardProviderConnectionCommand req,
        CancellationToken ct)
    {
        var config = await tenantUow.Repository<LoadBoardConfiguration>().GetByIdAsync(req.ProviderId, ct);
        if (config is null)
        {
            return Result<LoadBoardProviderConnectionTestResultDto>.Fail("Load board provider configuration not found");
        }

        var testedAt = DateTime.UtcNow;
        var provider = providerFactory.GetProvider(config.ProviderType);
        var validation = await provider.ValidateCredentialsAsync(
            credentialProtector.Unprotect(config.ApiKey) ?? string.Empty,
            credentialProtector.Unprotect(config.ApiSecret));

        config.LastConnectionTestedAt = testedAt;

        if (!validation.IsValid)
        {
            config.LastConnectionError = $"Unable to connect to {config.ProviderType}. Verify the provider credentials and API access.";
            await tenantUow.SaveChangesAsync(ct);

            logger.LogWarning("Load board provider connection test failed for {ProviderType}", config.ProviderType);

            return Result<LoadBoardProviderConnectionTestResultDto>.Ok(new LoadBoardProviderConnectionTestResultDto
            {
                ProviderId = config.Id,
                ProviderType = config.ProviderType,
                IsConnected = false,
                TestedAt = testedAt,
                ErrorMessage = config.LastConnectionError
            });
        }

        config.AccessToken = credentialProtector.Protect(validation.AccessToken);
        config.RefreshToken = credentialProtector.Protect(validation.RefreshToken);
        config.TokenExpiresAt = validation.ExpiresAt;
        config.ExternalAccountId = validation.ExternalAccountId;
        config.LastConnectionError = null;

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation("Load board provider connection test succeeded for {ProviderType}", config.ProviderType);

        return Result<LoadBoardProviderConnectionTestResultDto>.Ok(new LoadBoardProviderConnectionTestResultDto
        {
            ProviderId = config.Id,
            ProviderType = config.ProviderType,
            IsConnected = true,
            TestedAt = testedAt
        });
    }
}
