using Erasmus_SSC.Client.Services;

namespace Erasmus_SSC.Services;

public sealed class ServerTokenStore : ITokenStore
{
    public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(null);
    public Task SetAccessTokenAsync(string token) => Task.CompletedTask;
    public Task ClearAsync() => Task.CompletedTask;
}
