using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Mesa_Mohloane_Frontend.Services.Api;

public abstract class ApiClientBase
{
    protected readonly IHttpClientFactory HttpFactory;
    protected readonly IHttpContextAccessor HttpContextAccessor;

    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected ApiClientBase(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        HttpFactory = httpFactory;
        HttpContextAccessor = httpContextAccessor;
    }

    //protected HttpClient CreateClient(bool attachJwt = true)
    //{
    //    var client = HttpFactory.CreateClient("MesaApi");

    //    if (!attachJwt)
    //        return client;

    //    var token = GetJwtToken();

    //    if (!string.IsNullOrWhiteSpace(token))
    //    {
    //        client.DefaultRequestHeaders.Authorization =
    //            new AuthenticationHeaderValue("Bearer", token);
    //    }

    //    return client;
    //}
    protected HttpClient CreateClient(bool attachJwt = true)
    {
        var client = HttpFactory.CreateClient("MesaApi");

        if (!attachJwt)
            return client;

        var http = HttpContextAccessor.HttpContext;
        var token = http?.Session.GetString("jwt_token");

        if (string.IsNullOrWhiteSpace(token))
            token = http?.User?.FindFirstValue("jwt_token");

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    protected static StringContent JsonBody(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    protected string? GetJwtToken()
    {
        var http = HttpContextAccessor.HttpContext;

        var token = http?.Session.GetString("jwt_token");

        if (string.IsNullOrWhiteSpace(token))
            token = http?.User?.FindFirst("jwt_token")?.Value;

        if (string.IsNullOrWhiteSpace(token))
            token = http?.User?.FindFirstValue("jwt_token");

        return token;
    }

    protected static async Task<(bool Ok, T? Data, string? Error)> ParseResponseAsync<T>(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (true, default, null);

            return (true, JsonSerializer.Deserialize<T>(json, JsonOpts), null);
        }

        return (false, default, ExtractError(json) ?? response.ReasonPhrase ?? "Request failed.");
    }

    protected static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return ExtractError(json) ?? response.ReasonPhrase ?? "Request failed.";
    }

    private static string? ExtractError(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
                return error.GetString();

            if (root.TryGetProperty("message", out var message))
                return message.GetString();

            if (root.ValueKind == JsonValueKind.String)
                return root.GetString();

            return json;
        }
        catch
        {
            return json;
        }
    }
}