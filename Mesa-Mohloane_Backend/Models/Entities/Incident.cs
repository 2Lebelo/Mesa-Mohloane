namespace Mesa_Mohloane_Backend.Models.Entities;

public class Incident : BaseEntity
{
    public Guid CitizenId { get; set; }
    public Guid? VerifiedByAdminId { get; set; }
    public string IncidentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public IncidentStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public User? Citizen { get; set; }
    public User? VerifiedByAdmin { get; set; }
    public ICollection<IncidentPhoto> Photos { get; set; } = new List<IncidentPhoto>();
    public ICollection<TenderApplication> TenderApplications { get; set; } = new List<TenderApplication>();
    public Assignment? Assignment { get; set; }
    public ICollection<ContractorRating> Ratings { get; set; } = new List<ContractorRating>();
}
