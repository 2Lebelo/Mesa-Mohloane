namespace Mesa_Mohloane_Backend.Models.Entities;

public class AuditLog : BaseEntity
{
    public Guid ActorUserId { get; set; }
    public AuditActionType ActionType { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ActionAt { get; set; }
    public string? Notes { get; set; }

    public User? ActorUser { get; set; }
}
