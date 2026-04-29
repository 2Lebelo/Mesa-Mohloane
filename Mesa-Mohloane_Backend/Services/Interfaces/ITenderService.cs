using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface ITenderService
{
    // Contractor operations
    Task<ServiceResult<TenderApplicationDto>> SubmitAsync(Guid contractorId, TenderApplicationCreateDto dto);
    Task<ServiceResult<TenderApplicationDto>> UpdateAsync(Guid applicationId, Guid contractorId, TenderApplicationUpdateDto dto);
    Task<ServiceResult> WithdrawAsync(Guid applicationId, Guid contractorId);

    // Admin operations
    Task<ServiceResult<IReadOnlyList<TenderApplicationDto>>> EvaluateAndRankAsync(Guid incidentId, Guid adminId);

    // Queries
    Task<ServiceResult<TenderApplicationDto>> GetByIdAsync(Guid id);
    Task<IReadOnlyList<TenderApplicationDto>> GetByIncidentAsync(Guid incidentId);
    Task<PagedResultDto<TenderApplicationListDto>> GetByContractorAsync(Guid contractorId, int page, int pageSize);
}