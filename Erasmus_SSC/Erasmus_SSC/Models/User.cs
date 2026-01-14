namespace Erasmus_SSC.Models
{
    public class User
    {
        public int Id { get; set; }

        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;

        public int RoleId { get; set; }

        public UserRole? Role { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<UserFile> UserFiles { get; set; } = new List<UserFile>();
    }
}
