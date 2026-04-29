using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly MesaMohloaneDbContext _context;

    public PaymentRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<Payment?> GetByIdAsync(Guid id)
        => await _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.ApprovedByAdmin)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

    public async Task<Payment?> GetByInvoiceAsync(Guid invoiceId)
        => await _context.Payments
            .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId && !p.IsDeleted);

    public async Task<Guid> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment.Id;
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }
}