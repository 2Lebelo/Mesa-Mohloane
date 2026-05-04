using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Mesa_Mohloane_Frontend.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public abstract class BaseController : Controller
{
    protected readonly IHttpClientFactory HttpFactory;
    protected readonly IConfiguration Config;

    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected BaseController(IHttpClientFactory http, IConfiguration config)
    {
        HttpFactory = http;
        Config = config;
    }

    protected HttpClient ApiClient()
    {
        var client = HttpFactory.CreateClient("MesaApi");
        var token = GetJwtToken();

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    protected async Task<T?> GetAsync<T>(string path)
    {
        try
        {
            var res = await ApiClient().GetAsync(path);
            if (!res.IsSuccessStatusCode) return default;

            var json = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return default;

            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch
        {
            return default;
        }
    }

    protected async Task<(bool Ok, T? Data, string? Error)> PostAsync<T>(string path, object body)
    {
        try
        {
            var res = await ApiClient().PostAsync(path, JsonContent(body));
            return await ParseResponse<T>(res);
        }
        catch (JsonException)
        {
            return (false, default, "Invalid JSON from API.");
        }
        catch (Exception ex)
        {
            return (false, default, ex.Message);
        }
    }

    protected async Task<(bool Ok, T? Data, string? Error)> PutAsync<T>(string path, object body)
    {
        try
        {
            var res = await ApiClient().PutAsync(path, JsonContent(body));
            return await ParseResponse<T>(res);
        }
        catch (JsonException)
        {
            return (false, default, "Invalid JSON from API.");
        }
        catch (Exception ex)
        {
            return (false, default, ex.Message);
        }
    }

    protected async Task<(bool Ok, string? Error)> PatchAsync(string path, object? body = null)
    {
        try
        {
            var res = await ApiClient().PatchAsync(path, JsonContent(body ?? new { }));
            if (res.IsSuccessStatusCode) return (true, null);

            return (false, await ReadApiError(res));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    protected async Task<(bool Ok, T? Data, string? Error)> PatchAsync<T>(string path, object? body = null)
    {
        try
        {
            var res = await ApiClient().PatchAsync(path, JsonContent(body ?? new { }));
            return await ParseResponse<T>(res);
        }
        catch (JsonException)
        {
            return (false, default, "Invalid JSON from API.");
        }
        catch (Exception ex)
        {
            return (false, default, ex.Message);
        }
    }

    protected async Task<(bool Ok, string? Error)> DeleteAsync(string path)
    {
        try
        {
            var res = await ApiClient().DeleteAsync(path);
            if (res.IsSuccessStatusCode) return (true, null);

            return (false, await ReadApiError(res));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    //protected void SetUserViewData()
    //{
    //    ViewData["UserName"] = GetSessionOrClaim("user_name", ClaimTypes.Name) ?? "User";
    //    ViewData["UserEmail"] = GetSessionOrClaim("user_email", ClaimTypes.Email) ?? "";
    //    ViewData["UserRole"] = GetSessionOrClaim("user_role", ClaimTypes.Role) ?? "";
    //    ViewData["UserId"] = GetSessionOrClaim("user_id", ClaimTypes.NameIdentifier) ?? "";
    //    ViewData["JwtToken"] = GetJwtToken() ?? "";
    //}
    protected void SetUserViewData()
    {
        ViewData["UserName"] =
            HttpContext.Session.GetString("user_name")
            ?? HttpContext.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? "User";

        ViewData["UserEmail"] =
            HttpContext.Session.GetString("user_email")
            ?? HttpContext.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? "";

        ViewData["UserRole"] =
            HttpContext.Session.GetString("user_role")
            ?? HttpContext.User?.FindFirst(ClaimTypes.Role)?.Value
            ?? "";

        ViewData["UserId"] =
            HttpContext.Session.GetString("user_id")
            ?? HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "";

        ViewData["JwtToken"] =
            HttpContext.Session.GetString("jwt_token")
            ?? HttpContext.User?.FindFirst("jwt_token")?.Value
            ?? "";
    }

    protected Guid CurrentUserId()
    {
        var raw = GetSessionOrClaim("user_id", ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    protected string? CurrentUserRole()
        => GetSessionOrClaim("user_role", ClaimTypes.Role);

    protected string? CurrentUserName()
        => GetSessionOrClaim("user_name", ClaimTypes.Name);

    protected string? CurrentUserEmail()
        => GetSessionOrClaim("user_email", ClaimTypes.Email);

    private string? GetJwtToken()
    {
        var token = HttpContext.Session.GetString("jwt_token");

        if (string.IsNullOrWhiteSpace(token))
            token = HttpContext.User?.FindFirst("jwt_token")?.Value;

        return token;
    }

    private string? GetSessionOrClaim(string sessionKey, string claimType)
    {
        var value = HttpContext.Session.GetString(sessionKey);

        if (string.IsNullOrWhiteSpace(value))
            value = HttpContext.User?.FindFirst(claimType)?.Value;

        return value;
    }

    private static StringContent JsonContent(object body)
        => new(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

    private static async Task<(bool Ok, T? Data, string? Error)> ParseResponse<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (true, default, null);

            return (true, JsonSerializer.Deserialize<T>(json, JsonOpts), null);
        }

        return (false, default, await ReadApiError(res, json));
    }

    private static async Task<string?> ReadApiError(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        return await ReadApiError(res, json);
    }

    private static Task<string?> ReadApiError(HttpResponseMessage res, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Task.FromResult<string?>(res.ReasonPhrase);

        try
        {
            var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
            return Task.FromResult<string?>(err?.Error ?? err?.Message ?? res.ReasonPhrase);
        }
        catch
        {
            return Task.FromResult<string?>(res.ReasonPhrase);
        }
    }

    private sealed class ApiErrorDto
    {
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}