using Logistics.Application.Abstractions;
using Logistics.Domain.Entities;
using Logistics.Domain.Persistence;
using Logistics.Domain.Primitives.Enums;
using Logistics.Domain.Primitives.ValueObjects;
using Logistics.Shared.Models;
using Microsoft.Extensions.Logging;
using Logistics.Application.Abstractions.LoadBoard;

namespace Logistics.Application.Modules.Integrations.LoadBoard.Commands;

internal sealed class SearchLoadBoardHandler(
    ITenantUnitOfWork tenantUow,
    ILoadBoardProviderFactory providerFactory,
    ILoadBoardCredentialProtector credentialProtector,
    ILogger<SearchLoadBoardHandler> logger)
    : IAppRequestHandler<SearchLoadBoardCommand, Result<LoadBoardSearchResultDto>>
{
    public async Task<Result<LoadBoardSearchResultDto>> Handle(SearchLoadBoardCommand req, CancellationToken ct)
    {
        // Get active provider configurations
        var configs = await tenantUow.Repository<LoadBoardConfiguration>()
            .GetListAsync(c => c.IsActive, ct);

        if (!configs.Any())
        {
            return Result<LoadBoardSearchResultDto>.Fail("No load board providers configured. Please add a provider first.");
        }

        // Filter by requested providers if specified
        if (req.Providers is { Length: > 0 })
        {
            configs = configs.Where(c => req.Providers.Contains(c.ProviderType)).ToList();
        }

        var allListings = new List<LoadBoardListingDto>();
        var countByProvider = new Dictionary<LoadBoardProviderType, int>();
        var errors = new Dictionary<LoadBoardProviderType, string?>();

        var criteria = new LoadBoardSearchCriteria
        {
            OriginAddress = req.OriginAddress,
            OriginRadius = req.OriginRadius,
            DestinationAddress = req.DestinationAddress,
            DestinationRadius = req.DestinationRadius,
            PickupDateStart = req.PickupDateStart,
            PickupDateEnd = req.PickupDateEnd,
            EquipmentTypes = req.EquipmentTypes,
            MinRatePerMile = req.MinRatePerMile,
            MinTotalRate = req.MinTotalRate,
            MinWeight = req.MinWeight,
            MaxWeight = req.MaxWeight,
            MaxLength = req.MaxLength,
            MaxResults = req.MaxResults
        };

        // Search each provider
        foreach (var config in configs)
        {
            try
            {
                var credentialError = await LoadBoardProviderCredentials.RefreshIfNeededAsync(
                    config,
                    providerFactory,
                    credentialProtector,
                    logger);

                if (credentialError is not null)
                {
                    errors[config.ProviderType] = credentialError;
                    countByProvider[config.ProviderType] = 0;
                    continue;
                }

                var provider = providerFactory.GetProvider(config);
                var listings = await provider.SearchLoadsAsync(criteria);
                var listingsList = new List<LoadBoardListingDto>();

                foreach (var listing in listings)
                {
                    var persistedListing = await UpsertListingAsync(config.ProviderType, listing, ct);
                    listingsList.Add(ToDto(persistedListing));
                }

                allListings.AddRange(listingsList);
                countByProvider[config.ProviderType] = listingsList.Count;
                config.LastSyncedAt = DateTime.UtcNow;

                logger.LogDebug("Found {Count} listings from {Provider}", listingsList.Count, config.ProviderType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching load board provider {Provider}", config.ProviderType);
                errors[config.ProviderType] = ex.Message;
                countByProvider[config.ProviderType] = 0;
            }
        }

        // Sort by rate per mile descending (best rates first)
        var sortedListings = allListings
            .OrderByDescending(l => l.RatePerMile ?? 0)
            .Take(req.MaxResults)
            .ToList();

        var result = new LoadBoardSearchResultDto
        {
            Listings = sortedListings,
            TotalCount = sortedListings.Count,
            CountByProvider = countByProvider,
            Errors = errors.Count > 0 ? errors : null
        };

        await tenantUow.SaveChangesAsync(ct);

        return Result<LoadBoardSearchResultDto>.Ok(result);
    }

    private async Task<LoadBoardListing> UpsertListingAsync(
        LoadBoardProviderType providerType,
        LoadBoardListingDto dto,
        CancellationToken ct)
    {
        var listing = await tenantUow.Repository<LoadBoardListing>()
            .GetAsync(l => l.ProviderType == providerType && l.ExternalListingId == dto.ExternalListingId, ct);

        if (listing is null)
        {
            listing = new LoadBoardListing
            {
                ProviderType = providerType,
                ExternalListingId = dto.ExternalListingId,
                OriginAddress = dto.OriginAddress,
                OriginLocation = dto.OriginLocation,
                DestinationAddress = dto.DestinationAddress,
                DestinationLocation = dto.DestinationLocation,
                ExpiresAt = dto.ExpiresAt
            };

            await tenantUow.Repository<LoadBoardListing>().AddAsync(listing, ct);
        }

        listing.OriginAddress = dto.OriginAddress;
        listing.OriginLocation = dto.OriginLocation;
        listing.DestinationAddress = dto.DestinationAddress;
        listing.DestinationLocation = dto.DestinationLocation;
        listing.RatePerMile = dto.RatePerMile;
        listing.TotalRate = dto.TotalRate.HasValue
            ? new Money { Amount = dto.TotalRate.Value, Currency = dto.Currency ?? "USD" }
            : null;
        listing.Distance = dto.Distance;
        listing.Weight = dto.Weight;
        listing.Length = dto.Length;
        listing.PickupDateStart = dto.PickupDateStart;
        listing.PickupDateEnd = dto.PickupDateEnd;
        listing.DeliveryDateStart = dto.DeliveryDateStart;
        listing.DeliveryDateEnd = dto.DeliveryDateEnd;
        listing.EquipmentType = dto.EquipmentType;
        listing.Commodity = dto.Commodity;
        listing.BrokerName = dto.BrokerName;
        listing.BrokerPhone = dto.BrokerPhone;
        listing.BrokerEmail = dto.BrokerEmail;
        listing.BrokerMcNumber = dto.BrokerMcNumber;
        listing.ExpiresAt = dto.ExpiresAt;

        if (listing.Status != LoadBoardListingStatus.Booked)
        {
            listing.Status = dto.Status;
            listing.BookedAt = dto.BookedAt;
        }

        return listing;
    }

    private static LoadBoardListingDto ToDto(LoadBoardListing listing) => new()
    {
        Id = listing.Id,
        ExternalListingId = listing.ExternalListingId,
        ProviderType = listing.ProviderType,
        ProviderName = listing.ProviderType.ToString(),
        OriginAddress = listing.OriginAddress,
        OriginLocation = listing.OriginLocation,
        DestinationAddress = listing.DestinationAddress,
        DestinationLocation = listing.DestinationLocation,
        RatePerMile = listing.RatePerMile,
        TotalRate = listing.TotalRate?.Amount,
        Currency = listing.TotalRate?.Currency,
        Distance = listing.Distance,
        Weight = listing.Weight,
        Length = listing.Length,
        PickupDateStart = listing.PickupDateStart,
        PickupDateEnd = listing.PickupDateEnd,
        DeliveryDateStart = listing.DeliveryDateStart,
        DeliveryDateEnd = listing.DeliveryDateEnd,
        EquipmentType = listing.EquipmentType,
        Commodity = listing.Commodity,
        BrokerName = listing.BrokerName,
        BrokerPhone = listing.BrokerPhone,
        BrokerEmail = listing.BrokerEmail,
        BrokerMcNumber = listing.BrokerMcNumber,
        Status = listing.Status,
        BookedAt = listing.BookedAt,
        ExpiresAt = listing.ExpiresAt,
        LoadId = listing.LoadId,
        Notes = listing.Notes
    };
}
