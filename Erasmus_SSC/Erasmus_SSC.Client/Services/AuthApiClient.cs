using System.Net.Http.Json;
using Erasmus_SSC.Client.Dtos;

namespace Erasmus_SSC.Client.Services;

public sealed class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<TokenResponseDto> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var payload = new
        {
            email,
            password
        };

        var resp = await _http.PostAsJsonAsync("/api/auth/login", payload, ct);

        
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(text) ? $"Login failed ({(int)resp.StatusCode})" : text);
        }

        var result = await resp.Content.ReadFromJsonAsync<TokenResponseDto>(cancellationToken: ct);
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken))
            throw new InvalidOperationException("Login succeeded but token response is empty.");

        return result;
    }

    public async Task<TokenResponseDto> RefreshAsync(CancellationToken ct = default)
    {
       
        var resp = await _http.PostAsync("/api/auth/refresh-token", content: null, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(text) ? $"Refresh failed ({(int)resp.StatusCode})" : text);
        }

        var result = await resp.Content.ReadFromJsonAsync<TokenResponseDto>(cancellationToken: ct);
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken))
            throw new InvalidOperationException("Refresh succeeded but token response is empty.");

        return result;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        
        var resp = await _http.PostAsync("/api/auth/logout", content: null, ct);
      
    }

}
