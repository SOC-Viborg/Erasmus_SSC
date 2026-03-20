using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Erasmus_SSC.Dtos.UserFiles;

public sealed class UploadUserFileForm
{
    [Required]
    public IFormFile? File { get; set; }
}