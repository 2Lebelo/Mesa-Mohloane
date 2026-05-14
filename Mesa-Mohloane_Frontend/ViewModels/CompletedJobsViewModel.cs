using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class CompletedJobsViewModel
{
    public IReadOnlyList<CompletedJobItem> Items { get; set; } = Array.Empty<CompletedJobItem>();
}

public sealed class CompletedJobItem
{
    public IncidentListItemDto Incident { get; set; } = default!;
    public AssignmentDto? Assignment { get; set; }
    public ContractorRatingDto? Rating { get; set; }
}
