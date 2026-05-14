using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IRatingApiService
{
    Task<(bool Ok, ContractorRatingDto? Data, string? Error)> RateAsync(ContractorRatingCreateDto dto);

    Task<PagedResultDto<ContractorRatingDto>?> GetByContractorAsync(
        Guid contractorId,
        int page = 1,
        int pageSize = 10);

    Task<ContractorRatingDto?> GetByAssignmentAsync(Guid assignmentId);
}

public sealed class RatingApiService : ApiClientBase, IRatingApiService
{
    private const string Base = "/api/contractor-ratings";

    public RatingApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, ContractorRatingDto? Data, string? Error)> RateAsync(
        ContractorRatingCreateDto dto)
    {
        if (dto.IncidentId == Guid.Empty)
            return (false, null, "Invalid incident id.");

        if (dto.AssignmentId == Guid.Empty)
            return (false, null, "Invalid assignment id.");

        if (dto.ContractorId == Guid.Empty)
            return (false, null, "Invalid contractor id.");

        if (dto.Stars is < 1 or > 5)
            return (false, null, "Rating must be between 1 and 5 stars.");

        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<ContractorRatingDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<PagedResultDto<ContractorRatingDto>?> GetByContractorAsync(
        Guid contractorId,
        int page = 1,
        int pageSize = 10)
    {
        if (contractorId == Guid.Empty)
            return null;

        try
        {
            page = Math.Max(page, 1);
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var client = CreateClient();
            var res = await client.GetAsync(
                $"{Base}/contractors/{contractorId}?page={page}&pageSize={pageSize}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<ContractorRatingDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ContractorRatingDto?> GetByAssignmentAsync(Guid assignmentId)
    {
        if (assignmentId == Guid.Empty)
            return null;

        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/assignments/{assignmentId}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ContractorRatingDto>(json, JsonOpts);
        }
        catch
        {
            return null;
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

        if (string.IsNullOrWhiteSpace(json))
            return (false, default, $"{(int)res.StatusCode} {res.ReasonPhrase}");

        try
        {
            var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
            return (false, default, err?.Error ?? json);
        }
        catch
        {
            return (false, default, json);
        }
    }
}