namespace Erasmus_SSC.Client.Dtos;

public sealed class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty; 
}
