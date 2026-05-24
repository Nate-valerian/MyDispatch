namespace Logistics.Application.Abstractions.LoadBoard;

public record LoadBoardCredentialValidationResult
{
    public bool IsValid { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? ExternalAccountId { get; init; }

    public static LoadBoardCredentialValidationResult Valid(
        string? accessToken = null,
        string? refreshToken = null,
        DateTime? expiresAt = null,
        string? externalAccountId = null) =>
        new()
        {
            IsValid = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            ExternalAccountId = externalAccountId
        };

    public static LoadBoardCredentialValidationResult Invalid() => new() { IsValid = false };
}
