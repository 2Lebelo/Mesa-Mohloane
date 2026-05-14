using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class CitizenDashboardViewModel
{
    public PagedResultDto<IncidentListItemDto>? MyIncidents { get; set; }
    public int CompletedJobsCount { get; set; }
    public PagedResultDto<NotificationDto>? Notifications { get; set; }
    public int TotalIncidents => MyIncidents?.TotalCount ?? 0;
    public int UnreadNotifications { get; set; }
}

public sealed class ContractorDashboardViewModel
{
    public PagedResultDto<IncidentListItemDto>? OpenIncidents { get; set; }
    public PagedResultDto<TenderApplicationListDto>? MyTenders { get; set; }
    public PagedResultDto<AssignmentDto>? MyAssignments { get; set; }
    public PagedResultDto<InvoiceListDto>? MyInvoices { get; set; }
    public PagedResultDto<NotificationDto>? Notifications { get; set; }
    public int UnreadNotifications { get; set; }
}

public sealed class InspectorDashboardViewModel
{
    public PagedResultDto<AuditLogDto>? RecentAuditLogs { get; set; }
    public PagedResultDto<InvoiceListDto>? FlaggedInvoices { get; set; }
    public PagedResultDto<NotificationDto>? Notifications { get; set; }
    public int UnreadNotifications { get; set; }
}

public sealed class SubmitTenderViewModel
{
    public Guid IncidentId { get; set; }
    public IncidentDetailDto? Incident { get; set; }
    public TenderApplicationCreateDto Form { get; set; } = new(
        Guid.Empty,
        Guid.Empty,
        string.Empty,
        0,
        0,
        Array.Empty<TenderLineItemCreateDto>());
}

public sealed class SubmitInvoiceViewModel
{
    public Guid AssignmentId { get; set; }
    public AssignmentDto? Assignment { get; set; }
    public InvoiceCreateDto Form { get; set; } = new(
        Guid.Empty,
        Guid.Empty,
        0,
        0,
        Array.Empty<InvoiceLineItemCreateDto>());
}

public sealed class InvoiceReviewViewModel
{
    public InvoiceDto? Invoice { get; set; }
    public PagedResultDto<AuditLogDto>? AuditLogs { get; set; }
}

public sealed class AuditLogListViewModel
{
    public PagedResultDto<AuditLogDto>? Logs { get; set; }
    public string? EntityName { get; set; }
    public string? ActionType { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
