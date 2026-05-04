using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IUserApiService
{
    Task<PagedResultDto<UserListItemDto>?> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, Guid? roleId = null, bool? isActive = null);
    Task<PagedResultDto<UserListItemDto>?> GetContractorsAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<PagedResultDto<UserListItemDto>?> GetCitizensAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<PagedResultDto<UserListItemDto>?> GetAdministratorsAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<PagedResultDto<UserListItemDto>?> GetAuditorsAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<UserListItemDto?> GetByIdAsync(Guid id);
    Task<(bool Ok, UserListItemDto? Data, string? Error)> CreateAsync(CreateUserRequestDto dto);
    Task<(bool Ok, UserListItemDto? Data, string? Error)> UpdateAsync(Guid id, UpdateUserRequestDto dto);
    Task<(bool Ok, string? Error)> DeleteAsync(Guid id);
    Task<(bool Ok, string? Error)> ToggleActiveAsync(Guid id);
}

public sealed class UserApiService : ApiClientBase, IUserApiService
{
    private const string Base = "/api/users";

    public UserApiService(IHttpClientFactory httpFactory, IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<PagedResultDto<UserListItemDto>?> GetAllAsync(
        int page = 1, int pageSize = 10, string? search = null, Guid? roleId = null, bool? isActive = null)
    {
        try
        {
            var query = $"?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
            if (roleId.HasValue) query += $"&roleId={roleId.Value}";
            if (isActive.HasValue) query += $"&isActive={isActive.Value.ToString().ToLowerInvariant()}";

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}{query}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<UserListItemDto>>(json, JsonOpts);
        }
        catch { return null; }
    }

    public Task<PagedResultDto<UserListItemDto>?> GetContractorsAsync(int page = 1, int pageSize = 10, string? search = null)
        => GetByRoleAsync("contractors", page, pageSize, search);

    public Task<PagedResultDto<UserListItemDto>?> GetCitizensAsync(int page = 1, int pageSize = 10, string? search = null)
        => GetByRoleAsync("citizens", page, pageSize, search);

    public Task<PagedResultDto<UserListItemDto>?> GetAdministratorsAsync(int page = 1, int pageSize = 10, string? search = null)
        => GetByRoleAsync("administrators", page, pageSize, search);

    public Task<PagedResultDto<UserListItemDto>?> GetAuditorsAsync(int page = 1, int pageSize = 10, string? search = null)
        => GetByRoleAsync("auditors", page, pageSize, search);

    public async Task<UserListItemDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/{id}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserListItemDto>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<(bool Ok, UserListItemDto? Data, string? Error)> CreateAsync(CreateUserRequestDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<UserListItemDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, UserListItemDto? Data, string? Error)> UpdateAsync(Guid id, UpdateUserRequestDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PutAsync($"{Base}/{id}", JsonBody(dto));
            return await ParseResponse<UserListItemDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.DeleteAsync($"{Base}/{id}");
            if (res.IsSuccessStatusCode) return (true, null);
            return (false, await ReadError(res));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Ok, string? Error)> ToggleActiveAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/toggle-active", null);
            if (res.IsSuccessStatusCode) return (true, null);
            return (false, await ReadError(res));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private async Task<PagedResultDto<UserListItemDto>?> GetByRoleAsync(
        string path, int page, int pageSize, string? search)
    {
        try
        {
            var query = $"?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/{path}{query}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<UserListItemDto>>(json, JsonOpts);
        }
        catch { return null; }
    }

    private static async Task<(bool Ok, T? Data, string? Error)> ParseResponse<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(json)) return (true, default, null);
            return (true, JsonSerializer.Deserialize<T>(json, JsonOpts), null);
        }
        return (false, default, await ReadError(res, json));
    }

    private static async Task<string?> ReadError(HttpResponseMessage res, string? jsonOverride = null)
    {
        try
        {
            var json = jsonOverride ?? await res.Content.ReadAsStringAsync();
            var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
            return err?.Error ?? res.ReasonPhrase;
        }
        catch { return res.ReasonPhrase; }
    }
}
