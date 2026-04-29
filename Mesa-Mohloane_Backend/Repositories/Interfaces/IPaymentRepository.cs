using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByInvoiceAsync(Guid invoiceId);
    Task<Guid> CreateAsync(Payment payment);
    Task UpdateAsync(Payment payment);
}