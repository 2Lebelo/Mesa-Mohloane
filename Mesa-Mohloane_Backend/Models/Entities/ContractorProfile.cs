namespace Mesa_Mohloane_Backend.Models.Entities;

public class ContractorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string CoverageArea { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int CompletedJobsCount { get; set; }
    public int LateCompletionCount { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public User? User { get; set; }
    public User? ApprovedByAdmin { get; set; }
}
