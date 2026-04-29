using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly MesaMohloaneDbContext _context;

    public InvoiceRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<Invoice?> GetByIdAsync(Guid id)
        => await _context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Assignment)
            .Include(i => i.TenderApplication)
            .Include(i => i.Contractor)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

    public async Task<Invoice?> GetByAssignmentAsync(Guid assignmentId)
        => await _context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Assignment)
            .FirstOrDefaultAsync(i => i.AssignmentId == assignmentId && !i.IsDeleted);

    public async Task<IEnumerable<Invoice>> GetByContractorAsync(
        Guid contractorId, int page, int pageSize)
        => await _context.Invoices
            .Include(i => i.LineItems)
            .Where(i => i.ContractorId == contractorId && !i.IsDeleted)
            .OrderByDescending(i => i.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByContractorAsync(Guid contractorId)
        => await _context.Invoices
            .CountAsync(i => i.ContractorId == contractorId && !i.IsDeleted);

    public async Task<IEnumerable<Invoice>> GetFlaggedAsync(int page, int pageSize)
        => await _context.Invoices
            .Include(i => i.Contractor)
            .Include(i => i.Assignment)
            .Where(i => i.IsVarianceFlagged && !i.IsDeleted)
            .OrderByDescending(i => i.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<Guid> CreateAsync(Invoice invoice)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice.Id;
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"INV-{today:yyyyMMdd}";
        var count = await _context.Invoices
            .CountAsync(i => i.InvoiceNumber.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D4}";
    }
}