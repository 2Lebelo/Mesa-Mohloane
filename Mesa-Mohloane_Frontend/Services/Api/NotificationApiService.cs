using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface INotificationApiService
{
    Task<PagedResultDto<NotificationDto>?> GetMineAsync(
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false);

    Task<int> GetUnreadCountAsync();

    Task<(bool Ok, NotificationDto? Data, string? Error)> MarkAsReadAsync(Guid id);

    Task<(bool Ok, string? Error)> MarkAllAsReadAsync();
}

public sealed class NotificationApiService : ApiClientBase, INotificationApiService
{
    private const string Base = "/api/notifications";

    public NotificationApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<PagedResultDto<NotificationDto>?> GetMineAsync(
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var query =
                $"?page={page}&pageSize={pageSize}&unreadOnly={unreadOnly.ToString().ToLowerInvariant()}";

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}{query}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<NotificationDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/unread-count");

            if (!res.IsSuccessStatusCode)
                return 0;

            var json = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("unreadCount", out var unreadCount))
                return unreadCount.GetInt32();

            if (root.TryGetProperty("count", out var count))
                return count.GetInt32();

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<(bool Ok, NotificationDto? Data, string? Error)> MarkAsReadAsync(Guid id)
    {
        if (id == Guid.Empty)
            return (false, null, "Invalid notification id.");

        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/read", null);
            return await ParseResponse<NotificationDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, string? Error)> MarkAllAsReadAsync()
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/read-all", null);

            if (res.IsSuccessStatusCode)
                return (true, null);

            return (false, await ReadErrorAsync(res));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<(bool Ok, T? Data, string? Error)> ParseResponse<T>(
        HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (true, default, null);

            return (true, JsonSerializer.Deserialize<T>(json, JsonOpts), null);
        }

        return (false, default, await ReadErrorAsync(res, json));
    }

    private static async Task<string?> ReadErrorAsync(
        HttpResponseMessage res,
        string? existingJson = null)
    {
        try
        {
            var json = existingJson ?? await res.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
                return $"{(int)res.StatusCode} {res.ReasonPhrase}";

            var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);

            if (!string.IsNullOrWhiteSpace(err?.Error))
                return err.Error;

            return json;
        }
        catch
        {
            return $"{(int)res.StatusCode} {res.ReasonPhrase}";
        }
    }
}