namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record UserListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    bool IsActive,
    Guid RoleId,
    string? RoleName);

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages
    {
        get
        {
            if (PageSize <= 0) return 0;
            return (int)Math.Ceiling(TotalCount / (double)PageSize);
        }
    }

    public bool HasItems => Items.Count > 0;
}

public sealed record CreateUserRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string? Password,
    Guid RoleId);

public sealed record UpdateUserRequestDto(
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool IsActive,
    Guid RoleId);