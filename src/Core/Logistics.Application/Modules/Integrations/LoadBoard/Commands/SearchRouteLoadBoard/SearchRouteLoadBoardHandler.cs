using Logistics.Application.Abstractions;
using Logistics.Application.Abstractions.Geocoding;
using Logistics.Application.Abstractions.LoadBoard;
using Logistics.Domain.Entities;
using Logistics.Domain.Persistence;
using Logistics.Domain.Primitives.Enums;
using Logistics.Domain.Primitives.ValueObjects;
using Logistics.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Logistics.Application.Modules.Integrations.LoadBoard.Commands;

internal sealed class SearchRouteLoadBoardHandler(
    ITenantUnitOfWork tenantUow,
    ILoadBoardProviderFactory providerFactory,
    ILoadBoardCredentialProtector credentialProtector,
    IGeocodingService geocodingService,
    ILogger<SearchRouteLoadBoardHandler> logger)
    : IAppRequestHandler<SearchRouteLoadBoardCommand, Result<RouteLoadBoardSearchResultDto>>
{
    private static readonly IReadOnlyDictionary<string, GeoPoint> KnownLocations =
        new Dictionary<string, GeoPoint>(StringComparer.OrdinalIgnoreCase)
        {
            ["atlanta"] = new(-84.3880, 33.7490),
            ["atlanta ga"] = new(-84.3880, 33.7490),
            ["chicago"] = new(-87.6298, 41.8781),
            ["chicago il"] = new(-87.6298, 41.8781),
            ["dallas"] = new(-96.7970, 32.7767),
            ["dallas tx"] = new(-96.7970, 32.7767),
            ["denver"] = new(-104.9903, 39.7392),
            ["denver co"] = new(-104.9903, 39.7392),
            ["houston"] = new(-95.3698, 29.7604),
            ["houston tx"] = new(-95.3698, 29.7604),
            ["kansas city"] = new(-94.5786, 39.0997),
            ["kansas city mo"] = new(-94.5786, 39.0997),
            ["los angeles"] = new(-118.2437, 34.0522),
            ["los angeles ca"] = new(-118.2437, 34.0522),
            ["memphis"] = new(-90.0490, 35.1495),
            ["memphis tn"] = new(-90.0490, 35.1495),
            ["miami"] = new(-80.1918, 25.7617),
            ["miami fl"] = new(-80.1918, 25.7617),
            ["new york"] = new(-74.0060, 40.7128),
            ["new york ny"] = new(-74.0060, 40.7128),
            ["phoenix"] = new(-112.0740, 33.4484),
            ["phoenix az"] = new(-112.0740, 33.4484),
            ["seattle"] = new(-122.3321, 47.6062),
            ["seattle wa"] = new(-122.3321, 47.6062)
        };

    public async Task<Result<RouteLoadBoardSearchResultDto>> Handle(
        SearchRouteLoadBoardCommand req,
        CancellationToken ct)
    {
        var originResult = await GeocodeRouteEndpointAsync(req.Origin, "route start", ct);
        if (!originResult.IsSuccess || originResult.Value is null)
        {
            return Result<RouteLoadBoardSearchResultDto>.Fail(
                originResult.Error ?? $"Unable to geocode route start: {req.Origin}");
        }

        var destinationResult = await GeocodeRouteEndpointAsync(req.Destination, "route end", ct);
        if (!destinationResult.IsSuccess || destinationResult.Value is null)
        {
            return Result<RouteLoadBoardSearchResultDto>.Fail(
                destinationResult.Error ?? $"Unable to geocode route end: {req.Destination}");
        }

        var configs = await tenantUow.Repository<LoadBoardConfiguration>()
            .GetListAsync(c => c.IsActive, ct);

        if (!configs.Any())
        {
            return Result<RouteLoadBoardSearchResultDto>.Fail(
                "No load board providers configured. Please add a provider first.");
        }

        if (req.Providers is { Length: > 0 })
        {
            configs = configs.Where(c => req.Providers.Contains(c.ProviderType)).ToList();
        }

        var origin = originResult.Value;
        var destination = destinationResult.Value;
        var samples = RouteLoadBoardSearchScoring.BuildSamples(origin, destination);
        var radiusMiles = (int)Math.Ceiling(RouteLoadBoardSearchScoring.ConvertDistanceToMiles(
            req.Radius,
            req.DistanceUnit));
        var originAddress = BuildSearchAddress(req.Origin);
        var destinationAddress = BuildSearchAddress(req.Destination);
        var allListings = new List<LoadBoardListingDto>();
        var countByProvider = new Dictionary<LoadBoardProviderType, int>();
        var errors = new Dictionary<LoadBoardProviderType, string?>();
        var maxResultsPerSample = Math.Clamp((req.MaxResults / samples.Count) + 3, 5, 15);

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
                var providerListings = new Dictionary<string, LoadBoardListingDto>(StringComparer.OrdinalIgnoreCase);

                foreach (var sample in samples)
                {
                    var criteria = BuildCriteria(
                        req,
                        originAddress,
                        sample.Location,
                        destinationAddress,
                        destination,
                        radiusMiles,
                        maxResultsPerSample);

                    var listings = await provider.SearchLoadsAsync(criteria);
                    foreach (var listing in listings)
                    {
                        var persistedListing = await UpsertListingAsync(config.ProviderType, listing, ct);
                        var dto = ToDto(persistedListing);
                        providerListings[BuildListingKey(dto)] = dto;
                    }
                }

                allListings.AddRange(providerListings.Values);
                countByProvider[config.ProviderType] = providerListings.Count;
                config.LastSyncedAt = DateTime.UtcNow;

                logger.LogDebug(
                    "Found {Count} route-corridor listings from {Provider}",
                    providerListings.Count,
                    config.ProviderType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching route corridor for load board provider {Provider}", config.ProviderType);
                errors[config.ProviderType] = ex.Message;
                countByProvider[config.ProviderType] = 0;
            }
        }

        var rankedListings = RouteLoadBoardSearchScoring.RankListings(
            allListings,
            samples,
            req.DistanceUnit,
            req.EquipmentTypes,
            req.MaxResults);

        await tenantUow.SaveChangesAsync(ct);

        return Result<RouteLoadBoardSearchResultDto>.Ok(new RouteLoadBoardSearchResultDto
        {
            Origin = req.Origin,
            Destination = req.Destination,
            OriginLocation = origin,
            DestinationLocation = destination,
            Radius = req.Radius,
            DistanceUnit = req.DistanceUnit,
            EstimatedRouteDistance = RouteLoadBoardSearchScoring.ConvertDistanceFromMiles(
                RouteLoadBoardSearchScoring.ToMiles(origin.DistanceTo(destination)),
                req.DistanceUnit),
            Listings = rankedListings,
            TotalCount = rankedListings.Count,
            CountByProvider = countByProvider,
            Errors = errors.Count > 0 ? errors : null
        });
    }

    private static LoadBoardSearchCriteria BuildCriteria(
        SearchRouteLoadBoardCommand req,
        Address originAddress,
        GeoPoint originLocation,
        Address destinationAddress,
        GeoPoint destinationLocation,
        int radiusMiles,
        int maxResults) => new()
    {
        OriginAddress = originAddress,
        OriginLocation = originLocation,
        OriginRadius = radiusMiles,
        DestinationAddress = destinationAddress,
        DestinationLocation = destinationLocation,
        DestinationRadius = radiusMiles,
        PickupDateStart = req.PickupDateStart,
        PickupDateEnd = req.PickupDateEnd,
        EquipmentTypes = req.EquipmentTypes,
        MinRatePerMile = req.MinRatePerMile,
        MinTotalRate = req.MinTotalRate,
        MinWeight = req.MinWeight,
        MaxWeight = req.MaxWeight,
        MaxLength = req.MaxLength,
        MaxResults = maxResults
    };

    private static Address BuildSearchAddress(string locationText)
    {
        var parts = locationText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var city = parts.FirstOrDefault();
        var state = parts.Skip(1)
            .FirstOrDefault()?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return new Address
        {
            Line1 = string.Empty,
            City = string.IsNullOrWhiteSpace(city) ? locationText : city,
            State = string.IsNullOrWhiteSpace(state) ? string.Empty : state.ToUpperInvariant(),
            ZipCode = string.Empty,
            Country = "US"
        };
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

    private static string BuildListingKey(LoadBoardListingDto listing) =>
        $"{listing.ProviderType}:{listing.ExternalListingId}";

    private async Task<Result<GeoPoint>> GeocodeRouteEndpointAsync(
        string locationText,
        string label,
        CancellationToken ct)
    {
        var geocodeResult = await geocodingService.GeocodeLocationAsync(locationText, ct);
        if (geocodeResult.IsSuccess)
        {
            return geocodeResult;
        }

        if (TryResolveKnownLocation(locationText, out var knownLocation))
        {
            logger.LogWarning(
                "Using known fallback coordinate for {Label} '{Location}' because geocoding failed: {Error}",
                label,
                locationText,
                geocodeResult.Error);
            return Result<GeoPoint>.Ok(knownLocation);
        }

        return geocodeResult;
    }

    private static bool TryResolveKnownLocation(string locationText, out GeoPoint location)
    {
        var normalized = NormalizeLocation(locationText);
        return KnownLocations.TryGetValue(normalized, out location!);
    }

    private static string NormalizeLocation(string locationText) =>
        string.Join(
            ' ',
            locationText
                .Replace(",", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .ToLowerInvariant();
}
