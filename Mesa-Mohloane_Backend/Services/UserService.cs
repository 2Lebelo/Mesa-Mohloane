using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;
using System.Security.Claims;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditRepository _audit;
    private readonly IHttpContextAccessor _http;

    public UserService(
        IUserRepository userRepo,
        IAuditRepository audit,
        IHttpContextAccessor http)
    {
        _userRepo = userRepo;
        _audit = audit;
        _http = http;
    }

    // Reads the admin's Guid from the JWT claim on every operation.
    // Returns null only if somehow called outside an authenticated context
    // (which the [Authorize] attribute on UsersController prevents).
    private Guid? CurrentAdminId
    {
        get
        {
            var raw = _http.HttpContext?.User.FindFirstValue(AppClaimTypes.UserId);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public async Task<PagedResultDto<UserDto>> GetAllAsync(
        int page, int pageSize, string? search, Guid? roleId, bool? isActive)
    {
        var items = await _userRepo.GetAllAsync(page, pageSize, search, roleId, isActive);
        var total = await _userRepo.GetTotalCountAsync(search, roleId, isActive);

        var dtos = items.Select(u => new UserDto(
            u.Id, u.FirstName, u.LastName, u.Email,
            u.PhoneNumber, u.IsActive, u.RoleId, u.Role?.Name)).ToList();

        return new PagedResultDto<UserDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null)
            return ServiceResult<UserDto>.Fail("User not found.");

        return ServiceResult<UserDto>.Ok(new UserDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.PhoneNumber, user.IsActive, user.RoleId, user.Role?.Name));
    }

    public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
    {
        if (await _userRepo.EmailExistsAsync(dto.Email))
            return ServiceResult<UserDto>.Fail("Email already in use.");

        if (await _userRepo.PhoneNumberExistsAsync(dto.PhoneNumber))
            return ServiceResult<UserDto>.Fail("Phone number already in use.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.ToLower().Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            PasswordHash = string.IsNullOrWhiteSpace(dto.Password)
                ? null
                : BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = dto.RoleId,
            IsActive = true
        };

        var id = await _userRepo.CreateAsync(user);

        // CurrentAdminId resolves the real Guid from the JWT —
        // null is no longer possible here because UsersController is [Authorize]
        await _audit.LogAsync(
            "UserCreated", "User",
            id,
            performedBy: _http.HttpContext?.User.FindFirstValue(AppClaimTypes.Email) ?? "system",
            userId: CurrentAdminId,
            details: $"Email: {dto.Email}");

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null)
            return ServiceResult<UserDto>.Fail("User not found.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber.Trim();
        user.IsActive = dto.IsActive;
        user.RoleId = dto.RoleId;

        await _userRepo.UpdateAsync(user);

        await _audit.LogAsync(
            "UserUpdated", "User",
            id,
            performedBy: _http.HttpContext?.User.FindFirstValue(AppClaimTypes.Email) ?? "system",
            userId: CurrentAdminId,
            details: $"Email: {user.Email}");

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null)
            return ServiceResult.Fail("User not found.");

        var incidentsCount = await _userRepo.GetUserIncidentsCountAsync(id);
        var applicationsCount = await _userRepo.GetUserTenderApplicationsCountAsync(id);

        if (incidentsCount > 0 || applicationsCount > 0)
            return ServiceResult.Fail(
                "Cannot delete user with active incidents or tender applications.");

        await _userRepo.DeleteAsync(id);

        await _audit.LogAsync(
            "UserDeleted", "User",
            id,
            performedBy: _http.HttpContext?.User.FindFirstValue(AppClaimTypes.Email) ?? "system",
            userId: CurrentAdminId,
            details: $"Email: {user.Email}");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ToggleActiveAsync(Guid id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null)
            return ServiceResult.Fail("User not found.");

        var newStatus = !user.IsActive;
        await _userRepo.ToggleActiveStatusAsync(id, newStatus);

        await _audit.LogAsync(
            "UserStatusToggled", "User",
            id,
            performedBy: _http.HttpContext?.User.FindFirstValue(AppClaimTypes.Email) ?? "system",
            userId: CurrentAdminId,
            details: $"Status changed to: {(newStatus ? "Active" : "Inactive")}");

        return ServiceResult.Ok();
    }

    public async Task<PagedResultDto<UserDto>> GetContractorsAsync(
        int page, int pageSize, string? search)
    {
        var items = await _userRepo.GetContractorsAsync(page, pageSize, search);
        var total = await _userRepo.GetTotalCountAsync(search, null, null);
        return BuildPaged(items, page, pageSize, total);
    }

    public async Task<PagedResultDto<UserDto>> GetCitizensAsync(
        int page, int pageSize, string? search)
    {
        var items = await _userRepo.GetCitizensAsync(page, pageSize, search);
        var total = await _userRepo.GetTotalCountAsync(search, null, null);
        return BuildPaged(items, page, pageSize, total);
    }

    public async Task<PagedResultDto<UserDto>> GetAdministratorsAsync(
        int page, int pageSize, string? search)
    {
        var items = await _userRepo.GetAdministratorsAsync(page, pageSize, search);
        var total = await _userRepo.GetTotalCountAsync(search, null, null);
        return BuildPaged(items, page, pageSize, total);
    }

    public async Task<PagedResultDto<UserDto>> GetAuditorsAsync(
        int page, int pageSize, string? search)
    {
        var items = await _userRepo.GetAuditorsAsync(page, pageSize, search);
        var total = await _userRepo.GetTotalCountAsync(search, null, null);
        return BuildPaged(items, page, pageSize, total);
    }

    // ── Private helper ────────────────────────────────────────────────────────
    private static PagedResultDto<UserDto> BuildPaged(
        IEnumerable<User> items, int page, int pageSize, int total)
    {
        return new PagedResultDto<UserDto>
        {
            Items = items.Select(u => new UserDto(
                u.Id, u.FirstName, u.LastName, u.Email,
                u.PhoneNumber, u.IsActive, u.RoleId, u.Role?.Name)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}