namespace Mesa_Mohloane_Backend.Models.Entities;

public class IncidentPhoto : BaseEntity
{
    public Guid IncidentId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? PublicId { get; set; }
    public string? Caption { get; set; }

    public Incident? Incident { get; set; }
}
