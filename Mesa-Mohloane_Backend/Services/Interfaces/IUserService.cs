using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Helpers;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IUserService
{
    Task<PagedResultDto<UserDto>> GetAllAsync(int page, int pageSize, string? search, Guid? roleId, bool? isActive);
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult> ToggleActiveAsync(Guid id);
    Task<PagedResultDto<UserDto>> GetContractorsAsync(int page, int pageSize, string? search);
    Task<PagedResultDto<UserDto>> GetCitizensAsync(int page, int pageSize, string? search);
    Task<PagedResultDto<UserDto>> GetAdministratorsAsync(int page, int pageSize, string? search);
    Task<PagedResultDto<UserDto>> GetAuditorsAsync(int page, int pageSize, string? search);
}