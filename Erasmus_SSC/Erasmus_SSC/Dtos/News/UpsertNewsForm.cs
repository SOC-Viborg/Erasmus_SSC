using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Erasmus_SSC.Dtos.News;

public sealed class UpsertNewsForm
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public IFormFile? Image { get; set; }
}
