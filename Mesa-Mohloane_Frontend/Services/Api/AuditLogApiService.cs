using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IAuditLogApiService
{
    Task<PagedResultDto<AuditLogDto>?> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? entityName = null,
        string? actionType = null,
        Guid? actorUserId = null,
        DateTime? from = null,
        DateTime? to = null);

    Task<PagedResultDto<AuditLogDto>?> GetByEntityAsync(string entityName, Guid entityId, int page = 1, int pageSize = 20);
}

public sealed class AuditLogApiService : ApiClientBase, IAuditLogApiService
{
    private const string Base = "/api/auditlogs";

    public AuditLogApiService(IHttpClientFactory httpFactory, IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<PagedResultDto<AuditLogDto>?> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? entityName = null,
        string? actionType = null,
        Guid? actorUserId = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        try
        {
            var query = $"?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(entityName)) query += $"&entityName={Uri.EscapeDataString(entityName)}";
            if (!string.IsNullOrWhiteSpace(actionType)) query += $"&actionType={Uri.EscapeDataString(actionType)}";
            if (actorUserId.HasValue) query += $"&actorUserId={actorUserId.Value}";
            if (from.HasValue) query += $"&from={Uri.EscapeDataString(from.Value.ToString("O"))}";
            if (to.HasValue) query += $"&to={Uri.EscapeDataString(to.Value.ToString("O"))}";

            var client = CreateClient();
            var res = await client.GetAsync(Base + query);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<AuditLogDto>>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<PagedResultDto<AuditLogDto>?> GetByEntityAsync(
        string entityName,
        Guid entityId,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var client = CreateClient();
            var safeEntity = Uri.EscapeDataString(entityName);
            var res = await client.GetAsync($"{Base}/entity/{safeEntity}/{entityId}?page={page}&pageSize={pageSize}");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<AuditLogDto>>(json, JsonOpts);
        }
        catch { return null; }
    }
}
