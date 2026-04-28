namespace Erasmus_SSC.Client.Dtos;

public sealed class ReportItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = "application/octet-stream";
    public int SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public int LanguageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
}
