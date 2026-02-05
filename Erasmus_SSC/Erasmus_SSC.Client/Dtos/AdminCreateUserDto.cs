namespace Erasmus_SSC.Client.Dtos;

public sealed class AdminCreateUserDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
