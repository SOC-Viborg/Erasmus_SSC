using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Erasmus_SSC.Dtos.Reports;

public sealed class UploadReportForm
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public int LanguageId { get; set; } = 1;

    [Required]
    public IFormFile? File { get; set; }
}
