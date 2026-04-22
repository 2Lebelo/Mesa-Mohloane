namespace Mesa_Mohloane_Backend.Models.Entities;

public class ContractorRating : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid CitizenId { get; set; }
    public Guid ContractorId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime RatedAt { get; set; }

    public Incident? Incident { get; set; }
    public Assignment? Assignment { get; set; }
    public User? Citizen { get; set; }
    public User? Contractor { get; set; }
}
