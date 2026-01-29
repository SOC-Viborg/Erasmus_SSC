using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Erasmus_SSC.Client.Services;

public sealed class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStore _store;

    public JwtAuthStateProvider(ITokenStore store) => _store = store;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _store.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(ParseClaims(token), authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyLoggedInAsync(string accessToken)
    {
        await _store.SetAccessTokenAsync(accessToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLoggedOutAsync()
    {
        await _store.ClearAsync();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        // payload is second part
        var payload = jwt.Split('.')[1];
        var jsonBytes = DecodeBase64(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes) ?? new();

        foreach (var kv in keyValuePairs)
        {
            // role can be array or single
            if (kv.Value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in je.EnumerateArray())
                    yield return new Claim(kv.Key, item.ToString());
            }
            else
            {
                yield return new Claim(kv.Key, kv.Value?.ToString() ?? "");
            }
        }
    }

    private static byte[] DecodeBase64(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
