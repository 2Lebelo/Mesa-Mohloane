using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IInvoiceApiService
{
    Task<(bool Ok, InvoiceDto? Data, string? Error)> SubmitAsync(InvoiceCreateDto dto);

    Task<PagedResultDto<InvoiceListDto>?> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        int? status = null);

    Task<PagedResultDto<InvoiceListDto>?> GetMineAsync(
        int page = 1,
        int pageSize = 10);

    Task<InvoiceDto?> GetByIdAsync(Guid id);

    Task<InvoiceDto?> GetByAssignmentAsync(Guid assignmentId);

    Task<PagedResultDto<InvoiceListDto>?> GetFlaggedAsync(
        int page = 1,
        int pageSize = 10);

    Task<(bool Ok, InvoiceDto? Data, string? Error)> ValidateAsync(Guid id, string? remarks);

    Task<(bool Ok, InvoiceDto? Data, string? Error)> ApproveAsync(Guid id);

    Task<(bool Ok, InvoiceDto? Data, string? Error)> RejectAsync(Guid id, string reason);
}

public sealed class InvoiceApiService : ApiClientBase, IInvoiceApiService
{
    private const string Base = "/api/invoices";

    public InvoiceApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, InvoiceDto? Data, string? Error)> SubmitAsync(InvoiceCreateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<InvoiceDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<PagedResultDto<InvoiceListDto>?> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        int? status = null)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = $"?page={page}&pageSize={pageSize}";

            if (status.HasValue)
                query += $"&status={status.Value}";

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}{query}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<InvoiceListDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PagedResultDto<InvoiceListDto>?> GetMineAsync(
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/my?page={page}&pageSize={pageSize}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<InvoiceListDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/{id}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<InvoiceDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<InvoiceDto?> GetByAssignmentAsync(Guid assignmentId)
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
            return JsonSerializer.Deserialize<InvoiceDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PagedResultDto<InvoiceListDto>?> GetFlaggedAsync(
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/flagged?page={page}&pageSize={pageSize}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<InvoiceListDto>>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Ok, InvoiceDto? Data, string? Error)> ValidateAsync(
        Guid id,
        string? remarks)
    {
        if (id == Guid.Empty)
            return (false, null, "Invalid invoice id.");

        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync(
                $"{Base}/{id}/validate",
                JsonBody(new { Remarks = remarks }));

            return await ParseResponse<InvoiceDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, InvoiceDto? Data, string? Error)> ApproveAsync(Guid id)
    {
        if (id == Guid.Empty)
            return (false, null, "Invalid invoice id.");

        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/approve", null);
            return await ParseResponse<InvoiceDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, InvoiceDto? Data, string? Error)> RejectAsync(
        Guid id,
        string reason)
    {
        if (id == Guid.Empty)
            return (false, null, "Invalid invoice id.");

        if (string.IsNullOrWhiteSpace(reason))
            return (false, null, "Rejection reason is required.");

        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync(
                $"{Base}/{id}/reject",
                JsonBody(new { Reason = reason }));

            return await ParseResponse<InvoiceDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
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