namespace Erasmus_SSC.Client.Dtos.Users;

public sealed class CreateUserRequestDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "User";
}

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}
