using System.ComponentModel.DataAnnotations;

namespace Erasmus_SSC.Dtos
{

    public class LoginDto
    {
       
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }

}