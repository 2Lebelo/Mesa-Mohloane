using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IRatingApiService
{
    Task<(bool Ok, ContractorRatingDto? Data, string? Error)> RateAsync(ContractorRatingCreateDto dto);
    Task<IReadOnlyList<ContractorRatingDto>?> GetByContractorAsync(Guid contractorId);
    Task<ContractorRatingDto?> GetByAssignmentAsync(Guid assignmentId);
    Task<PagedResultDto<ContractorRatingDto>?> GetMineAsync(int page = 1, int pageSize = 10);
}

public sealed class RatingApiService : ApiClientBase, IRatingApiService
{
    private const string Base = "/api/contractor-ratings";

    public RatingApiService(IHttpClientFactory httpFactory, IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, ContractorRatingDto? Data, string? Error)> RateAsync(ContractorRatingCreateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<ContractorRatingDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<IReadOnlyList<ContractorRatingDto>?> GetByContractorAsync(Guid contractorId)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/contractors/{contractorId}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IReadOnlyList<ContractorRatingDto>>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<ContractorRatingDto?> GetByAssignmentAsync(Guid assignmentId)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/assignments/{assignmentId}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ContractorRatingDto>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<PagedResultDto<ContractorRatingDto>?> GetMineAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/my?page={page}&pageSize={pageSize}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<ContractorRatingDto>>(json, JsonOpts);
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
        if (string.IsNullOrWhiteSpace(json)) return (false, default, res.ReasonPhrase);
        try
        {
            var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
            return (false, default, err?.Error ?? res.ReasonPhrase);
        }
        catch { return (false, default, res.ReasonPhrase); }
    }
}
