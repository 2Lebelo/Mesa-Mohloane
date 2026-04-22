namespace Mesa_Mohloane_Backend.Models.DTOs;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    bool IsActive,
    Guid RoleId,
    string? RoleName);

public record UserCreateDto(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string? Password,
    Guid RoleId);

public record UserUpdateDto(
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool IsActive,
    Guid RoleId);
