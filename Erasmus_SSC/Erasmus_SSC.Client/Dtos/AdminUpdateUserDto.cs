namespace Erasmus_SSC.Client.Dtos;

public sealed class AdminUpdateUserDto
{
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
   
    public string? Password { get; set; } 
}
