using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public interface IIncidentApiService
{
    Task<(bool Ok, IncidentDetailDto? Data, string? Error)> CreateAsync(string title, string description, string locationName, decimal latitude, decimal longitude, IFormFileCollection? photos);
    Task<(bool Ok, IncidentDetailDto? Data, string? Error)> UpdateAsync(Guid id, IncidentUpdateRequestDto dto);
    Task<(bool Ok, string? Error)> DeleteAsync(Guid id);
    Task<(bool Ok, IncidentDetailDto? Data, string? Error)> AddPhotosAsync(Guid id, IFormFileCollection? photos);
    Task<(bool Ok, string? Error)> DeletePhotoAsync(Guid incidentId, Guid photoId);
    Task<PagedResultDto<IncidentListItemDto>?> GetMineAsync(int page = 1, int pageSize = 10);
    Task<IncidentDetailDto?> GetByIdAsync(Guid id);
    Task<PagedResultDto<IncidentListItemDto>?> GetAllAsync(int page = 1, int pageSize = 10, int? status = null, string? search = null);
    Task<PagedResultDto<IncidentListItemDto>?> GetOpenAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<(bool Ok, IncidentDetailDto? Data, string? Error)> VerifyAsync(Guid id);
    Task<(bool Ok, IncidentDetailDto? Data, string? Error)> PublishAsync(Guid id);
    Task<(bool Ok, IncidentDetailDto? Data, string? Error)> RejectAsync(Guid id, string reason);
}

public sealed class IncidentApiService : ApiClientBase, IIncidentApiService
{
    private const string Base = "/api/incidents";

    public IncidentApiService(IHttpClientFactory httpFactory, IHttpContextAccessor httpContextAccessor)
        : base(httpFactory, httpContextAccessor) { }

    public async Task<(bool Ok, IncidentDetailDto? Data, string? Error)> CreateAsync(string title, string description, string locationName, decimal latitude, decimal longitude, IFormFileCollection? photos)
    {
        try
        {
            using var form = BuildIncidentForm(title, description, locationName, latitude, longitude, photos);
            var res = await CreateClient().PostAsync(Base, form);
            return await ParseResponse<IncidentDetailDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, IncidentDetailDto? Data, string? Error)> UpdateAsync(Guid id, IncidentUpdateRequestDto dto)
    {
        try
        {
            var res = await CreateClient().PutAsync($"{Base}/{id}", JsonBody(dto));
            return await ParseResponse<IncidentDetailDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(Guid id)
    {
        try
        {
            var res = await CreateClient().DeleteAsync($"{Base}/{id}");
            return res.IsSuccessStatusCode ? (true, null) : (false, await ReadError(res));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Ok, IncidentDetailDto? Data, string? Error)> AddPhotosAsync(Guid id, IFormFileCollection? photos)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            AddFiles(form, photos);
            var res = await CreateClient().PostAsync($"{Base}/{id}/photos", form);
            return await ParseResponse<IncidentDetailDto>(res);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, string? Error)> DeletePhotoAsync(Guid incidentId, Guid photoId)
    {
        try
        {
            var res = await CreateClient().DeleteAsync($"{Base}/{incidentId}/photos/{photoId}");
            return res.IsSuccessStatusCode ? (true, null) : (false, await ReadError(res));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<PagedResultDto<IncidentListItemDto>?> GetMineAsync(int page = 1, int pageSize = 10)
        => await GetPagedAsync($"{Base}/my?page={page}&pageSize={pageSize}");

    public async Task<IncidentDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var res = await CreateClient().GetAsync($"{Base}/{id}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentDetailDto>(json, JsonOpts);
        }
        catch { return null; }
    }

    public async Task<PagedResultDto<IncidentListItemDto>?> GetAllAsync(int page = 1, int pageSize = 10, int? status = null, string? search = null)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (status.HasValue) query += $"&status={status.Value}";
        if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
        return await GetPagedAsync(Base + query);
    }

    public async Task<PagedResultDto<IncidentListItemDto>?> GetOpenAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
        return await GetPagedAsync($"{Base}/open{query}");
    }

    public async Task<(bool Ok, IncidentDetailDto? Data, string? Error)> VerifyAsync(Guid id)
    {
        try { return await ParseResponse<IncidentDetailDto>(await CreateClient().PatchAsync($"{Base}/{id}/verify", null)); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, IncidentDetailDto? Data, string? Error)> PublishAsync(Guid id)
    {
        try { return await ParseResponse<IncidentDetailDto>(await CreateClient().PatchAsync($"{Base}/{id}/publish", null)); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Ok, IncidentDetailDto? Data, string? Error)> RejectAsync(Guid id, string reason)
    {
        try { return await ParseResponse<IncidentDetailDto>(await CreateClient().PatchAsync($"{Base}/{id}/reject", JsonBody(new { Reason = reason }))); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    private async Task<PagedResultDto<IncidentListItemDto>?> GetPagedAsync(string path)
    {
        try
        {
            var res = await CreateClient().GetAsync(path);
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PagedResultDto<IncidentListItemDto>>(json, JsonOpts);
        }
        catch { return null; }
    }

    private static MultipartFormDataContent BuildIncidentForm(string title, string description, string locationName, decimal latitude, decimal longitude, IFormFileCollection? photos)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(title.Trim()), "title" },
            { new StringContent(description.Trim()), "description" },
            { new StringContent(locationName.Trim()), "locationName" },
            { new StringContent(latitude.ToString(CultureInfo.InvariantCulture)), "latitude" },
            { new StringContent(longitude.ToString(CultureInfo.InvariantCulture)), "longitude" }
        };
        AddFiles(form, photos);
        return form;
    }

    private static void AddFiles(MultipartFormDataContent form, IFormFileCollection? photos)
    {
        if (photos is null) return;
        foreach (var file in photos.Where(f => f.Length > 0))
        {
            var content = new StreamContent(file.OpenReadStream());
            content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType);
            form.Add(content, "photos", file.FileName);
        }
    }

    private static async Task<(bool Ok, T? Data, string? Error)> ParseResponse<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode)
            return string.IsNullOrWhiteSpace(json) ? (true, default, null) : (true, JsonSerializer.Deserialize<T>(json, JsonOpts), null);
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
