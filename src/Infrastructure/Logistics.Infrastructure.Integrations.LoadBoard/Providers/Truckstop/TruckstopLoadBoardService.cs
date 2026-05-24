using System.Net.Http.Headers;
using System.Text.Json;
using Logistics.Domain.Entities;
using Logistics.Domain.Primitives.Enums;
using Logistics.Infrastructure.Integrations.LoadBoard.Common;
using Logistics.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Logistics.Application.Abstractions.LoadBoard;

namespace Logistics.Infrastructure.Integrations.LoadBoard.Providers.Truckstop;

/// <summary>
///     Truckstop.com Load Board provider implementation.
///     Authentication: OAuth 2.0 (Resource Owner Password grant)
///     Access Token validity: 20 minutes, Refresh Token validity: 6 months
/// </summary>
internal class TruckstopLoadBoardService(
    HttpClient httpClient,
    IOptions<LoadBoardOptions> options,
    ILogger<TruckstopLoadBoardService> logger)
    : ILoadBoardProviderService
{
    private readonly LoadBoardOptions loadBoardOptions = options.Value;
    private readonly TruckstopOptions options = options.Value.Truckstop ?? new TruckstopOptions();
    private string? accessToken;
    private string? refreshToken;
    private DateTime tokenExpiry = DateTime.MinValue;

    public LoadBoardProviderType ProviderType => LoadBoardProviderType.Truckstop;

    public void Initialize(LoadBoardConfiguration configuration)
    {
        accessToken = configuration.AccessToken;
        refreshToken = configuration.RefreshToken;
        tokenExpiry = configuration.TokenExpiresAt ?? DateTime.MinValue;

        httpClient.BaseAddress = new Uri(options.BaseUrl);

        if (!string.IsNullOrEmpty(accessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        logger.LogInformation("Initialized Truckstop Load Board provider");
    }

    public async Task<LoadBoardCredentialValidationResult> ValidateCredentialsAsync(string apiKey, string? apiSecret)
    {
        try
        {
            var tokenResult = await GetTokenAsync(apiKey, apiSecret);
            if (tokenResult?.AccessToken is null)
            {
                return LoadBoardCredentialValidationResult.Invalid();
            }

            return LoadBoardCredentialValidationResult.Valid(
                tokenResult.AccessToken,
                tokenResult.RefreshToken,
                DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogError(ex, "Error validating Truckstop credentials");
            return LoadBoardCredentialValidationResult.Invalid();
        }
    }

    public async Task<OAuthTokenResultDto?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            using var authClient = CreateAuthClient();
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken
            };

            var result = await authClient.TryPostFormAsync<TruckstopTokenResponse>(
                options.TokenUrl,
                tokenRequest,
                logger,
                "Truckstop refresh token",
                loadBoardOptions);

            if (result.Value == null)
            {
                logger.LogWarning("Truckstop token refresh failed: {StatusCode}", result.StatusCode);
                return null;
            }

            accessToken = result.Value.AccessToken;
            this.refreshToken = result.Value.RefreshToken;
            tokenExpiry = DateTime.UtcNow.AddSeconds(result.Value.ExpiresIn);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return new OAuthTokenResultDto
            {
                AccessToken = result.Value.AccessToken, RefreshToken = result.Value.RefreshToken, ExpiresAt = tokenExpiry
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogError(ex, "Error refreshing Truckstop token");
            return null;
        }
    }

    public async Task<IEnumerable<LoadBoardListingDto>> SearchLoadsAsync(LoadBoardSearchCriteria criteria)
    {
        logger.LogInformation("Searching Truckstop loads: Origin={Origin}, Dest={Dest}",
            criteria.OriginAddress?.City, criteria.DestinationAddress?.City);

        await EnsureValidTokenAsync();

        var searchRequest = new
        {
            origin = new
            {
                city = criteria.OriginAddress?.City,
                stateProvince = criteria.OriginAddress?.State,
                deadheadMiles = criteria.OriginRadius
            },
            destination = criteria.DestinationAddress != null
                ? new
                {
                    city = criteria.DestinationAddress.City,
                    stateProvince = criteria.DestinationAddress.State,
                    deadheadMiles = criteria.DestinationRadius
                }
                : null,
            pickupDate = criteria.PickupDateStart?.ToString("yyyy-MM-dd"),
            equipmentTypes = criteria.EquipmentTypes,
            pageSize = criteria.MaxResults
        };

        var result = await httpClient.TryPostAsJsonAsync<object, TruckstopSearchResponse>(
            "/loadmanagement-v2/load/search", searchRequest, logger, "Truckstop search loads", loadBoardOptions);

        return result.Value?.Loads?.Select(TruckstopMapper.ToListingDto) ?? [];
    }

    public async Task<LoadBoardListingDto?> GetLoadDetailsAsync(string externalListingId)
    {
        await EnsureValidTokenAsync();
        var load = await httpClient.TryGetFromJsonAsync<TruckstopLoad>(
            $"/loadmanagement-v2/load/{externalListingId}",
            logger,
            $"Truckstop get load {externalListingId}",
            loadBoardOptions);

        return load != null ? TruckstopMapper.ToListingDto(load) : null;
    }

    public async Task<LoadBoardBookingResultDto> BookLoadAsync(string externalListingId,
        LoadBoardBookingRequest request)
    {
        logger.LogInformation("Booking Truckstop load {ListingId} for truck {TruckId}",
            externalListingId, request.TruckId);

        await EnsureValidTokenAsync();
        var bookRequest = new { loadId = externalListingId, notes = request.Notes };

        var result = await httpClient.TryPostAsJsonAsync<object, TruckstopBookingResponse>(
            $"/loadmanagement-v2/load/{externalListingId}/contact", bookRequest, logger,
            $"Truckstop book load {externalListingId}", loadBoardOptions);

        return result.IsSuccess
            ? new LoadBoardBookingResultDto { Success = true, ExternalConfirmationId = result.Value?.ConfirmationNumber }
            : new LoadBoardBookingResultDto { Success = false, ErrorMessage = $"Truckstop booking failed: {result.ErrorBody}" };
    }

    public async Task<bool> CancelBookingAsync(string externalListingId, string? reason)
    {
        await EnsureValidTokenAsync();
        return await httpClient.TryPostAsync(
            $"/loadmanagement-v2/load/{externalListingId}/cancel", new { reason }, logger,
            $"Truckstop cancel booking {externalListingId}", loadBoardOptions);
    }

    public async Task<PostTruckResultDto> PostTruckAsync(PostTruckRequest request)
    {
        logger.LogInformation("Posting truck {TruckId} to Truckstop", request.TruckId);

        await EnsureValidTokenAsync();

        var postRequest = new
        {
            origin = new
            {
                city = request.AvailableAtAddress.City,
                stateProvince = request.AvailableAtAddress.State,
                postalCode = request.AvailableAtAddress.ZipCode,
                latitude = request.AvailableAtLocation.Latitude,
                longitude = request.AvailableAtLocation.Longitude
            },
            destination = request.DestinationPreference != null
                ? new
                {
                    city = request.DestinationPreference.City,
                    stateProvince = request.DestinationPreference.State,
                    deadheadMiles = request.DestinationRadius
                }
                : null,
            availableDate = request.AvailableFrom.ToString("yyyy-MM-dd"),
            availableDateEnd = request.AvailableTo?.ToString("yyyy-MM-dd"),
            equipmentType = request.EquipmentType,
            weight = request.MaxWeight,
            length = request.MaxLength
        };

        var result = await httpClient.TryPostAsJsonAsync<object, TruckstopPostTruckResponse>(
            "/truckposting-v2/truck", postRequest, logger, $"Truckstop post truck {request.TruckId}", loadBoardOptions);

        return result.IsSuccess
            ? new PostTruckResultDto
            {
                Success = true, ExternalPostId = result.Value?.TruckId, ExpiresAt = result.Value?.ExpiresAt
            }
            : new PostTruckResultDto { Success = false, ErrorMessage = $"Truckstop post truck failed: {result.ErrorBody}" };
    }

    public async Task<bool> UpdateTruckPostAsync(string externalPostId, PostTruckRequest request)
    {
        await EnsureValidTokenAsync();
        var updateRequest = new
        {
            availableDate = request.AvailableFrom.ToString("yyyy-MM-dd"),
            availableDateEnd = request.AvailableTo?.ToString("yyyy-MM-dd"),
            equipmentType = request.EquipmentType,
            weight = request.MaxWeight,
            length = request.MaxLength
        };

        return await httpClient.TryPutAsync(
            $"/truckposting-v2/truck/{externalPostId}", updateRequest, logger,
            $"Truckstop update truck post {externalPostId}", loadBoardOptions);
    }

    public async Task<bool> RemoveTruckPostAsync(string externalPostId)
    {
        await EnsureValidTokenAsync();
        return await httpClient.TryDeleteAsync(
            $"/truckposting-v2/truck/{externalPostId}",
            logger,
            $"Truckstop remove truck post {externalPostId}",
            loadBoardOptions);
    }

    public async Task<IEnumerable<PostedTruckDto>> GetPostedTrucksAsync()
    {
        await EnsureValidTokenAsync();
        var result = await httpClient.TryGetFromJsonAsync<TruckstopTrucksResponse>(
            "/truckposting-v2/truck", logger, "Truckstop get posted trucks", loadBoardOptions);

        return result?.Trucks?.Select(TruckstopMapper.ToPostedTruckDto) ?? [];
    }

    public Task<LoadBoardWebhookResultDto> ProcessWebhookAsync(string payload, string? signature,
        string? webhookSecret)
    {
        try
        {
            var webhook = JsonSerializer.Deserialize<TruckstopWebhookPayload>(payload);
            return Task.FromResult(new LoadBoardWebhookResultDto
            {
                IsValid = true,
                EventType = TruckstopMapper.MapWebhookEventType(webhook?.Event),
                ExternalListingId = webhook?.LoadId
            });
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error processing Truckstop webhook");
            return Task.FromResult(new LoadBoardWebhookResultDto
            {
                IsValid = false, EventType = LoadBoardWebhookEventType.Unknown, ErrorMessage = ex.Message
            });
        }
    }

    private async Task<TruckstopTokenResponse?> GetTokenAsync(string username, string? password)
    {
        using var authClient = CreateAuthClient();
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "password", ["username"] = username, ["password"] = password ?? string.Empty
        };

        var result = await authClient.TryPostFormAsync<TruckstopTokenResponse>(
            options.TokenUrl,
            tokenRequest,
            logger,
            "Truckstop get token",
            loadBoardOptions);

        return result.Value;
    }

    private HttpClient CreateAuthClient() =>
        new()
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, loadBoardOptions.RequestTimeoutSeconds))
        };

    private async Task EnsureValidTokenAsync()
    {
        if (DateTime.UtcNow < tokenExpiry.AddMinutes(-2))
        {
            return;
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var result = await RefreshTokenAsync(refreshToken);
            if (result != null)
            {
                return;
            }
        }

        logger.LogWarning("Truckstop token expired and refresh failed");
    }
}
