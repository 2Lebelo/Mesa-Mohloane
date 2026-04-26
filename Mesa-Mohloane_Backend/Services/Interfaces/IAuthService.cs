using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Helpers;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto dto);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<ServiceResult<UserDto>> GetProfileAsync(Guid userId);
    Task<ServiceResult> UpdateProfileAsync(Guid userId, UserUpdateDto dto);
}