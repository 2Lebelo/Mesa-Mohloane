namespace Mesa_Mohloane_Frontend.Dtos;

public sealed class AuditLogDto
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    // Backend returns this as a number, e.g. "actionType": 6
    public int ActionType { get; set; }

    public string ActionTypeLabel => ActionType switch
    {
        1 => "Created",
        2 => "Updated",
        3 => "Deleted",
        4 => "Submitted",
        5 => "Validated",
        6 => "Login Successful",
        7 => "Login Failed",
        8 => "Approved",
        9 => "Rejected",
        10 => "Assigned",
        11 => "Started",
        12 => "Completed",
        13 => "Work Completed",
        14 => "Payment Initiated",
        15 => "Payment Approved",
        16 => "Payment Disbursed",
        17 => "Payment Failed",
        _ => $"Action #{ActionType}"
    };

    public string EntityName { get; set; } = string.Empty;

    // Backend stores this as string, even when the value is a GUID.
    public string? EntityId { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public string? IpAddress { get; set; }

    public DateTime ActionAt { get; set; }

    public string? Notes { get; set; }
}

public sealed class AuditLogPagedResultDto
{
    public List<AuditLogDto> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}