namespace Mesa_Mohloane_Backend.Models.Entities;

public class TenderApplication : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Guid ContractorId { get; set; }
    public string ProposalText { get; set; } = string.Empty;
    public int EstimatedTimelineDays { get; set; }
    public decimal QuotedTotalAmount { get; set; }
    public TenderStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public decimal WeightedScore { get; set; }
    public decimal CostScore { get; set; }
    public decimal RatingScore { get; set; }
    public decimal PerformanceScore { get; set; }
    public int RankPosition { get; set; }
    public string? EvaluationNotes { get; set; }

    public Incident? Incident { get; set; }
    public User? Contractor { get; set; }
    public User? ReviewedByAdmin { get; set; }
    public ICollection<TenderLineItem> LineItems { get; set; } = new List<TenderLineItem>();
    public Assignment? Assignment { get; set; }
    public Invoice? Invoice { get; set; }
}
