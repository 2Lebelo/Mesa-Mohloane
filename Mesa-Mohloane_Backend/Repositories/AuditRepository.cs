using Dapper;
using Microsoft.Data.SqlClient;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;

namespace Mesa_Mohloane_Backend.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly string _conn;

    public AuditRepository(IConfiguration config)
        => _conn = config.GetConnectionString("DefaultConnection")!;

    private SqlConnection Create() => new(_conn);

    private async Task CoreLogAsync(
        string action,
        string entityName,
        Guid? entityId,
        Guid? actorUserId,
        string? performedBy = null,
        string? details = null,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? ipAddress = null)
    {
        using var db = Create();

        await db.ExecuteAsync(@"
            INSERT INTO AuditLogs (
                Id,
                ActorUserId,
                ActionType,
                EntityName,
                EntityId,
                OldValuesJson,
                NewValuesJson,
                IpAddress,
                ActionAt,
                Notes,
                CreatedAt,
                IsDeleted
            ) VALUES (
                @Id,
                @ActorUserId,
                @ActionType,
                @EntityName,
                @EntityId,
                @OldValuesJson,
                @NewValuesJson,
                @IpAddress,
                GETUTCDATE(),
                @Notes,
                GETUTCDATE(),
                0
            )",
            new
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                ActionType = ParseActionType(action),
                EntityName = entityName,
                EntityId = entityId,
                OldValuesJson = oldValuesJson,
                NewValuesJson = newValuesJson,
                IpAddress = ipAddress,
                Notes = BuildNotes(performedBy, details)
            });
    }

    public Task LogAsync(
        string action,
        string entityName,
        Guid? entityId,
        Guid actorUserId,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? ipAddress = null,
        string? notes = null)
        => CoreLogAsync(
            action: action,
            entityName: entityName,
            entityId: entityId,
            actorUserId: actorUserId,
            details: notes,
            oldValuesJson: oldValuesJson,
            newValuesJson: newValuesJson,
            ipAddress: ipAddress);

    public Task LogAsync(
        string action,
        string entityName,
        string entityId,
        string performedBy,
        string? details = null)
    {
        var parsedEntityId = Guid.TryParse(entityId, out var entityGuid)
            ? entityGuid
            : (Guid?)null;

        var parsedActorUserId = Guid.TryParse(performedBy, out var actorGuid)
            ? actorGuid
            : (Guid?)null;

        return CoreLogAsync(
            action: action,
            entityName: entityName,
            entityId: parsedEntityId,
            actorUserId: parsedActorUserId,
            performedBy: performedBy,
            details: details);
    }

    public Task LogAsync(
        string action,
        string entityName,
        Guid entityId,
        string performedBy,
        Guid? userId,
        string? details = null)
        => CoreLogAsync(
            action: action,
            entityName: entityName,
            entityId: entityId,
            actorUserId: userId,
            performedBy: performedBy,
            details: details);

    public async Task<IEnumerable<AuditLog>> GetAllAsync(int page, int pageSize)
    {
        using var db = Create();

        return await db.QueryAsync<AuditLog, User, AuditLog>(@"
            SELECT
                a.*,
                u.Id, u.FirstName, u.LastName, u.Email
            FROM AuditLogs a
            LEFT JOIN Users u ON a.ActorUserId = u.Id
            WHERE a.IsDeleted = 0
            ORDER BY a.ActionAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            (log, user) =>
            {
                log.ActorUser = user;
                return log;
            },
            new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            splitOn: "Id");
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 50)
    {
        using var db = Create();

        return await db.QueryAsync<AuditLog, User, AuditLog>(@"
            SELECT TOP (@Count)
                a.*,
                u.Id, u.FirstName, u.LastName, u.Email
            FROM AuditLogs a
            LEFT JOIN Users u ON a.ActorUserId = u.Id
            WHERE a.IsDeleted = 0
            ORDER BY a.ActionAt DESC",
            (log, user) =>
            {
                log.ActorUser = user;
                return log;
            },
            new { Count = count },
            splitOn: "Id");
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        using var db = Create();

        return await db.QueryAsync<AuditLog, User, AuditLog>(@"
            SELECT
                a.*,
                u.Id, u.FirstName, u.LastName, u.Email
            FROM AuditLogs a
            LEFT JOIN Users u ON a.ActorUserId = u.Id
            WHERE a.ActorUserId = @UserId
              AND a.IsDeleted = 0
            ORDER BY a.ActionAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            (log, user) =>
            {
                log.ActorUser = user;
                return log;
            },
            new
            {
                UserId = userId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            splitOn: "Id");
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        int page,
        int pageSize)
    {
        using var db = Create();

        return await db.QueryAsync<AuditLog, User, AuditLog>(@"
            SELECT
                a.*,
                u.Id, u.FirstName, u.LastName, u.Email
            FROM AuditLogs a
            LEFT JOIN Users u ON a.ActorUserId = u.Id
            WHERE a.EntityName = @EntityName
              AND a.EntityId   = @EntityId
              AND a.IsDeleted  = 0
            ORDER BY a.ActionAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            (log, user) =>
            {
                log.ActorUser = user;
                return log;
            },
            new
            {
                EntityName = entityName,
                EntityId = entityId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            splitOn: "Id");
    }

    public async Task<IEnumerable<AuditLog>> GetByActionTypeAsync(
        AuditActionType actionType,
        int page,
        int pageSize)
    {
        using var db = Create();

        return await db.QueryAsync<AuditLog, User, AuditLog>(@"
            SELECT
                a.*,
                u.Id, u.FirstName, u.LastName, u.Email
            FROM AuditLogs a
            LEFT JOIN Users u ON a.ActorUserId = u.Id
            WHERE a.ActionType = @ActionType
              AND a.IsDeleted  = 0
            ORDER BY a.ActionAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            (log, user) =>
            {
                log.ActorUser = user;
                return log;
            },
            new
            {
                ActionType = actionType,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            splitOn: "Id");
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int page,
        int pageSize)
    {
        using var db = Create();

        return await db.QueryAsync<AuditLog, User, AuditLog>(@"
            SELECT
                a.*,
                u.Id, u.FirstName, u.LastName, u.Email
            FROM AuditLogs a
            LEFT JOIN Users u ON a.ActorUserId = u.Id
            WHERE a.ActionAt >= @StartDate
              AND a.ActionAt <= @EndDate
              AND a.IsDeleted  = 0
            ORDER BY a.ActionAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            (log, user) =>
            {
                log.ActorUser = user;
                return log;
            },
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            splitOn: "Id");
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var db = Create();

        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AuditLogs WHERE IsDeleted = 0");
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        using var db = Create();

        return await db.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) 
              FROM AuditLogs 
              WHERE ActorUserId = @UserId 
                AND IsDeleted = 0",
            new { UserId = userId });
    }

    public async Task<int> GetCountByEntityAsync(string entityName, Guid entityId)
    {
        using var db = Create();

        return await db.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) 
              FROM AuditLogs 
              WHERE EntityName = @EntityName 
                AND EntityId = @EntityId 
                AND IsDeleted = 0",
            new
            {
                EntityName = entityName,
                EntityId = entityId
            });
    }

    private static string? BuildNotes(string? performedBy, string? details)
    {
        if (string.IsNullOrWhiteSpace(performedBy) && string.IsNullOrWhiteSpace(details))
            return null;

        if (string.IsNullOrWhiteSpace(performedBy))
            return details;

        if (string.IsNullOrWhiteSpace(details))
            return performedBy;

        return $"{performedBy} | {details}";
    }

    private static AuditActionType ParseActionType(string action)
    {
        var a = action.ToLowerInvariant();

        return a switch
        {
            _ when a.Contains("create") || a.Contains("register") => AuditActionType.Create,
            _ when a.Contains("update") || a.Contains("edit") || a.Contains("modify") => AuditActionType.Update,
            _ when a.Contains("delete") || a.Contains("remove") => AuditActionType.Delete,
            _ when a.Contains("login") || a.Contains("signin") => AuditActionType.Login,
            _ when a.Contains("logout") || a.Contains("signout") => AuditActionType.Logout,
            _ when a.Contains("approve") => AuditActionType.Approve,
            _ when a.Contains("reject") => AuditActionType.Reject,
            _ when a.Contains("assign") => AuditActionType.Assign,
            _ when a.Contains("submit") => AuditActionType.Submit,
            _ when a.Contains("complete") => AuditActionType.Complete,
            _ when a.Contains("verify") => AuditActionType.Verify,
            _ when a.Contains("photo") => AuditActionType.Update,
            _ when a.Contains("rate") || a.Contains("rating") => AuditActionType.Rate,
            _ => AuditActionType.Other
        };
    }

}