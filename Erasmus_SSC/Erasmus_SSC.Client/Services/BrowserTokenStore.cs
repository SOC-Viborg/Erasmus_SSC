using Microsoft.JSInterop;

namespace Erasmus_SSC.Client.Services;

public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task SetAccessTokenAsync(string token);
    Task ClearAsync();
}

public sealed class BrowserTokenStore : ITokenStore
{
    private const string Key = "auth.access_token";
    private readonly IJSRuntime _js;

    public BrowserTokenStore(IJSRuntime js) => _js = js;

    public Task<string?> GetAccessTokenAsync()
        => _js.InvokeAsync<string?>("localStorage.getItem", Key).AsTask();

    public Task SetAccessTokenAsync(string token)
        => _js.InvokeVoidAsync("localStorage.setItem", Key, token).AsTask();

    public Task ClearAsync()
        => _js.InvokeVoidAsync("localStorage.removeItem", Key).AsTask();
}
