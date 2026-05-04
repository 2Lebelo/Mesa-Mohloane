using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface ITenderApiService
{
    Task<(bool Ok, TenderApplicationDto? Data, string? Error)> SubmitTenderAsync(TenderApplicationCreateDto dto);
    Task<(bool Ok, TenderApplicationDto? Data, string? Error)> UpdateTenderAsync(Guid id, TenderApplicationUpdateDto dto);
    Task<(bool Ok, string? Error)> WithdrawAsync(Guid id);

    Task<TenderApplicationDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<TenderApplicationDto>?> GetByIncidentAsync(Guid incidentId);
    Task<(bool Ok, IReadOnlyList<TenderApplicationDto>? Data, string? Error)> EvaluateAsync(Guid incidentId);
    Task<PagedResultDto<TenderApplicationListDto>?> GetMineAsync(int page = 1, int pageSize = 10);

    // Existing assignment integration kept here because Admin incident review uses it.
    Task<(bool Ok, AssignmentDto? Data, string? Error)> AssignTenderAsync(Guid incidentId, Guid tenderId);
}

public sealed class TenderApiService : ApiClientBase, ITenderApiService
{
    private const string Base = "/api/TenderApplications";

    public TenderApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, TenderApplicationDto? Data, string? Error)> SubmitTenderAsync(
        TenderApplicationCreateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<TenderApplicationDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, TenderApplicationDto? Data, string? Error)> UpdateTenderAsync(
        Guid id,
        TenderApplicationUpdateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PutAsync($"{Base}/{id}", JsonBody(dto));
            return await ParseResponse<TenderApplicationDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, string? Error)> WithdrawAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/withdraw", null);

            if (res.IsSuccessStatusCode)
                return (true, null);

            return (false, await ReadError(res));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<TenderApplicationDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/{id}");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TenderApplicationDto>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<TenderApplicationDto>?> GetByIncidentAsync(Guid incidentId)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/incidents/{incidentId}");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IReadOnlyList<TenderApplicationDto>>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<(bool Ok, IReadOnlyList<TenderApplicationDto>? Data, string? Error)> EvaluateAsync(
        Guid incidentId)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync($"{Base}/incidents/{incidentId}/evaluate", null);
            return await ParseResponse<IReadOnlyList<TenderApplicationDto>>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<PagedResultDto<TenderApplicationListDto>?> GetMineAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/my?page={page}&pageSize={pageSize}");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<TenderApplicationListDto>>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<(bool Ok, AssignmentDto? Data, string? Error)> AssignTenderAsync(
        Guid incidentId,
        Guid tenderId)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync("/api/assignments/assign", JsonBody(new
            {
                IncidentId = incidentId,
                TenderApplicationId = tenderId
            }));

            return await ParseResponse<AssignmentDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
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

    private static async Task<string?> ReadError(HttpResponseMessage res)
    {
        try
        {
            var json = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return res.ReasonPhrase;

            var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
            return err?.Error ?? res.ReasonPhrase;
        }
        catch { return res.ReasonPhrase; }
    }
}
