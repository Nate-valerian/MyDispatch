using Logistics.Application.Modules.Integrations.LoadBoard.Commands;
using Logistics.Domain.Primitives.Enums;
using Logistics.Domain.Primitives.ValueObjects;
using Logistics.Shared.Models;
using Xunit;

namespace Logistics.Application.Tests.LoadBoard;

public class RouteLoadBoardSearchScoringTests
{
    [Fact]
    public void BuildSamples_LongRoute_IncludesEndpointsAndIntermediatePoints()
    {
        var origin = new GeoPoint(-96.7970, 32.7767);
        var destination = new GeoPoint(-84.3880, 33.7490);

        var samples = RouteLoadBoardSearchScoring.BuildSamples(origin, destination);

        Assert.True(samples.Count > 2);
        Assert.Equal(origin.Longitude, samples[0].Location.Longitude, 6);
        Assert.Equal(origin.Latitude, samples[0].Location.Latitude, 6);
        Assert.Equal(destination.Longitude, samples[^1].Location.Longitude, 6);
        Assert.Equal(destination.Latitude, samples[^1].Location.Latitude, 6);
    }

    [Fact]
    public void RankListings_PrefersNearerCorridorListingWhenRatesAreComparable()
    {
        var origin = new GeoPoint(-96.7970, 32.7767);
        var destination = new GeoPoint(-84.3880, 33.7490);
        var samples = RouteLoadBoardSearchScoring.BuildSamples(origin, destination);
        var nearListing = BuildListing("near", samples[samples.Count / 2].Location, 2.85m, 2400m);
        var farListing = BuildListing("far", new GeoPoint(-118.2437, 34.0522), 2.95m, 2500m);

        var ranked = RouteLoadBoardSearchScoring.RankListings(
            [farListing, nearListing],
            samples,
            DistanceUnit.Miles,
            null,
            10);

        Assert.Equal("near", ranked[0].Listing.ExternalListingId);
        Assert.True(ranked[0].FitScore > ranked[1].FitScore);
    }

    private static LoadBoardListingDto BuildListing(
        string externalId,
        GeoPoint originLocation,
        decimal ratePerMile,
        decimal totalRate) => new()
    {
        ExternalListingId = externalId,
        ProviderType = LoadBoardProviderType.Demo,
        ProviderName = "Demo",
        OriginAddress = BuildAddress("Memphis", "TN"),
        OriginLocation = originLocation,
        DestinationAddress = BuildAddress("Atlanta", "GA"),
        DestinationLocation = new GeoPoint(-84.3880, 33.7490),
        RatePerMile = ratePerMile,
        TotalRate = totalRate,
        Currency = "USD",
        Distance = 500,
        EquipmentType = "Dry Van",
        Status = LoadBoardListingStatus.Available,
        PickupDateStart = DateTime.UtcNow.AddDays(1),
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    private static Address BuildAddress(string city, string state) => new()
    {
        Line1 = "1 Test St",
        City = city,
        State = state,
        ZipCode = "00000",
        Country = "US"
    };
}
