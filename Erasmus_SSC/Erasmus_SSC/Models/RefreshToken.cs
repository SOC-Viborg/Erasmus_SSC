namespace Erasmus_SSC.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public DateTime? Revoked { get; set; }
        public string ReplacedByToken { get; set; } = string.Empty;
        public string CreatedByIp { get; set; } = string.Empty;
        public string Device { get; set; } = "Unknown device";
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
