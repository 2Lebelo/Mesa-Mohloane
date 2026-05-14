using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IAuditLogApiService
{
    Task<AuditLogPagedResultDto> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? entityName = null,
        string? actionType = null,
        Guid? actorUserId = null,
        DateTime? from = null,
        DateTime? to = null);

    Task<AuditLogPagedResultDto> GetByEntityAsync(
        string entityName,
        Guid entityId,
        int page = 1,
        int pageSize = 20);
}

public sealed class AuditLogApiService : ApiClientBase, IAuditLogApiService
{
    private const string Base = "/api/auditlogs";

    public AuditLogApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor)
    {
    }

    public async Task<AuditLogPagedResultDto> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? entityName = null,
        string? actionType = null,
        Guid? actorUserId = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 20 : pageSize;

        try
        {
            var query = $"?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrWhiteSpace(entityName))
                query += $"&entityName={Uri.EscapeDataString(entityName.Trim())}";

            if (!string.IsNullOrWhiteSpace(actionType))
                query += $"&actionType={Uri.EscapeDataString(actionType.Trim())}";

            if (actorUserId.HasValue && actorUserId.Value != Guid.Empty)
                query += $"&actorUserId={actorUserId.Value}";

            if (from.HasValue)
                query += $"&from={Uri.EscapeDataString(from.Value.ToString("O"))}";

            if (to.HasValue)
                query += $"&to={Uri.EscapeDataString(to.Value.ToString("O"))}";

            var client = CreateClient();
            var res = await client.GetAsync(Base + query);

            if (!res.IsSuccessStatusCode)
                return Empty(page, pageSize);

            var json = await res.Content.ReadAsStringAsync();

            return ReadPagedResult(json, page, pageSize);
        }
        catch
        {
            return Empty(page, pageSize);
        }
    }

    public async Task<AuditLogPagedResultDto> GetByEntityAsync(
        string entityName,
        Guid entityId,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 20 : pageSize;

        try
        {
            if (string.IsNullOrWhiteSpace(entityName) || entityId == Guid.Empty)
                return Empty(page, pageSize);

            var safeEntityName = Uri.EscapeDataString(entityName.Trim());

            var client = CreateClient();
            var res = await client.GetAsync(
                $"{Base}/entity/{safeEntityName}/{entityId}?page={page}&pageSize={pageSize}");

            if (!res.IsSuccessStatusCode)
                return Empty(page, pageSize);

            var json = await res.Content.ReadAsStringAsync();

            return ReadPagedResult(json, page, pageSize);
        }
        catch
        {
            return Empty(page, pageSize);
        }
    }

    private static AuditLogPagedResultDto ReadPagedResult(
        string json,
        int page,
        int pageSize)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Empty(page, pageSize);

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var arrayItems = JsonSerializer.Deserialize<List<AuditLogDto>>(json, JsonOpts)
                             ?? new List<AuditLogDto>();

            return new AuditLogPagedResultDto
            {
                Items = arrayItems,
                TotalCount = arrayItems.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        var paged = JsonSerializer.Deserialize<AuditLogPagedResultDto>(json, JsonOpts);

        return paged ?? Empty(page, pageSize);
    }

    private static AuditLogPagedResultDto Empty(int page, int pageSize)
        => new()
        {
            Items = new List<AuditLogDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
}