namespace Erasmus_SSC.Dtos.Users;

public sealed class CreateUserRequestDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "User"; 
}