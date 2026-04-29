using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface ITenderApplicationRepository
{
    Task<TenderApplication?> GetByIdAsync(Guid id);
    Task<IEnumerable<TenderApplication>> GetByIncidentAsync(Guid incidentId);
    Task<IEnumerable<TenderApplication>> GetByContractorAsync(Guid contractorId, int page, int pageSize);
    Task<int> GetCountByContractorAsync(Guid contractorId);
    Task<bool> HasContractorAppliedAsync(Guid incidentId, Guid contractorId);
    Task<decimal> GetMinBidForIncidentAsync(Guid incidentId);
    Task<Guid> CreateAsync(TenderApplication application);
    Task UpdateAsync(TenderApplication application);
    Task UpdateRangeAsync(IEnumerable<TenderApplication> applications);
}