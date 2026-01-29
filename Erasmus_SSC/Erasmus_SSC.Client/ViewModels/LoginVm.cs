using System.ComponentModel.DataAnnotations;

namespace Erasmus_SSC.Client.ViewModels;

public sealed class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

   
    public string? Error { get; set; }
    public bool IsBusy { get; set; }
}
