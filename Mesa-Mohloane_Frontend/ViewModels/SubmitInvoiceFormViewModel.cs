using System.ComponentModel.DataAnnotations;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class SubmitInvoiceFormViewModel
{
    [Required]
    public Guid AssignmentId { get; set; }

    [Required]
    public Guid TenderApplicationId { get; set; }

    public Guid ContractorId { get; set; }

    public string? InvoiceNumber { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal OriginalQuotedAmount { get; set; }

    public decimal FinalInvoiceAmount { get; set; }

    public List<InvoiceLineItemFormViewModel> LineItems { get; set; } = new();
}

public sealed class InvoiceLineItemFormViewModel
{
    [Range(1, 5)]
    public int Category { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal LineTotal { get; set; }

    public InvoiceLineItemCreateDto ToDto()
        => new(
            Category,
            Description.Trim(),
            Quantity,
            UnitOfMeasure.Trim(),
            UnitPrice,
            LineTotal);
}