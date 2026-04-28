namespace Erasmus_SSC.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string StoredPath { get; set; } = default!;
        public string FileType { get; set; } = default!;
        public int SizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public int LanguageId { get; set; }
        public ReportLanguage Language { get; set; } = default!;
        public bool IsDeleted { get; set; }
    }
}
