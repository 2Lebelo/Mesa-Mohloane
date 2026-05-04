using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(Guid id);
    Task<Assignment?> GetByIncidentAsync(Guid incidentId);
    Task<Assignment?> GetByContractorAsync(Guid contractorId);
    Task<IEnumerable<Assignment>> GetByContractorAsync(Guid contractorId, int page, int pageSize);
    Task<int> GetCountByContractorAsync(Guid contractorId);
    Task<IEnumerable<Assignment>> GetAllAsync(int page, int pageSize, AssignmentStatus? status);
    Task<int> GetTotalCountAsync(AssignmentStatus? status);
    Task<Guid> CreateAsync(Assignment assignment);
    Task UpdateAsync(Assignment assignment);
    Task<WorkCompletion?> GetWorkCompletionAsync(Guid assignmentId);
    Task<Guid> CreateWorkCompletionAsync(WorkCompletion workCompletion);
}