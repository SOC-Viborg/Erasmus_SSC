using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erasmus_SSC.Client.Dtos;

namespace Erasmus_SSC.Client.Services;

public sealed class AdminUsersApiClient
{
    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;

    public AdminUsersApiClient(HttpClient http, ITokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        string url,
        object? body,
        CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("No access token. Please log in again.");

        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
            req.Content = JsonContent.Create(body);

        return req;
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Get, "/api/admin/users", body: null, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(text)
                ? $"GetUsers failed ({(int)resp.StatusCode})"
                : text);
        }

        return await resp.Content.ReadFromJsonAsync<List<AdminUserDto>>(cancellationToken: ct)
               ?? new List<AdminUserDto>();
    }

    public async Task<AdminUserDto> CreateUserAsync(AdminCreateUserDto dto, CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/admin/users", dto, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(text)
                ? $"CreateUser failed ({(int)resp.StatusCode})"
                : text);
        }

        var created = await resp.Content.ReadFromJsonAsync<AdminUserDto>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("CreateUser succeeded but response is empty.");

        return created;
    }

    public async Task DeleteUserAsync(int userId, CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"/api/admin/users/{userId}", body: null, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent)
            return;

        var text = await resp.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(text)
            ? $"DeleteUser failed ({(int)resp.StatusCode})"
            : text);
    }

    public async Task<AdminUserDto> UpdateUserAsync(int userId, AdminUpdateUserDto dto, CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Put, $"/api/admin/users/{userId}", dto, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(text)
                ? $"UpdateUser failed ({(int)resp.StatusCode})"
                : text);
        }

        var updated = await resp.Content.ReadFromJsonAsync<AdminUserDto>(cancellationToken: ct);
        if (updated is null) throw new InvalidOperationException("UpdateUser succeeded but response is empty.");

        return updated;
    }

}
