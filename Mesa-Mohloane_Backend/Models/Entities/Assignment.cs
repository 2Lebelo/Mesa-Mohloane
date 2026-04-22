namespace Mesa_Mohloane_Backend.Models.Entities;

public class Assignment : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Guid TenderApplicationId { get; set; }
    public Guid ContractorId { get; set; }
    public Guid AssignedByAdminId { get; set; }
    public DateTime AssignedAt { get; set; }
    public AssignmentStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CitizenAcknowledgedAt { get; set; }
    public DateTime? AdminApprovedAt { get; set; }

    public Incident? Incident { get; set; }
    public TenderApplication? TenderApplication { get; set; }
    public User? Contractor { get; set; }
    public User? AssignedByAdmin { get; set; }
    public WorkCompletion? WorkCompletion { get; set; }
    public Invoice? Invoice { get; set; }
    public ContractorRating? ContractorRating { get; set; }
}
