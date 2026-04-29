using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IContractorRatingService
{
    Task<ServiceResult<ContractorRatingDto>> RateAsync(Guid citizenId, ContractorRatingCreateDto dto);
    Task<ServiceResult<ContractorRatingDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<ContractorRatingDto>> GetByAssignmentAsync(Guid assignmentId);
    Task<PagedResultDto<ContractorRatingDto>> GetByContractorAsync(
        Guid contractorId, int page, int pageSize);
}