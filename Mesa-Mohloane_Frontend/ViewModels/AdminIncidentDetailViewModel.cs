using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class AdminIncidentDetailViewModel
{
    public IncidentDetailDto Incident { get; set; } = default!;
    public IReadOnlyList<TenderApplicationDto> Tenders { get; set; } = Array.Empty<TenderApplicationDto>();
}
