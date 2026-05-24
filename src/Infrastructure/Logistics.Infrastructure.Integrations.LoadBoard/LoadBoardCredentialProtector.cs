using Logistics.Application.Abstractions.LoadBoard;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Logistics.Infrastructure.Integrations.LoadBoard;

internal sealed class LoadBoardCredentialProtector(
    IDataProtectionProvider dataProtectionProvider,
    ILogger<LoadBoardCredentialProtector> logger)
    : ILoadBoardCredentialProtector
{
    private const string Prefix = "dp:";
    private readonly IDataProtector protector =
        dataProtectionProvider.CreateProtector("DispatchLoad.LoadBoard.ProviderCredentials.v1");

    public string? Protect(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return value;
        }

        return Prefix + protector.Protect(value);
    }

    public string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            return protector.Unprotect(value[Prefix.Length..]);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to unprotect load board credential");
            return null;
        }
    }
}
