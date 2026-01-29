
namespace Erasmus_SSC.Client.Services;

public sealed class TokenStore
{
    public string? AccessToken { get; private set; }

    public void SetAccessToken(string token) => AccessToken = token;

    public void Clear() => AccessToken = null;
}
