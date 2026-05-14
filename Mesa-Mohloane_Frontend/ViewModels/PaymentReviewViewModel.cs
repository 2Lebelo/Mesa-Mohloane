using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.ViewModels;

public sealed class PaymentReviewViewModel
{
    public Guid? InvoiceId { get; set; }

    public InvoiceDto? Invoice { get; set; }

    public PaymentDto? Payment { get; set; }

    public bool HasInvoice => Invoice is not null;

    public bool HasPayment => Payment is not null;

    public bool CanInitiatePayment =>
        Invoice is not null &&
        Payment is null &&
        Invoice.Status == 3 &&
        Invoice.CitizenAcknowledgedAt.HasValue;

    public decimal Amount => Invoice?.FinalInvoiceAmount ?? Payment?.Amount ?? 0;

    public string SuggestedPaymentReference =>
        Invoice is null
            ? string.Empty
            : $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}";
}