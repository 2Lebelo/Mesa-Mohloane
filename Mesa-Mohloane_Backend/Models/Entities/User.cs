namespace Mesa_Mohloane_Backend.Models.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public ContractorProfile? ContractorProfile { get; set; }
    public ICollection<Incident> IncidentsReported { get; set; } = new List<Incident>();
    public ICollection<Incident> IncidentsVerified { get; set; } = new List<Incident>();
    public ICollection<TenderApplication> TenderApplications { get; set; } = new List<TenderApplication>();
    public ICollection<Assignment> AssignmentsAssigned { get; set; } = new List<Assignment>();
    public ICollection<Assignment> AssignmentsAsContractor { get; set; } = new List<Assignment>();
    public ICollection<WorkCompletion> WorkCompletionsReviewed { get; set; } = new List<WorkCompletion>();
    public ICollection<Invoice> InvoicesApproved { get; set; } = new List<Invoice>();
    public ICollection<Payment> PaymentsApproved { get; set; } = new List<Payment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ContractorRating> RatingsGiven { get; set; } = new List<ContractorRating>();
}
