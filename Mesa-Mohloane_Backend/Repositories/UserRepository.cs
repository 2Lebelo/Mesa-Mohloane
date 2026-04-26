using Dapper;
using Microsoft.Data.SqlClient;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;

namespace Mesa_Mohloane_Backend.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _conn;

    public UserRepository(IConfiguration config)
        => _conn = config.GetConnectionString("DefaultConnection")!;

    private SqlConnection Create() => new(_conn);

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var db = Create();
        var users = await db.QueryAsync<User, Role, User>(@"
            SELECT u.*, r.Id, r.Name, r.Description
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.Id
            WHERE u.Id = @Id AND u.IsDeleted = 0",
            (user, role) =>
            {
                user.Role = role;
                return user;
            },
            new { Id = id },
            splitOn: "Id");

        return users.FirstOrDefault();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var db = Create();
        var users = await db.QueryAsync<User, Role, User>(@"
            SELECT u.*, r.Id, r.Name, r.Description
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.Id
            WHERE u.Email = @Email AND u.IsDeleted = 0",
            (user, role) =>
            {
                user.Role = role;
                return user;
            },
            new { Email = email },
            splitOn: "Id");

        return users.FirstOrDefault();
    }

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, string? search, Guid? roleId, bool? isActive)
    {
        using var db = Create();
        var sql = @"
            SELECT u.*, r.Id, r.Name, r.Description
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.Id
            WHERE u.IsDeleted = 0
              AND (@Search IS NULL OR u.FirstName LIKE '%'+@Search+'%'
                                   OR u.LastName  LIKE '%'+@Search+'%'
                                   OR u.Email     LIKE '%'+@Search+'%'
                                   OR u.PhoneNumber LIKE '%'+@Search+'%')
              AND (@RoleId IS NULL OR u.RoleId = @RoleId)
              AND (@IsActive IS NULL OR u.IsActive = @IsActive)
            ORDER BY u.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var users = await db.QueryAsync<User, Role, User>(
            sql,
            (user, role) =>
            {
                user.Role = role;
                return user;
            },
            new
            {
                Search = search,
                RoleId = roleId,
                IsActive = isActive,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            splitOn: "Id");

        return users;
    }

    public async Task<int> GetTotalCountAsync(string? search, Guid? roleId, bool? isActive)
    {
        using var db = Create();
        return await db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Users
            WHERE IsDeleted = 0
              AND (@Search IS NULL OR FirstName LIKE '%'+@Search+'%'
                                   OR LastName  LIKE '%'+@Search+'%'
                                   OR Email     LIKE '%'+@Search+'%'
                                   OR PhoneNumber LIKE '%'+@Search+'%')
              AND (@RoleId IS NULL OR RoleId = @RoleId)
              AND (@IsActive IS NULL OR IsActive = @IsActive)",
            new { Search = search, RoleId = roleId, IsActive = isActive });
    }

    public async Task<Guid> CreateAsync(User user)
    {
        using var db = Create();

        // Generate new GUID if not provided
        if (user.Id == Guid.Empty)
            user.Id = Guid.NewGuid();

        await db.ExecuteAsync(@"
            INSERT INTO Users (Id, FirstName, LastName, Email, PhoneNumber, PasswordHash,
                               IsActive, RoleId, CreatedAt, IsDeleted)
            VALUES (@Id, @FirstName, @LastName, @Email, @PhoneNumber, @PasswordHash,
                    @IsActive, @RoleId, GETUTCDATE(), 0)", user);

        return user.Id;
    }

    public async Task UpdateAsync(User user)
    {
        using var db = Create();
        await db.ExecuteAsync(@"
            UPDATE Users SET
                FirstName     = @FirstName,
                LastName      = @LastName,
                PhoneNumber   = @PhoneNumber,
                IsActive      = @IsActive,
                RoleId        = @RoleId,
                UpdatedAt     = GETUTCDATE()
            WHERE Id = @Id AND IsDeleted = 0", user);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var db = Create();
        await db.ExecuteAsync(
            "UPDATE Users SET IsDeleted=1, UpdatedAt=GETUTCDATE() WHERE Id=@Id",
            new { Id = id });
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        using var db = Create();
        return await db.ExecuteScalarAsync<bool>(
            "SELECT CAST(COUNT(1) AS BIT) FROM Users WHERE Email=@Email AND IsDeleted=0",
            new { Email = email });
    }

    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        using var db = Create();
        return await db.ExecuteScalarAsync<bool>(
            "SELECT CAST(COUNT(1) AS BIT) FROM Users WHERE PhoneNumber=@PhoneNumber AND IsDeleted=0",
            new { PhoneNumber = phoneNumber });
    }

    public async Task<IEnumerable<User>> GetContractorsAsync(int page, int pageSize, string? search)
    {
        using var db = Create();
        var sql = @"
            SELECT u.*, r.Id, r.Name, r.Description
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.Id
            WHERE u.IsDeleted = 0 
              AND r.Name = 'Contractor'
              AND (@Search IS NULL OR u.FirstName LIKE '%'+@Search+'%'
                                   OR u.LastName  LIKE '%'+@Search+'%'
                                   OR u.Email     LIKE '%'+@Search+'%')
            ORDER BY u.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var users = await db.QueryAsync<User, Role, User>(
            sql,
            (user, role) =>
            {
                user.Role = role;
                return user;
            },
            new { Search = search, Offset = (page - 1) * pageSize, PageSize = pageSize },
            splitOn: "Id");

        return users;
    }

    public async Task<IEnumerable<User>> GetCitizensAsync(int page, int pageSize, string? search)
    {
        using var db = Create();
        var sql = @"
            SELECT u.*, r.Id, r.Name, r.Description
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.Id
            WHERE u.IsDeleted = 0 
              AND r.Name = 'Citizen'
              AND (@Search IS NULL OR u.FirstName LIKE '%'+@Search+'%'
                                   OR u.LastName  LIKE '%'+@Search+'%'
                                   OR u.Email     LIKE '%'+@Search+'%')
            ORDER BY u.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var users = await db.QueryAsync<User, Role, User>(
            sql,
            (user, role) =>
            {
                user.Role = role;
                return user;
            },
            new { Search = search, Offset = (page - 1) * pageSize, PageSize = pageSize },
            splitOn: "Id");

        return users;
    }

    public async Task<IEnumerable<User>> GetAdministratorsAsync(int page, int pageSize, string? search)
    {
        using var db = Create();
        var sql = @"
            SELECT u.*, r.Id, r.Name, r.Description
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.Id
            WHERE u.IsDeleted = 0 
              AND r.Name = 'Administrator'
              AND (@Search IS NULL OR u.FirstName LIKE '%'+@Search+'%'
                                   OR u.LastName  LIKE '%'+@Search+'%'
                                   OR u.Email     LIKE '%'+@Search+'%')
            ORDER BY u.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var users = await db.QueryAsync<User, Role, User>(
            sql,
            (user, role) =>
            {
                user.Role = role;
                return user;
            },
            new { Search = search, Offset = (page - 1) * pageSize, PageSize = pageSize },
            splitOn: "Id");

        return users;
    }

    public async Task<IEnumerable<User>> GetAuditorsAsync(int page, int pageSize, string? search)
    {
        using var db = Create();
        var sql = @"
            SELECT u.*, r.Id, r.Name, r.Description
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.Id
            WHERE u.IsDeleted = 0 
              AND r.Name = 'Auditor'
              AND (@Search IS NULL OR u.FirstName LIKE '%'+@Search+'%'
                                   OR u.LastName  LIKE '%'+@Search+'%'
                                   OR u.Email     LIKE '%'+@Search+'%')
            ORDER BY u.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var users = await db.QueryAsync<User, Role, User>(
            sql,
            (user, role) =>
            {
                user.Role = role;
                return user;
            },
            new { Search = search, Offset = (page - 1) * pageSize, PageSize = pageSize },
            splitOn: "Id");

        return users;
    }

    public async Task<int> GetUserIncidentsCountAsync(Guid userId)
    {
        using var db = Create();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Incidents WHERE ReportedById=@UserId AND IsDeleted=0",
            new { UserId = userId });
    }

    public async Task<int> GetUserTenderApplicationsCountAsync(Guid userId)
    {
        using var db = Create();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TenderApplications WHERE ContractorId=@UserId AND IsDeleted=0",
            new { UserId = userId });
    }

    public async Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash)
    {
        using var db = Create();
        var rowsAffected = await db.ExecuteAsync(@"
            UPDATE Users 
            SET PasswordHash = @PasswordHash, 
                UpdatedAt = GETUTCDATE() 
            WHERE Id = @Id AND IsDeleted = 0",
            new { Id = userId, PasswordHash = passwordHash });

        return rowsAffected > 0;
    }

    public async Task<bool> ToggleActiveStatusAsync(Guid userId, bool isActive)
    {
        using var db = Create();
        var rowsAffected = await db.ExecuteAsync(@"
            UPDATE Users 
            SET IsActive = @IsActive, 
                UpdatedAt = GETUTCDATE() 
            WHERE Id = @Id AND IsDeleted = 0",
            new { Id = userId, IsActive = isActive });

        return rowsAffected > 0;
    }
}