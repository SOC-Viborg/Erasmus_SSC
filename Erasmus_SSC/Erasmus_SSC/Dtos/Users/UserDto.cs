namespace Erasmus_SSC.Dtos.Users;

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}