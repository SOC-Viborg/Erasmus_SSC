namespace Erasmus_SSC.Dtos.UserFiles;

public sealed class UserFileItemDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = "application/octet-stream";
    public int SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public int OwnerUserId { get; set; }
    public string OwnerUserName { get; set; } = string.Empty;
}