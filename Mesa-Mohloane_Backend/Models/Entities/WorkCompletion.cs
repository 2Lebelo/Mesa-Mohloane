namespace Mesa_Mohloane_Backend.Models.Entities;

public class WorkCompletion : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public string CompletionSummary { get; set; } = string.Empty;
    public string? CompletionEvidenceUrl { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }

    public Assignment? Assignment { get; set; }
    public User? ReviewedByAdmin { get; set; }
}
