namespace Mesa_Mohloane_Backend.Models.Entities;

public class Payment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public string? FailureReason { get; set; }

    public Invoice? Invoice { get; set; }
    public User? ApprovedByAdmin { get; set; }
}
