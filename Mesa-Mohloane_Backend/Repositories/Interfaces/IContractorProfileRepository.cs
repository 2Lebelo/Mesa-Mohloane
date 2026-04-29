using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IContractorProfileRepository
{
    Task<ContractorProfile?> GetByIdAsync(Guid id);
    Task<ContractorProfile?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<ContractorProfile>> GetAllApprovedAsync(int page, int pageSize);
    Task<int> GetTotalApprovedCountAsync();
    Task<Guid> CreateAsync(ContractorProfile profile);
    Task UpdateAsync(ContractorProfile profile);
}