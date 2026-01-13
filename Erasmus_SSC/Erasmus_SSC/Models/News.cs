namespace Erasmus_SSC.Models
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
