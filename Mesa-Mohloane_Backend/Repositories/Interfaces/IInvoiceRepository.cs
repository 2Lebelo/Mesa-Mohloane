using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Invoice?> GetByAssignmentAsync(Guid assignmentId);
    Task<IEnumerable<Invoice>> GetByContractorAsync(Guid contractorId, int page, int pageSize);
    Task<int> GetCountByContractorAsync(Guid contractorId);
    Task<IEnumerable<Invoice>> GetFlaggedAsync(int page, int pageSize);
    Task<Guid> CreateAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task<string> GenerateInvoiceNumberAsync();
}