using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IAssignmentApiService
{
    Task<(bool Ok, AssignmentDto? Data, string? Error)> AssignAsync(Guid incidentId, Guid tenderApplicationId);

    Task<PagedResultDto<AssignmentDto>?> GetMineAsync(int page = 1, int pageSize = 10);
    Task<PagedResultDto<AssignmentDto>?> GetAllAsync(int page = 1, int pageSize = 10, int? status = null);

    Task<AssignmentDto?> GetByIdAsync(Guid id);
    Task<AssignmentDto?> GetByIncidentAsync(Guid incidentId);

    Task<(bool Ok, AssignmentDto? Data, string? Error)> StartAsync(Guid id);
    Task<(bool Ok, WorkCompletionDto? Data, string? Error)> CompleteAsync(Guid id, WorkCompletionCreateDto dto);
    Task<(bool Ok, AssignmentDto? Data, string? Error)> AcknowledgeAsync(Guid id);
    Task<(bool Ok, AssignmentDto? Data, string? Error)> ApproveAsync(Guid id);
    Task<(bool Ok, WorkCompletionDto? Data, string? Error)> CompleteWithEvidenceAsync(
    Guid id,
    string completionSummary,
    IFormFile evidenceFile);
}

public sealed class AssignmentApiService : ApiClientBase, IAssignmentApiService
{
    private const string Base = "/api/assignments";

    public AssignmentApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, AssignmentDto? Data, string? Error)> AssignAsync(
        Guid incidentId,
        Guid tenderApplicationId)
    {
        try
        {
            var client = CreateClient();

            var payload = new
            {
                IncidentId = incidentId,
                TenderApplicationId = tenderApplicationId
            };

            var res = await client.PostAsync($"{Base}/assign", JsonBody(payload));
            return await ParseResponse<AssignmentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<PagedResultDto<AssignmentDto>?> GetMineAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/my?page={page}&pageSize={pageSize}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<AssignmentDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PagedResultDto<AssignmentDto>?> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        int? status = null)
    {
        try
        {
            var query = $"?page={page}&pageSize={pageSize}";

            if (status.HasValue)
                query += $"&status={status.Value}";

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}{query}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<AssignmentDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AssignmentDto?> GetByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return null;

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/{id}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssignmentDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AssignmentDto?> GetByIncidentAsync(Guid incidentId)
    {
        try
        {
            if (incidentId == Guid.Empty)
                return null;

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/incidents/{incidentId}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssignmentDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Ok, AssignmentDto? Data, string? Error)> StartAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/start", null);
            return await ParseResponse<AssignmentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, WorkCompletionDto? Data, string? Error)> CompleteWithEvidenceAsync(
    Guid id,
    string completionSummary,
    IFormFile evidenceFile)
    {
        try
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(completionSummary), "completionSummary");

            var fileContent = new StreamContent(evidenceFile.OpenReadStream());
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(evidenceFile.ContentType);

            form.Add(fileContent, "completionEvidenceFile", evidenceFile.FileName);

            var client = CreateClient();
            var res = await client.PostAsync($"/api/assignments/{id}/complete", form);

            return await ParseResponse<WorkCompletionDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, WorkCompletionDto? Data, string? Error)> CompleteAsync(
        Guid id,
        WorkCompletionCreateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync($"{Base}/{id}/complete", JsonBody(dto));
            return await ParseResponse<WorkCompletionDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, AssignmentDto? Data, string? Error)> AcknowledgeAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/acknowledge", null);
            return await ParseResponse<AssignmentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, AssignmentDto? Data, string? Error)> ApproveAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/approve", null);
            return await ParseResponse<AssignmentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private static async Task<(bool Ok, T? Data, string? Error)> ParseResponse<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (true, default, null);

            return (true, JsonSerializer.Deserialize<T>(json, JsonOpts), null);
        }

        if (string.IsNullOrWhiteSpace(json))
            return (false, default, res.ReasonPhrase);

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