using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IPaymentService
{
    Task<ServiceResult<PaymentDto>> InitiateAsync(Guid adminId, PaymentCreateDto dto);
    Task<ServiceResult<PaymentDto>> ApproveAsync(Guid paymentId, Guid adminId);
    Task<ServiceResult<PaymentDto>> DisburseAsync(Guid paymentId, Guid adminId);
    Task<ServiceResult<PaymentDto>> MarkFailedAsync(Guid paymentId, Guid adminId, string reason);
    Task<ServiceResult<PaymentDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PaymentDto>> GetByInvoiceAsync(Guid invoiceId);
}