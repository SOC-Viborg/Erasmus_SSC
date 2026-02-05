using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Erasmus_SSC.Client.Services;

/// <summary>
/// Builds Blazor auth state from the JWT stored in localStorage.
/// Fixes two common issues:
/// 1) maps "role" -> ClaimTypes.Role so principal.IsInRole("Admin") works
/// 2) if token is expired, clears storage and returns anonymous state
/// </summary>
public sealed class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStore _store;

  
    private static readonly TimeSpan ExpSkew = TimeSpan.FromSeconds(30);

    public JwtAuthStateProvider(ITokenStore store) => _store = store;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _store.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous();

        if (IsExpired(token))
        {
            await _store.ClearAsync();
            return Anonymous();
        }

        var identity = new ClaimsIdentity(
            claims: ParseClaims(token),
            authenticationType: "jwt",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

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

    private static AuthenticationState Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static bool IsExpired(string jwt)
    {
        if (!TryGetExpUtc(jwt, out var expUtc))
            return false; 
        
        return expUtc <= DateTimeOffset.UtcNow.Add(ExpSkew);
    }

    private static bool TryGetExpUtc(string jwt, out DateTimeOffset expUtc)
    {
        expUtc = default;

        var parts = jwt.Split('.');
        if (parts.Length < 2) return false;

        var jsonBytes = DecodeBase64(parts[1]);
        using var doc = JsonDocument.Parse(jsonBytes);

        if (!doc.RootElement.TryGetProperty("exp", out var expEl))
            return false;

        long seconds;
        switch (expEl.ValueKind)
        {
            case JsonValueKind.Number:
                if (!expEl.TryGetInt64(out seconds)) return false;
                break;
            case JsonValueKind.String:
                if (!long.TryParse(expEl.GetString(), out seconds)) return false;
                break;
            default:
                return false;
        }

        if (seconds <= 0) return false;
        expUtc = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) yield break;

        var jsonBytes = DecodeBase64(parts[1]);
        using var doc = JsonDocument.Parse(jsonBytes);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var claimType = MapClaimType(prop.Name);

            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                    yield return new Claim(claimType, item.ToString());

                continue;
            }

            yield return new Claim(claimType, prop.Value.ToString());
        }
    }

    private static string MapClaimType(string type) => type switch
    {
       
        "role" or "roles" => ClaimTypes.Role,
        "email" => ClaimTypes.Email,
        "name" or "unique_name" => ClaimTypes.Name,
        "sub" or "nameid" => ClaimTypes.NameIdentifier,

        
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" => ClaimTypes.Role,
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => ClaimTypes.Name,
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => ClaimTypes.NameIdentifier,
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" => ClaimTypes.Email,

        _ => type
    };

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
