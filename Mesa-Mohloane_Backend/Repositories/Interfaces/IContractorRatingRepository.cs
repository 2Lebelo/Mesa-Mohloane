using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IContractorRatingRepository
{
    Task<ContractorRating?> GetByIdAsync(Guid id);
    Task<ContractorRating?> GetByAssignmentAsync(Guid assignmentId);
    Task<IEnumerable<ContractorRating>> GetByContractorAsync(Guid contractorId, int page, int pageSize);
    Task<int> GetCountByContractorAsync(Guid contractorId);
    Task<decimal> GetAverageRatingAsync(Guid contractorId);
    Task<bool> HasCitizenRatedAsync(Guid assignmentId, Guid citizenId);
    Task<Guid> CreateAsync(ContractorRating rating);
}