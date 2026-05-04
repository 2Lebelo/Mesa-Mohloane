using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class CitizenIncidentDetailViewModel
{
    public IncidentDetailDto Incident { get; set; } = default!;

    public AssignmentDto? Assignment { get; set; }

    public bool CanAcknowledge =>
        Incident.Status == 6 &&
        Assignment is not null &&
        Assignment.Status == 3 &&
        Assignment.CitizenAcknowledgedAt is null;

    public bool IsAwaitingAdminApproval =>
        Assignment is not null &&
        Assignment.Status == 4;

    public bool IsAdminApproved =>
        Assignment is not null &&
        Assignment.Status >= 5;
}