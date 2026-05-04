using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IPaymentApiService
{
    Task<(bool Ok, PaymentDto? Data, string? Error)> InitiateAsync(PaymentCreateDto dto);
    Task<(bool Ok, PaymentDto? Data, string? Error)> ApproveAsync(Guid id);
    Task<(bool Ok, PaymentDto? Data, string? Error)> DisburseAsync(Guid id);
    Task<(bool Ok, PaymentDto? Data, string? Error)> MarkFailedAsync(Guid id, string reason);
    Task<PaymentDto?> GetByIdAsync(Guid id);
    Task<PaymentDto?> GetByInvoiceAsync(Guid invoiceId);
}

public sealed class PaymentApiService : ApiClientBase, IPaymentApiService
{
    private const string Base = "/api/payments";

    public PaymentApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, PaymentDto? Data, string? Error)> InitiateAsync(PaymentCreateDto dto)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, PaymentDto? Data, string? Error)> ApproveAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/approve", null);
            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, PaymentDto? Data, string? Error)> DisburseAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/disburse", null);
            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, PaymentDto? Data, string? Error)> MarkFailedAsync(Guid id, string reason)
    {
        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/fail", JsonBody(new { Reason = reason }));
            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/{id}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PaymentDto>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<PaymentDto?> GetByInvoiceAsync(Guid invoiceId)
    {
        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/invoices/{invoiceId}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PaymentDto>(json, JsonOpts);
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
