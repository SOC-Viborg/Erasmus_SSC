namespace Erasmus_SSC.Models
{
    public class UserFile
    {
        public int Id { get; set; }
        public string FileName { get; set; } = default!;
        public string StoredPath { get; set; } = default!;
        public string FileType { get; set; } = default!;
        public int SizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }

        public int OwnerUserId { get; set; }
        public User OwnerUser { get; set; } = default!;

        public bool IsDeleted { get; set; }
    }
}
