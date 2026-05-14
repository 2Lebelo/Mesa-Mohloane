using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IContractorProfileApiService
{
    Task<(bool Ok, ContractorProfileDto? Data, string? Error)> CreateAsync(ContractorProfileCreateDto dto);
    Task<(bool Ok, ContractorProfileDto? Data, string? Error)> UpdateAsync(Guid id, ContractorProfileUpdateDto dto);
    Task<(bool Ok, ContractorProfileDto? Data, string? Error)> ApproveAsync(Guid id);

    Task<ContractorProfileDto?> GetMineAsync();
    Task<ContractorProfileDto?> GetByIdAsync(Guid id);

    Task<PagedResultDto<ContractorProfileDto>?> GetAllApprovedAsync(int page = 1, int pageSize = 10);
    Task<PagedResultDto<ContractorProfileDto>?> GetAllAsync(int page = 1, int pageSize = 10, bool? isApproved = null);
}

public sealed class ContractorProfileApiService : ApiClientBase, IContractorProfileApiService
{
    private const string Base = "/api/contractor-profiles";

    public ContractorProfileApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, ContractorProfileDto? Data, string? Error)> CreateAsync(ContractorProfileCreateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<ContractorProfileDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<PagedResultDto<ContractorProfileDto>?> GetAllAsync(
    int page = 1,
    int pageSize = 10,
    bool? isApproved = null)
    {
        try
        {
            var query = $"?page={page}&pageSize={pageSize}";

            if (isApproved.HasValue)
                query += $"&isApproved={isApproved.Value.ToString().ToLowerInvariant()}";

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/all{query}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<PagedResultDto<ContractorProfileDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Ok, ContractorProfileDto? Data, string? Error)> UpdateAsync(Guid id, ContractorProfileUpdateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PutAsync($"{Base}/{id}", JsonBody(dto));
            return await ParseResponse<ContractorProfileDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, ContractorProfileDto? Data, string? Error)> ApproveAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/approve", null);
            return await ParseResponse<ContractorProfileDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<ContractorProfileDto?> GetMineAsync()
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/me");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<ContractorProfileDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ContractorProfileDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/{id}");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<ContractorProfileDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PagedResultDto<ContractorProfileDto>?> GetAllApprovedAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}?page={page}&pageSize={pageSize}");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<PagedResultDto<ContractorProfileDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<(bool Ok, T? Data, string? Error)> ParseResponse<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(json)) return (true, default, null);
            return (true, JsonSerializer.Deserialize<T>(json, JsonOpts), null);
        }

        if (string.IsNullOrWhiteSpace(json)) return (false, default, res.ReasonPhrase);

        try
        {
            var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
            return (false, default, err?.Error ?? res.ReasonPhrase);
        }
        catch
        {
            return (false, default, res.ReasonPhrase);
        }
    }
}
