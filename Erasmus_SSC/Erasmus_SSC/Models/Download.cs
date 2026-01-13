namespace Erasmus_SSC.Models
{
    public class Download
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string StoredPath { get; set; } = default!;
        public int SizeBytes { get; set; }
    }
}
