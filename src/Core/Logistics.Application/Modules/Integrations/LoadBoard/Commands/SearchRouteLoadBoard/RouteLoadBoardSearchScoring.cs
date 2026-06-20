using Logistics.Domain.Primitives.Enums;
using Logistics.Domain.Primitives.ValueObjects;
using Logistics.Shared.Models;

namespace Logistics.Application.Modules.Integrations.LoadBoard.Commands;

internal static class RouteLoadBoardSearchScoring
{
    private const double MetersPerMile = 1609.344;

    public static IReadOnlyList<RouteLoadBoardSample> BuildSamples(GeoPoint origin, GeoPoint destination)
    {
        var routeMiles = ToMiles(origin.DistanceTo(destination));
        var segments = Math.Clamp((int)Math.Ceiling(routeMiles / 75d), 1, 16);
        var samples = new List<RouteLoadBoardSample>(segments + 1);

        for (var i = 0; i <= segments; i++)
        {
            var progress = (double)i / segments;
            samples.Add(new RouteLoadBoardSample(
                new GeoPoint(
                    Lerp(origin.Longitude, destination.Longitude, progress),
                    Lerp(origin.Latitude, destination.Latitude, progress)),
                progress * 100));
        }

        return samples;
    }

    public static List<RouteLoadBoardListingDto> RankListings(
        IEnumerable<LoadBoardListingDto> listings,
        IReadOnlyList<RouteLoadBoardSample> samples,
        DistanceUnit distanceUnit,
        string[]? requestedEquipmentTypes,
        int maxResults)
    {
        var requestedEquipment = requestedEquipmentTypes?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return listings
            .GroupBy(l => new ListingKey(l.ProviderType, l.ExternalListingId))
            .Select(g => g
                .OrderByDescending(l => l.RatePerMile ?? 0)
                .ThenByDescending(l => l.TotalRate ?? 0)
                .First())
            .Select(l => CreateRouteListing(l, samples, distanceUnit, requestedEquipment))
            .OrderByDescending(l => l.FitScore)
            .ThenBy(l => l.DistanceFromRouteMiles)
            .ThenByDescending(l => l.Listing.RatePerMile ?? 0)
            .ThenByDescending(l => l.Listing.TotalRate ?? 0)
            .ThenBy(l => l.Listing.PickupDateStart ?? DateTime.MaxValue)
            .Take(maxResults)
            .ToList();
    }

    public static double ConvertDistanceFromMiles(double miles, DistanceUnit distanceUnit) =>
        distanceUnit == DistanceUnit.Kilometers ? miles * 1.609344 : miles;

    public static double ConvertDistanceToMiles(double distance, DistanceUnit distanceUnit) =>
        distanceUnit == DistanceUnit.Kilometers ? distance * 0.621371 : distance;

    public static double ToMiles(double meters) => meters / MetersPerMile;

    private static RouteLoadBoardListingDto CreateRouteListing(
        LoadBoardListingDto listing,
        IReadOnlyList<RouteLoadBoardSample> samples,
        DistanceUnit distanceUnit,
        ISet<string>? requestedEquipmentTypes)
    {
        var nearest = FindNearestSample(listing.OriginLocation, samples);
        var distanceMiles = ToMiles(listing.OriginLocation.DistanceTo(nearest.Location));

        return new RouteLoadBoardListingDto
        {
            Listing = listing,
            DistanceFromRouteMiles = distanceMiles,
            DistanceFromRoute = ConvertDistanceFromMiles(distanceMiles, distanceUnit),
            RouteProgressPercent = nearest.ProgressPercent,
            FitScore = CalculateFitScore(listing, distanceMiles, requestedEquipmentTypes)
        };
    }

    private static int CalculateFitScore(
        LoadBoardListingDto listing,
        double distanceFromRouteMiles,
        ISet<string>? requestedEquipmentTypes)
    {
        var distanceScore = Math.Max(0, 45 - (distanceFromRouteMiles * 1.5));
        var rateScore = Math.Min(25, (double)(listing.RatePerMile ?? 0) * 7);
        var totalRateScore = Math.Min(10, (double)(listing.TotalRate ?? 0) / 500);
        var pickupScore = CalculatePickupScore(listing.PickupDateStart);
        var equipmentScore = CalculateEquipmentScore(listing.EquipmentType, requestedEquipmentTypes);

        return (int)Math.Round(distanceScore + rateScore + totalRateScore + pickupScore + equipmentScore);
    }

    private static int CalculatePickupScore(DateTime? pickupDate)
    {
        if (pickupDate is null)
        {
            return 3;
        }

        var daysUntilPickup = (pickupDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
        return daysUntilPickup switch
        {
            < 0 => 0,
            <= 2 => 10,
            <= 5 => 7,
            <= 10 => 4,
            _ => 2
        };
    }

    private static int CalculateEquipmentScore(string? equipmentType, ISet<string>? requestedEquipmentTypes)
    {
        if (requestedEquipmentTypes is null || requestedEquipmentTypes.Count == 0)
        {
            return 5;
        }

        return !string.IsNullOrWhiteSpace(equipmentType) && requestedEquipmentTypes.Contains(equipmentType)
            ? 10
            : 0;
    }

    private static RouteLoadBoardSample FindNearestSample(
        GeoPoint point,
        IReadOnlyList<RouteLoadBoardSample> samples) =>
        samples
            .OrderBy(s => point.DistanceTo(s.Location))
            .First();

    private static double Lerp(double start, double end, double progress) =>
        start + ((end - start) * progress);

    private readonly record struct ListingKey(LoadBoardProviderType ProviderType, string ExternalListingId);
}

internal readonly record struct RouteLoadBoardSample(GeoPoint Location, double ProgressPercent);
