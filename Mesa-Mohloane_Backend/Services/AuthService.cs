using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class AuthService : IAuthService
{
    // Roles that are permitted to self-register.
    // Inspector / Auditor accounts must be created by an Administrator.
    private static readonly HashSet<string> SelfRegistrationRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Citizen", "Contractor" };

    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly JwtHelper _jwt;
    private readonly IAuditRepository _audit;

    public AuthService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        JwtHelper jwt,
        IAuditRepository audit)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _jwt = jwt;
        _audit = audit;
    }

    // Register
    public async Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        // 1. Resolve and validate the requested role
        var role = await _roleRepo.GetByIdAsync(dto.RoleId);
        if (role is null)
            return ServiceResult<AuthResponseDto>.Fail("Invalid role selected.");

        if (!SelfRegistrationRoles.Contains(role.Name))
            return ServiceResult<AuthResponseDto>.Fail(
                "You are not permitted to self-register with this role. " +
                "Please contact the administrator.");

        // 2. Duplicate-email guard
        if (await _userRepo.EmailExistsAsync(dto.Email))
            return ServiceResult<AuthResponseDto>.Fail(
                "An account with this email already exists.");

        // 3. Create the user
        var user = new User
        {
            FirstName    = dto.FirstName.Trim(),
            LastName     = dto.LastName.Trim(),
            Email        = dto.Email.ToLower().Trim(),
            PhoneNumber  = dto.PhoneNumber?.Trim() ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = dto.RoleId,
            IsActive     = true
        };

        var userId = await _userRepo.CreateAsync(user);

        // 4. Re-fetch so that the Role navigation property is populated
        //    before we generate the JWT (which reads Role.Name)
        var createdUser = await _userRepo.GetByIdAsync(userId);
        if (createdUser is null)
            return ServiceResult<AuthResponseDto>.Fail(
                "Registration failed. Please try again.");

        var token = _jwt.GenerateToken(createdUser);

        await _audit.LogAsync(
            action     : "UserRegistered",
            entityName : "User",
            entityId   : userId.ToString(),
            performedBy: createdUser.Email,
            details    : $"Role: {createdUser.Role!.Name}");

        return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token     = token,
            Email     = createdUser.Email,
            FullName  = $"{createdUser.FirstName} {createdUser.LastName}",
            Role      = createdUser.Role.Name,
            UserId    = userId,
            ExpiresAt = _jwt.GetExpiry()
        });
    }

    // Login
    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLower().Trim());

        // Deliberately vague message — do not reveal whether the email exists
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return ServiceResult<AuthResponseDto>.Fail("Invalid email or password.");

        if (!user.IsActive)
            return ServiceResult<AuthResponseDto>.Fail(
                "Your account has been deactivated. Please contact the administrator.");

        var token = _jwt.GenerateToken(user);

        await _audit.LogAsync(
            action     : "UserLogin",
            entityName : "User",
            entityId   : user.Id.ToString(),
            performedBy: user.Email,
            details    : "Login successful");

        return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token     = token,
            Email     = user.Email,
            FullName  = $"{user.FirstName} {user.LastName}",
            Role      = user.Role!.Name,
            UserId    = user.Id,
            ExpiresAt = _jwt.GetExpiry()
        });
    }

    // Change Password
    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ServiceResult.Fail("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return ServiceResult.Fail("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepo.UpdateAsync(user);

        await _audit.LogAsync(
            action     : "PasswordChanged",
            entityName : "User",
            entityId   : userId.ToString(),
            performedBy: user.Email,
            details    : "Password changed successfully");

        return ServiceResult.Ok();
    }

    // Get Profile
    public async Task<ServiceResult<UserDto>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ServiceResult<UserDto>.Fail("User not found.");

        return ServiceResult<UserDto>.Ok(new UserDto(
            Id         : user.Id,
            FirstName  : user.FirstName,
            LastName   : user.LastName,
            Email      : user.Email,
            PhoneNumber: user.PhoneNumber,
            IsActive   : user.IsActive,
            RoleId     : user.RoleId,
            RoleName   : user.Role?.Name));
    }

    // Update Profile  (own profile — no role or IsActive changes allowed)
    public async Task<ServiceResult> UpdateProfileAsync(Guid userId, UserUpdateDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ServiceResult.Fail("User not found.");

        user.FirstName   = dto.FirstName.Trim();
        user.LastName    = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber.Trim();

        // IsActive and RoleId in UserUpdateDto are intentionally ignored here;
        // those fields are managed through the admin user-management endpoint.

        await _userRepo.UpdateAsync(user);

        await _audit.LogAsync(
            action     : "ProfileUpdated",
            entityName : "User",
            entityId   : userId.ToString(),
            performedBy: user.Email,
            details    : "Profile details updated");

        return ServiceResult.Ok();
    }
}