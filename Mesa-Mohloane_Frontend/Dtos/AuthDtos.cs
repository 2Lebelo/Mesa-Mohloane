namespace Mesa_Mohloane_Frontend.Dtos;

// ── Auth ──────────────────────────────────────────────────────────────────────

public sealed class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>Guid of the selected role — populated from GET /api/roles/public.</summary>
    public Guid RoleId { get; set; }
}

public sealed class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public sealed class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class ApiErrorDto
{
    public string? Error { get; set; }
}