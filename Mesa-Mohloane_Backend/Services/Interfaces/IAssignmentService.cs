using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IAssignmentService
{
    // Admin operations
    Task<ServiceResult<AssignmentDto>> AssignContractorAsync(Guid incidentId, Guid tenderApplicationId, Guid adminId);

    // Contractor operations
    Task<ServiceResult<AssignmentDto>> StartWorkAsync(Guid assignmentId, Guid contractorId);
    Task<ServiceResult<WorkCompletionDto>> SubmitWorkCompletionAsync(Guid assignmentId, Guid contractorId, WorkCompletionCreateDto dto);

    // Citizen operations
    Task<ServiceResult<AssignmentDto>> AcknowledgeCompletionAsync(Guid assignmentId, Guid citizenId);

    // Admin operations
    Task<ServiceResult<AssignmentDto>> ApproveCompletionAsync(Guid assignmentId, Guid adminId);

    // Queries
    Task<ServiceResult<AssignmentDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<AssignmentDto>> GetByIncidentAsync(Guid incidentId);
}