namespace Mesa_Mohloane_Backend.Models.Entities;

public class Invoice : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public Guid TenderApplicationId { get; set; }
    public Guid ContractorId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal OriginalQuotedAmount { get; set; }
    public decimal FinalInvoiceAmount { get; set; }
    public decimal VariancePercentage { get; set; }
    public bool IsVarianceFlagged { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public DateTime? CitizenAcknowledgedAt { get; set; }
    public string? ValidationRemarks { get; set; }

    public Assignment? Assignment { get; set; }
    public TenderApplication? TenderApplication { get; set; }
    public User? Contractor { get; set; }
    public User? ApprovedByAdmin { get; set; }
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public Payment? Payment { get; set; }
}
