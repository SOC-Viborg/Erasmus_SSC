namespace Erasmus_SSC.Models
{
    public class ReportLanguage
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;

        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
