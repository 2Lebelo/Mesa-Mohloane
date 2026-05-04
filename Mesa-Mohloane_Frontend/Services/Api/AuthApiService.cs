using Mesa_Mohloane_Frontend.Dtos;
using System.Text.Json;

namespace Mesa_Mohloane_Frontend.Services.Api;

// Interface 
public interface IAuthApiService
{
    Task<(bool Ok, AuthResponseDto? Data, string? Error)> LoginAsync(LoginRequestDto dto);
    Task<(bool Ok, AuthResponseDto? Data, string? Error)> RegisterAsync(RegisterRequestDto dto);
    Task<List<RoleDto>?> GetPublicRolesAsync();
}

// Implementation
public sealed class AuthApiService : ApiClientBase, IAuthApiService
{
    public AuthApiService(
        IHttpClientFactory httpFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, AuthResponseDto? Data, string? Error)> LoginAsync(
        LoginRequestDto dto)
    {
        var client = CreateClient(attachJwt: false);
        var res = await client.PostAsync("/api/auth/login", JsonBody(dto));
        var json = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
            return (true, JsonSerializer.Deserialize<AuthResponseDto>(json, JsonOpts), null);

        var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
        return (false, null, err?.Error ?? res.ReasonPhrase);
    }

    public async Task<(bool Ok, AuthResponseDto? Data, string? Error)> RegisterAsync(
        RegisterRequestDto dto)
    {
        var client = CreateClient(attachJwt: false);
        var res = await client.PostAsync("/api/auth/register", JsonBody(dto));
        var json = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
            return (true, JsonSerializer.Deserialize<AuthResponseDto>(json, JsonOpts), null);

        var err = JsonSerializer.Deserialize<ApiErrorDto>(json, JsonOpts);
        return (false, null, err?.Error ?? res.ReasonPhrase);
    }

    public async Task<List<RoleDto>?> GetPublicRolesAsync()
    {
        try
        {
            var client = CreateClient(attachJwt: false);
            var res = await client.GetAsync("/api/roles/public");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<RoleDto>>(json, JsonOpts);
        }
        catch { return null; }
    }
}