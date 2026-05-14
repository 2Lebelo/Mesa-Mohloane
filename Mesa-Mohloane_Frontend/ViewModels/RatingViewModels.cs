using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class CitizenRatingItemViewModel
{
    public IncidentListItemDto Incident { get; set; } = default!;

    public AssignmentDto Assignment { get; set; } = default!;

    public ContractorRatingDto? ExistingRating { get; set; }

    public bool CanRate =>
        Incident.Status == 7 &&
        ExistingRating is null &&
        Assignment.Status == 6;
}

public sealed class CitizenRatingsViewModel
{
    public IReadOnlyList<CitizenRatingItemViewModel> Items { get; set; }
        = Array.Empty<CitizenRatingItemViewModel>();
}

public sealed class ContractorRatingsViewModel
{
    public PagedResultDto<ContractorRatingDto>? Ratings { get; set; }

    public decimal AverageStars =>
        Ratings?.Items?.Any() == true
            ? (decimal)Ratings.Items.Average(x => x.Stars)
            : 0;

    public int TotalRatings => Ratings?.TotalCount ?? 0;
}