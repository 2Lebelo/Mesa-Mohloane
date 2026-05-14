using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class SubmitInvoiceViewModel
{
    public Guid AssignmentId { get; set; }

    public AssignmentDto Assignment { get; set; } = default!;

    public TenderApplicationDto? Tender { get; set; }

    public decimal OriginalQuotedAmount { get; set; }

    public InvoiceDto? ExistingInvoice { get; set; }
}