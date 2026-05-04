using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IContractorProfileService
{
    Task<ServiceResult<ContractorProfileDto>> CreateAsync(ContractorProfileCreateDto dto);
    Task<ServiceResult<ContractorProfileDto>> UpdateAsync(Guid id, ContractorProfileUpdateDto dto, Guid requesterId);
    Task<ServiceResult<ContractorProfileDto>> ApproveAsync(Guid id, Guid adminId);

    Task<ServiceResult<ContractorProfileDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<ContractorProfileDto>> GetByUserIdAsync(Guid userId);

    Task<PagedResultDto<ContractorProfileDto>> GetAllApprovedAsync(int page, int pageSize);
    Task<PagedResultDto<ContractorProfileDto>> GetAllAsync(int page, int pageSize, bool? isApproved = null);
}