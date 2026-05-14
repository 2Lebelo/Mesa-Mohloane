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
        if (dto.InvoiceId == Guid.Empty)
            return (false, null, "Invalid invoice id.");

        if (dto.Amount <= 0)
            return (false, null, "Payment amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(dto.PaymentReference))
            return (false, null, "Payment reference is required.");

        if (string.IsNullOrWhiteSpace(dto.Method))
            return (false, null, "Payment method is required.");

        try
        {
            var client = CreateClient();
            var res = await client.PostAsync(Base, JsonBody(dto));
            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, PaymentDto? Data, string? Error)> ApproveAsync(Guid id)
    {
        if (id == Guid.Empty)
            return (false, null, "Invalid payment id.");

        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/approve", null);
            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, PaymentDto? Data, string? Error)> DisburseAsync(Guid id)
    {
        if (id == Guid.Empty)
            return (false, null, "Invalid payment id.");

        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync($"{Base}/{id}/disburse", null);
            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Ok, PaymentDto? Data, string? Error)> MarkFailedAsync(Guid id, string reason)
    {
        if (id == Guid.Empty)
            return (false, null, "Invalid payment id.");

        if (string.IsNullOrWhiteSpace(reason))
            return (false, null, "Failure reason is required.");

        try
        {
            var client = CreateClient();
            var res = await client.PatchAsync(
                $"{Base}/{id}/fail",
                JsonBody(new { Reason = reason.Trim() }));

            return await ParseResponse<PaymentDto>(res);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid id)
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
            return JsonSerializer.Deserialize<PaymentDto>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PaymentDto?> GetByInvoiceAsync(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
            return null;

        try
        {
            var client = CreateClient();
            var res = await client.GetAsync($"{Base}/invoices/{invoiceId}");

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PaymentDto>(json, JsonOpts);
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

            if (!string.IsNullOrWhiteSpace(err?.Error))
                return (false, default, err.Error);

            return (false, default, json);
        }
        catch
        {
            return (false, default, json);
        }
    }
}