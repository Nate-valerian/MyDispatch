using FluentValidation;
using Logistics.Application.Abstractions;
using Logistics.Application.Attributes;
using Logistics.Domain.Primitives.Enums;
using Logistics.Shared.Models;

namespace Logistics.Application.Modules.Integrations.LoadBoard.Commands;

[RequiresFeature(TenantFeature.LoadBoard)]
public class SearchRouteLoadBoardCommand : ICommand<Result<RouteLoadBoardSearchResultDto>>
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int Radius { get; set; } = 50;
    public DistanceUnit DistanceUnit { get; set; } = DistanceUnit.Miles;
    public DateTime? PickupDateStart { get; set; }
    public DateTime? PickupDateEnd { get; set; }
    public string[]? EquipmentTypes { get; set; }
    public decimal? MinRatePerMile { get; set; }
    public decimal? MinTotalRate { get; set; }
    public int? MinWeight { get; set; }
    public int? MaxWeight { get; set; }
    public int? MaxLength { get; set; }
    public LoadBoardProviderType[]? Providers { get; set; }
    public int MaxResults { get; set; } = 25;
}

internal sealed class SearchRouteLoadBoardCommandValidator : AbstractValidator<SearchRouteLoadBoardCommand>
{
    public SearchRouteLoadBoardCommandValidator()
    {
        RuleFor(x => x.Origin)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Destination)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Radius)
            .InclusiveBetween(1, 250);

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 100);
    }
}
