using Logistics.Domain.Primitives.Enums;
using Logistics.Domain.Primitives.ValueObjects;

namespace Logistics.Shared.Models;

public record RouteLoadBoardSearchResultDto
{
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public required GeoPoint OriginLocation { get; set; }
    public required GeoPoint DestinationLocation { get; set; }
    public int Radius { get; set; }
    public DistanceUnit DistanceUnit { get; set; }
    public double EstimatedRouteDistance { get; set; }
    public string RouteModel { get; set; } = "GeodesicCorridor";
    public IEnumerable<RouteLoadBoardListingDto> Listings { get; set; } = [];
    public int TotalCount { get; set; }
    public Dictionary<LoadBoardProviderType, int> CountByProvider { get; set; } = [];
    public Dictionary<LoadBoardProviderType, string?>? Errors { get; set; }
}

public record RouteLoadBoardListingDto
{
    public required LoadBoardListingDto Listing { get; set; }
    public double DistanceFromRoute { get; set; }
    public double DistanceFromRouteMiles { get; set; }
    public double RouteProgressPercent { get; set; }
    public int FitScore { get; set; }
}
